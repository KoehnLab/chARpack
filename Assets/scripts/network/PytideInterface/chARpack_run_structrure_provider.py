from chARpack_structure_interface import chARpackStructureInterface
import time


# from chARpack_structure_formula import StructureFormulaGenerator
# pos = [[0.9030777812004089, -1.351619005203247, 0.08557255566120148], [0.7067006826400757, -2.384535551071167, -0.20186303555965424], [0.49260082840919495, -1.1663883924484253, 1.0781950950622559], [1.9788206815719604, -1.1763923168182373, 0.0987473726272583], [0.24491608142852783, -0.4077070951461792, -0.9227692484855652], [-0.8308273553848267, -0.5829324126243591, -0.935942530632019], [0.6553932428359985, -0.5929365754127502, -1.9153897762298584], [0.4412941336631775, 0.6252102851867676, -0.6353319883346558]]
# symbols = ['C', 'H', 'H', 'H', 'C', 'H', 'H', 'H']

# print(len(pos))
# print(len(symbols))

# sfg = StructureFormulaGenerator()
# sfg.get_structure_formula(pos, symbols)

# exit()

si = chARpackStructureInterface()


print("Connecting with Unity..")
si.connectClient()

try:
    while True:
        time.sleep(0.025)
except KeyboardInterrupt:
    print('Shutting down.')

si.closeConnection()