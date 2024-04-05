from pytidenetworking import message
from pytidenetworking.client import Client
from pytidenetworking.message import Message, create as createMessage
from pytidenetworking.message_base import MessageSendMode
from pytidenetworking.threading.fixedupdatethreads import FixedUpdateThread
from pytidenetworking.transports.udp.udp_client import UDPClient
import time


class chARpackSimulationInterface():

    PORT = 9050

    DEVICE_TYPE = 6

    MESSAGE_SEND_INIT = 3000
    MESSAGE_SEND_MOLECULE = 3001
    MESSAGE_SEND_MOLECULE_UPDATE = 3002

    GET_BCAST_MOLECULE = 2007
    GET_ATOM_WORLD = 2005
    GET_SETTINGS = 2024

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


    def sendMolecule(self, mol_id, atom_positions, symbols):
        assert(len(atom_positions) == len(symbols))

        msg = message.create(MessageSendMode.Unreliable, self.MESSAGE_SEND_MOLECULE)
        msg.putUInt16(mol_id)
        msg.putUInt16(len(atom_positions))
        for i in range(len(atom_positions)):
            msg.putUInt16(i)
            msg.putString(symbols[i])
            for coord in atom_positions[i]:
                msg.putFloat(coord)

        print("Message created. Sending data...")
        self.client.send(msg)
        print("Data sent!")


    def updateMolecule(self, mol_id, atom_positions):
        msg = message.create(MessageSendMode.Unreliable, self.MESSAGE_SEND_MOLECULE_UPDATE)
        msg.putUInt16(mol_id)
        msg.putUInt16(len(atom_positions))
        for i in range(len(atom_positions)):
            msg.putUInt16(i)
            for coord in atom_positions[i]:
                msg.putFloat(coord)
        self.client.send(msg)


    def getAtomWorld(self, message: Message):
        print("Got Atom World")
    

    def getSettings(self, message: Message):
        print("Got Settings")


    def doNothing(self, message: Message):
        print("Got message.")



#client = connectClient()
#client.registerMessageHandler(MESSAGE_SEND_MOLECULE, clientHandleMessage)
