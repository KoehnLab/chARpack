import numpy as np
from ase import Atom, Atoms
from ase.md.langevin import Langevin
from ase.md.verlet import VelocityVerlet
from ase import units
from ase.calculators.emt import EMT
from ase.calculators.lj import LennardJones
from io import StringIO
from ase.io import read
import ase
from apax.md import run_md, ASECalculator
import ase.optimize
import sys
import os
from glob import glob




class ApaxMD:

    def __init__(self, base_dir="", mode="optimizer"):
        if base_dir == "":
            self.base_dir = os.path.dirname(os.path.abspath(__file__))
        else:
            self.base_dir = base_dir

        #sys.path.insert(0,self.base_dir)
        self.mode = mode
        self.atoms = None
        self.mol_id = None
        self.dyn = None
        self.optimizer = None
        self.constraint_atoms = []
        #self.model_path = os.path.join(self.base_dir,"models/etoh")
        self.model_path = os.path.join(self.base_dir,"uncertainty_model/apax_ens")
        self.deleteMetaFiles()

    def setBaseDir(self, base_dir):
        self.base_dir = base_dir
        self.model_path = os.path.join(self.base_dir,"uncertainty_model/apax_ens")
        self.deleteMetaFiles()

    def deleteMetaFiles(self):
        if os.path.isdir(self.base_dir):
            for filename in glob(f"{self.base_dir}/**/*.meta", recursive=True):
                os.remove(filename)

    def setData(self, positions, symbols, indices = None, mol_id = None):
        self.mol_id = mol_id
        self.atoms = Atoms(symbols[0], positions=[positions[0]])
        if (indices):
            for i in range(1, len(symbols)):
                self.atoms.extend(Atom(symbols[i], positions[i], index=indices[i]))
        else:
            for i in range(1, len(symbols)):
                self.atoms.extend(Atom(symbols[i], positions[i]))
        if self.mode == "optimizer":
            self.setupOptimization()
        elif self.mode == "thermostat":
            self.setupSim()
        else: # False
            self.setupOptimization()

    def setupOptimization(self):
        calc = ASECalculator(model_dir=self.model_path)
        self.atoms.calc = calc
        self.optimizer = getattr(ase.optimize, "FIRE")
        self.dyn = self.optimizer(self.atoms)

    def setDataFromXYZ(self, string):
        f = StringIO(string)
        self.atoms = read(f, format='xyz') # or format='extxyz' if its extended XYZ
        self.setupSim()

    def setupSim(self):
        # initialize the apax ase calculator and assign it to the starting structure
        #calc = LennardJones()  # EMT()
        #self.dyn = VelocityVerlet(atoms=self.atoms, timestep=0.5*units.fs)
        calc = ASECalculator(model_dir=self.model_path)
        self.atoms.calc = calc
        self.dyn = Langevin(
            atoms=self.atoms,
            timestep=0.5 * units.fs,
            temperature_K=50,
            friction=0.01 / units.fs)
    
    def run(self, steps = 1):
        self.dyn.run(steps)
        # for i in range(steps):
        #     self.dyn.step()

    def getPositions(self):
        return self.atoms.positions

    def test(self):
        self.atoms.get_chemical_symbols()

    def getResults(self):
        # does not include uncertainty by default
        return self.atoms.calc.results
    
    def fixAtom(self, id, is_fixed):
        if (is_fixed):
            if (id not in self.constraint_atoms):
                self.constraint_atoms.append(id)
        else:
            if (id in self.constraint_atoms):
                self.constraint_atoms.remove(id)

        if len(self.constraint_atoms) > 0:
            constraint = ase.constraints.FixAtoms(indices=[id])
            self.atoms.set_constraint(constraint)
        else:
            self.atoms.set_constraint()
        

    def changeAtomPosition(self, id, pos):
        new_pos = self.atoms.positions
        new_pos[id] = pos
        self.atoms.set_positions(new_pos)

    def getPosZero(self):
        return self.atoms.get_positions()[0]
  
    def getMaxForce(self):
        return np.max(self.atoms.get_forces())




if __name__ == "__main__":
    pos_array = np.asarray([(1.626544, -0.037693, 0.845612),
             (1.011200, -0.045292, -0.062605),
             (1.325261, 0.803088, -0.684698),
             (1.250124, -0.961175, -0.618887),
             (-0.462076, 0.030628, 0.294699),
             (-0.758002, -0.826323, 0.931560),
             (-0.682225, 0.953690, 0.866556),
             (-1.198129, 0.018094, -0.907245),
             (-2.112696, 0.064982, -0.664993)
              ])
    sym_array = ["H", "C", "H", "H", "C", "H", "H", "O", "H"]

    xyz = """9

H          1.62654       -0.03768        0.84561
C          1.01120       -0.04529       -0.06260
H          1.32526        0.80308       -0.68471
H          1.25013       -0.96118       -0.61888
C         -0.46208        0.03063        0.29470
H         -0.75800       -0.82632        0.93155
H         -0.68223        0.95369        0.86656
O         -1.19813        0.01810       -0.90725
H         -2.11270        0.06497       -0.66499
"""
    a_md = ApaxMD()
    a_md.setData(pos_array, sym_array)
    #a_md.setDataFromXYZ(xyz)
    a_md.run()
    print(a_md.atoms.get_positions())
    a_md.fixAtom(0, True)
    a_md.changeAtomPosition(0,[1.626540, -0.037690, -0.89])
    print(a_md.atoms.get_positions())
    a_md.run()
    print(a_md.atoms.get_positions())