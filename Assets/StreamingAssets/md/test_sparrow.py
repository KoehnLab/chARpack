import os
import scine_utilities as su
import scine_sparrow
from queue import Queue
import numpy as np
import time
import re
import copy
from pprint import pprint
import threading
import multiprocessing
from typing import List
from scine_heron.electronic_data.molden_file_reader import MoldenFileReader
from scine_heron.electronic_data.electronic_data import (Atom, ElectronicData,
                                                         GaussianOrbital,
                                                         MolecularOrbital)
from scine_heron.electronic_data.electronic_data_image_generator import ElectronicDataImageGenerator
from vtk.util.numpy_support import vtk_to_numpy

element_dict = su.ElementType.__members__

available_methods = ['MNDO', 'AM1', 'RM1', 'PM3', 'PM6', 'DFTB0', 'DFTB2', 'DFTB3']


class StoppableThread(threading.Thread):
    """Thread class with a stop() method. The thread itself has to check
    regularly for the stopped() condition."""

    def __init__(self,  *args, **kwargs):
        super(StoppableThread, self).__init__(*args, **kwargs)
        self._stop_event = threading.Event()

    def stop(self):
        self._stop_event.set()

    def stopped(self):
        return self._stop_event.is_set()

class chARpackSparrow:
    MAX_POS_QUEUE = 2
    MAX_MO_QUEUE = 2
    def __init__(self):
        self.structure = su.AtomCollection()
        self.mol_id = None
        self.calculator = None
        self.method = 'PM6'
        self.initialized = False
        self.isRunning = False
        self.ss_lambda = None
        self.ss_beta = 1.0
        self.pos_queue: Queue = Queue()
        self.mo_queue: Queue = Queue()
        self.molden_parser = MoldenFileReader()
        self.mo_index = -1
        self.calculator_lock = threading.Lock()
        os.environ["OMP_NUM_THREADS"] = str(int(0.5 * multiprocessing.cpu_count()))
        self.orbital_thread = None
        self.calculator_thread = None


    def setMethod(self, method):
        if (method == self.method): return
        if (method in available_methods):
            self.method = method
            self.__reInit()


    def setData(self, positions, symbols, indices = None, mol_id = None):
        self.mol_id = mol_id
        self.structure.elements = [element_dict[x] for x in symbols]
        pos_in_bohr = [self.ang_to_bohr(x) for x in positions]
        self.structure.positions = pos_in_bohr

        # Get calculator
        manager = su.core.ModuleManager.get_instance()
        self.calculator = manager.get('calculator', self.method)
        self.calculator.log.output.clear() ## deactivates log spam of calculator

        # print(self.calculator.__dir__())
        # exit()

        # Configure calculator
        self.calculator.structure = self.structure
        self.calculator.set_required_properties([su.Property.Gradients])
        self.ss_lambda = np.ones(len(self.structure.elements))

    def setMOIndex(self, id):
        self.mo_index = id

    def readFile(self, file_path):
        # Get calculator
        manager = su.core.ModuleManager.get_instance()
        self.calculator = manager.get('calculator', self.method)

        self.structure = su.io.read(file_path)[0]
        self.calculator.structure = self.structure
        self.calculator.set_required_properties([su.Property.Gradients])
        self.ss_lambda = np.ones(len(self.structure.elements))

    def __reInit(self):
        if (self.initialized):
            s = copy.deepcopy(self.structure)
            # Get calculator
            manager = su.core.ModuleManager.get_instance()
            self.calculator = manager.get('calculator', self.method)
            # Configure calculator
            self.structure = s
            self.calculator.structure = self.structure
            self.calculator.set_required_properties([su.Property.Gradients])
            self.ss_lambda = np.ones(len(self.structure.elements))

    def generateOrbitals(self):
        ## Generate orbitals (volume data)
        st = time.time()
        self.calculator_lock.acquire()
        wf_generator = su.core.to_wf_generator(self.calculator)
        molden_content = wf_generator.output_wavefunction()
        self.calculator_lock.release()
        print(f"Time to calc WF: {time.time() - st:0.3f}")
        electronic_data = self.molden_parser.read_molden(molden_content)
        image_generator = ElectronicDataImageGenerator(electronic_data)
        actual_index = self.__get_actual_index(electronic_data, self.mo_index)
        image = image_generator.generate_mo_image(actual_index)
        #dims = np.array([*image.GetDimensions()])
        mo_data = {"dimensions": image.GetDimensions(),
                "origin": self.bohr_to_ang(image.GetOrigin()).tolist(),
                "spacing": self.bohr_to_ang(image.GetSpacing()).tolist(),
                #"data": vtk_to_numpy(image.GetPointData().GetScalars()).reshape(dims[::-1]).transpose().flatten()}
                "data": vtk_to_numpy(image.GetPointData().GetScalars()),
                "wf": molden_content}
        # write molecule orbitals
        if self.mo_queue.qsize() >= self.MAX_MO_QUEUE:
            self.mo_queue.get()
        self.mo_queue.put(mo_data)
        print(f"Time for full orbital generation: {time.time() - st:0.3f}")

    def run(self):
        self.calculator_lock.acquire()
        results = self.calculator.calculate()
        self.calculator_lock.release()
   
        # Simulation step
        self.steepestDescent(results)
        # write atom positions
        if self.pos_queue.qsize() >= self.MAX_POS_QUEUE:
            self.pos_queue.get()
        self.pos_queue.put(np.array([self.bohr_to_ang(x) for x in self.structure.positions], dtype="float32").tolist())

    def startContinuousRun(self):
        self.isRunning = True
        self.calculator_thread = StoppableThread(target = self.__continuousCalculate)
        self.calculator_thread.daemon = True
        self.calculator_thread.start()
        self.orbital_thread = StoppableThread(target = self.__continuousOrbitals)
        self.orbital_thread.daemon = True
        self.orbital_thread.start()

    def __continuousCalculate(self):
        while self.isRunning:
            self.run()

    def __continuousOrbitals(self):
        while self.pos_queue.empty():
            pass
        while self.isRunning:
            self.generateOrbitals()

    def stopContinuousRun(self):
        self.isRunning = False
        self.calculator_thread.stop()
        self.orbital_thread.stop()

    def ang_to_bohr(self, pos):
        return np.array(pos) * su.BOHR_PER_ANGSTROM

    def bohr_to_ang(self, pos):
        return np.array(pos, dtype="float32") / su.BOHR_PER_ANGSTROM

    def getPositions(self):
        if not self.pos_queue.empty():
            return self.pos_queue.get()
        else:
            return []
    
    def getMO(self):
        if not self.mo_queue.empty():
            return self.mo_queue.get()
        else:
            return {}
    
    def getPositionOf(self, id):
        return self.bohr_to_ang(self.structure.positions[id])

    def steepestDescent(self, results):
        #print(f"gradients: {results.gradients}")
        new_positions = self.structure.positions - np.multiply(results.gradients.transpose(), self.ss_lambda).transpose()
        self.structure.positions = new_positions
        self.calculator_lock.acquire()
        self.calculator.structure = self.structure
        self.calculator_lock.release()

    # def getIndices(self):
    #     return [x.index for x in self.atoms]

    def getNumAtoms(self):
        return len(self.structure.elements)

    def changeAtomPosition(self, id, pos):
        self.structure.set_position(id, self.ang_to_bohr(pos))
        self.calculator_lock.acquire()
        self.calculator.structure = self.structure
        self.calculator_lock.release()
        self.ss_lambda[id] = 1.0
        # if self.isRunning:
        #     self.orbital_thread.stop()
        #     self.orbital_thread = StoppableThread(target = self.__continuousOrbitals)
        #     self.orbital_thread.daemon = True
        #     self.orbital_thread.start()

    def __get_actual_index(self, electronic_data: ElectronicData, orbital_index: int) -> int:
        i = 0
        while electronic_data.mo[i].occupation >= 1.0:
            i += 1
        return i if orbital_index == -1 else i + 1

if __name__ == "__main__":
    sp = chARpackSparrow()

    elements = ["H", "C", "H", "H", "C", "H", "H", "O", "H"]
    
    positions = [[1.62654, -0.03768, 0.84561],
                 [1.01120, -0.04529, -0.06260],
                 [1.32526, 0.80308, -0.68471],
                 [1.25013, -0.96118, -0.61888],
                 [-0.46208, 0.03063, 0.29470],
                 [-0.75800, -0.82632, 0.93155],
                 [-0.68223, 0.95369, 0.86656],
                 [-1.19813, 0.01810, -0.90725],
                 [-2.11270, 0.06497, -0.66499]]
    print("Initial Positions")
    print(positions)

    sp.setData(positions, elements)
    sp.startContinuousRun()
    time.sleep(20)
    # for i in range(1):
    #     sp.run()

    #     sp.generateOrbitals()
    #     mo_data = sp.getMO()
    #     print(mo_data)
    #     #print(data.shape)
    #     #print(sp.getPositions())
        
    #     # print("POSITIONS")
    #     # pprint(sp.getPositions())
    #     time.sleep(0.5)
