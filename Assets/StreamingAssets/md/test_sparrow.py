import scine_utilities as su
import scine_sparrow
import numpy as np
import time
import copy
from pprint import pprint
import threading

element_dict = {"H": su.ElementType.H,
                "C": su.ElementType.C,
                "O": su.ElementType.O,
                "N": su.ElementType.N,
                "Cl": su.ElementType.Cl}

available_methods = ['MNDO', 'AM1', 'RM1', 'PM3', 'PM6', 'DFTB0', 'DFTB2', 'DFTB3']

class chARpackSparrow:
    def __init__(self):
        self.structure = su.AtomCollection()
        self.mol_id = None
        self.calculator = None
        self.method = 'PM6'
        self.initialized = False
        self.isRunning = False
        self.ss_lambda = None
        self.ss_beta = 1.0
        self.lock = threading.Lock()
        self.current_positions = None

    def setMethod(self, method):
        if (method == self.method): return
        if (method in available_methods):
            self.method = method
            self.__reInit()


    def setData(self, positions, symbols, indices = None, mol_id = None):
        self.mol_id = mol_id
        self.structure.elements = [element_dict[x] for x in symbols]
        self.current_positions = positions
        pos_in_bohr = [self.ang_to_bohr(x) for x in positions]
        self.structure.positions = pos_in_bohr

        # Get calculator
        manager = su.core.ModuleManager.get_instance()
        self.calculator = manager.get('calculator', self.method)

        # Configure calculator
        self.calculator.structure = self.structure
        self.calculator.set_required_properties([su.Property.Gradients])
        self.ss_lambda = np.ones(len(self.structure.elements))

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


    def run(self):
        results = self.calculator.calculate()
        self.steepestDescent(results)
        np.array([self.bohr_to_ang(x) for x in self.structure.positions], dtype="float32").tolist()

    def runWithLock(self):
        results = self.calculator.calculate()
        self.steepestDescent(results)
        self.lock.acquire()
        self.current_positions = np.array([self.bohr_to_ang(x) for x in self.structure.positions], dtype="float32").tolist()
        self.lock.release()


    def startContinuousRun(self):
        self.isRunning = True
        secondary_thread = threading.Thread(target = self.__continuousRun)
        secondary_thread.daemon = True
        secondary_thread.start()

    def __continuousRun(self):
        while self.isRunning:
            self.runWithLock()

    def stopContinuousRun(self):
        self.isRunning = False

    def ang_to_bohr(self, pos):
        return np.array(pos) * su.BOHR_PER_ANGSTROM

    def bohr_to_ang(self, pos):
        return np.array(pos, dtype="float32") / su.BOHR_PER_ANGSTROM

    def getPositions(self):
        self.lock.acquire()
        pos_in_ang = self.current_positions
        self.lock.release()
        return pos_in_ang
    
    def getPositionOf(self, id):
        return self.bohr_to_ang(self.structure.positions[id])

    def steepestDescent(self, results):
        #print(f"gradients: {results.gradients}")

        new_positions = self.structure.positions - np.multiply(results.gradients.transpose(), self.ss_lambda).transpose()
        self.structure.positions = new_positions
        self.calculator.structure = self.structure

    # def getIndices(self):
    #     return [x.index for x in self.atoms]

    def getNumAtoms(self):
        return len(self.structure.elements)

    def changeAtomPosition(self, id, pos):
        # self.lock.acquire()
        self.structure.set_position(id, self.ang_to_bohr(pos))
        self.calculator.structure = self.structure
        self.ss_lambda[id] = 1.0
        # self.lock.release()


# if __name__ == "__main__":
#     sp = chARpackSparrow()

#     elements = ["H", "C", "H", "H", "C", "H", "H", "O", "H"]
    
#     positions = [[1.62654, -0.03768, 0.84561],
#                  [1.01120, -0.04529, -0.06260],
#                  [1.32526, 0.80308, -0.68471],
#                  [1.25013, -0.96118, -0.61888],
#                  [-0.46208, 0.03063, 0.29470],
#                  [-0.75800, -0.82632, 0.93155],
#                  [-0.68223, 0.95369, 0.86656],
#                  [-1.19813, 0.01810, -0.90725],
#                  [-2.11270, 0.06497, -0.66499]]
#     print("Initial Positions")
#     print(positions)

#     sp.setData(positions, elements)
    
#     #sp.startContinuousRun()
#     for i in range(1000):
#         sp.run()
#         print("POSITIONS")
#         pprint(sp.getPositions())
#         time.sleep(0.5)
