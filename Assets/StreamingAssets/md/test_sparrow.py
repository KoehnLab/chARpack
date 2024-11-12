import scine_utilities as su
import scine_sparrow
import numpy as np

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
        self.ss_lambda = 1.0

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

        # Configure calculator
        self.calculator.structure = self.structure
        self.calculator.set_required_properties([su.Property.Gradients])

    def __reInit(self):
        if (self.initialized):
            self.structure = self.calculator.structure
            # Get calculator
            manager = su.core.ModuleManager.get_instance()
            self.calculator = manager.get('calculator', self.method)
            # Configure calculator
            self.calculator.structure = self.structure
            self.calculator.set_required_properties([su.Property.Gradients])


    def run(self, steps = 1):
        results = self.calculator.calculate()
        self.steepestDescent(results)

    def ang_to_bohr(self, pos):
        return np.array(pos) / 0.529177210903

    def bohr_to_ang(self, pos):
        return np.array(pos) * 0.529177210903

    def getPositions(self):
        pos_in_ang = [self.bohr_to_ang(x) for x in self.calculator.positions]
        return pos_in_ang
    
    def steepestDescent(self, results):
        new_positions = []
        for i in range(len(self.structure.elements)):
            pos = self.calculator.structure.positions[i] + self.ss_lambda * results.gradients[i]
            new_positions.append(pos)

        self.calculator.structure.positions = new_positions

    # def getIndices(self):
    #     return [x.index for x in self.atoms]

    def getNumAtoms(self):
        return len(self.calculator.structure.elements)

    def changeAtomPosition(self, id, pos):
        self.calculator.structure.set_position(id, self.ang_to_bohr(pos))




# # Calculate
# results = calculator.calculate()
# print(results.energy)
# print(results.gradients)

# # Update positions (without changing the rest of the molecule)
# new_positions = [[-0.7, 0, 0], [0.9, 0, 0]]
# calculator.positions = new_positions

# # Recalculate
# new_results = calculator.calculate()
# print(new_results.energy)
# print(new_results.gradients)
