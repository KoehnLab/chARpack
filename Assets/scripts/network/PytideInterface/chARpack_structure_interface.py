from pytidenetworking import message
from pytidenetworking.client import Client
from pytidenetworking.message import Message, create as createMessage
from pytidenetworking.message_base import MessageSendMode
from pytidenetworking.threading.fixedupdatethreads import FixedUpdateThread
from pytidenetworking.transports.udp.udp_client import UDPClient
import time
from chARpack_structure_formula import StructureFormulaGenerator


class chARpackStructureInterface():

    PORT = 9050

    DEVICE_TYPE = 6

    ## Sends
    MESSAGE_SEND_INIT = 5000
    MESSAGE_SEND_STRUCTURE_FORMULA = 5001

    # Receives
    GET_BCAST_MOLECULE = 2007
    GET_ATOM_WORLD = 2005
    GET_SETTINGS = 2024

    GET_REQUEST_STRUCTURE_FORMULA = 6000

    def __init__(self):
        self.client = None
        self.clientUpdater = None


    def connectClient(self):
        udpTransport = UDPClient()
        self.client: Client = Client(udpTransport)
        self.client.connect(("127.0.0.1", self.PORT))

        self.clientUpdater: FixedUpdateThread = FixedUpdateThread(self.client.update)
        self.clientUpdater.start()

        self.client.registerMessageHandler(self.GET_ATOM_WORLD, self.getAtomWorld)
        self.client.registerMessageHandler(self.GET_SETTINGS, self.getSettings)
        self.client.registerMessageHandler(self.GET_BCAST_MOLECULE, self.doNothing)
        self.client.registerMessageHandler(self.GET_REQUEST_STRUCTURE_FORMULA, self.sendStructureFormula)

        time.sleep(0.1)
        self.sendInit()


    def closeConnection(self):
        self.client.disconnect()
        self.clientUpdater.requestClose()


    def sendInit(self):
        msg = message.create(MessageSendMode.Unreliable, self.MESSAGE_SEND_INIT)
        self.client.send(msg)


    def sendStructureFormula(self, message: Message):
        ## Unpack Message
        mol_id = message.getUInt16()
        num_atoms = message.getUInt16()
        atom_positions = []
        for i in range(num_atoms):
            pos = []
            for j in range(3):
                pos.append(message.getFloat())
            atom_positions.append(pos)

        symbols = []
        for i in range(num_atoms):
            symbols.append(message.getString())

        print(atom_positions)
        print(symbols)

        ## Generate structure formula
        sfg = StructureFormulaGenerator()
        svg_content, svg_coordinates = sfg.get_structure_formula(atom_positions, symbols)

        ## Send structure and 2D positions back
        return_msg = createMessage(MessageSendMode.Unreliable, self.MESSAGE_SEND_STRUCTURE_FORMULA)
        return_msg.putString("start")
        ## send start message
        self.client.send(return_msg)

        ## Split svg content
        split_content = svg_content.splitlines()
        nlines = len(split_content)

        #return_msg.putUInt16(nlines)

        for i in range(nlines):
            return_msg = createMessage(MessageSendMode.Unreliable, self.MESSAGE_SEND_STRUCTURE_FORMULA)
            return_msg.putString("svg")
            return_msg.putString(split_content[i])
            self.client.send(return_msg)

        ## Place positions
        return_msg = createMessage(MessageSendMode.Unreliable, self.MESSAGE_SEND_STRUCTURE_FORMULA)
        return_msg.putString("pos")
        return_msg.putUInt16(len(svg_coordinates))
        for i in range(len(svg_coordinates)):
            for coord in atom_positions[i]:
                return_msg.putFloat(coord)

        self.client.send(return_msg)
            
        return_msg = createMessage(MessageSendMode.Unreliable, self.MESSAGE_SEND_STRUCTURE_FORMULA)
        return_msg.putString("end")
        return_msg.putUInt16(mol_id)
        self.client.send(return_msg)
        print("Data sent!")


    def getAtomWorld(self, message: Message):
        print("Got Atom World")


    def getSettings(self, message: Message):
        print("Got Settings")


    def doNothing(self, message: Message):
        print("Got message.")



#client = connectClient()
#client.registerMessageHandler(MESSAGE_SEND_MOLECULE, clientHandleMessage)
