using Riptide;
using chARpack.Structs;
using System.Collections.Generic;
using UnityEngine;
using Riptide.Utils;

namespace chARpack
{
    public static class NetworkUtils
    {
        #region cmlData

        public static void deserializeCmlData(Message message, ref byte[] cmlBytes_, ref List<cmlData> cmlWorld_, ushort chunkSize_, bool clearScene = true)
        {
            var state = message.GetString();
            if (state == "start")
            {
                cmlWorld_ = new List<cmlData>();
            }
            else if (state == "end")
            {
                // do the load
                if (clearScene)
                {
                    GlobalCtrl.Singleton.DeleteAll();
                }
                GlobalCtrl.Singleton.createFromCML(cmlWorld_);
                EventManager.Singleton.CmlReceiveCompleted();
            }
            else
            {
                // get rest of message
                var totalLength = message.GetUInt();
                var numPieces = message.GetUShort();
                var currentPieceID = message.GetUShort();
                var currentPiece = message.GetBytes();
                if (currentPieceID == 0)
                {
                    cmlBytes_ = new byte[totalLength];
                    currentPiece.CopyTo(cmlBytes_, 0);
                }
                else if (currentPieceID == numPieces - 1)
                {
                    currentPiece.CopyTo(cmlBytes_, currentPieceID * chunkSize_);
                    cmlWorld_.Add(Serializer.Deserialize<cmlData>(cmlBytes_));
                }
                else
                {
                    currentPiece.CopyTo(cmlBytes_, currentPieceID * chunkSize_);
                }
            }
        }

        public static void serializeCmlData(ushort messageSignature, List<cmlData> data, ushort chunkSize_, bool toServer = false, int toClientID = -1)
        {
            // prepare clients for the messages'
            Message startMessage = Message.Create(MessageSendMode.Reliable, messageSignature);
            startMessage.AddString("start");
            if (toServer)
            {
                NetworkManagerClient.Singleton.Client.Send(startMessage);
            }
            else
            {
                if (toClientID < 0)
                {
                    NetworkManagerServer.Singleton.Server.SendToAll(startMessage);
                }
                else
                {
                    NetworkManagerServer.Singleton.Server.Send(startMessage, (ushort)toClientID);
                }
            }


            // we need all meta data so we do the splitting of the world first
            for (ushort i = 0; i < data.Count; i++)
            {
                var currentCml = data[i];
                var totalBytes = Serializer.Serialize(currentCml);
                uint totalLength = (uint)totalBytes.Length; // first
                ushort rest = (ushort)(totalBytes.Length % chunkSize_);
                ushort numPieces = rest == 0 ? (ushort)(totalBytes.Length / chunkSize_) : (ushort)((totalBytes.Length / chunkSize_) + 1); // second
                                                                                                                                          //
                List<ushort> bytesPerPiece = new List<ushort>();
                for (ushort j = 0; j < (numPieces - 1); j++)
                {
                    bytesPerPiece.Add(chunkSize_);
                }
                if (rest != 0)
                {
                    bytesPerPiece.Add(rest);
                }
                else
                {
                    bytesPerPiece.Add(chunkSize_);
                }

                // create pieces and messages
                for (ushort j = 0; j < numPieces; j++)
                {
                    var currentPieceID = j; // third
                    var piece = totalBytes[..bytesPerPiece[j]]; // forth
                    totalBytes = totalBytes[bytesPerPiece[j]..];
                    Message message = Message.Create(MessageSendMode.Reliable, messageSignature);
                    message.AddString("data");
                    message.AddUInt(totalLength);
                    message.AddUShort(numPieces);
                    message.AddUShort(currentPieceID);
                    message.AddBytes(piece);
                    if (toServer)
                    {
                        NetworkManagerClient.Singleton.Client.Send(message);
                    }
                    else
                    {
                        if (toClientID < 0)
                        {
                            NetworkManagerServer.Singleton.Server.SendToAll(message);
                        }
                        else
                        {
                            NetworkManagerServer.Singleton.Server.Send(message, (ushort)toClientID);
                        }
                    }
                }
            }
            Message endMessage = Message.Create(MessageSendMode.Reliable, messageSignature);
            endMessage.AddString("end");
            if (toServer)
            {
                NetworkManagerClient.Singleton.Client.Send(endMessage);
            }
            else
            {
                if (toClientID < 0)
                {
                    NetworkManagerServer.Singleton.Server.SendToAll(endMessage);
                }
                else
                {
                    NetworkManagerServer.Singleton.Server.Send(endMessage, (ushort)toClientID);
                }
            }
        }

        public static void deserializeStructureData(Message message, ref string svg_content, ref List<Vector2> svg_coords)
        {
            var state = message.GetString();
            if (state == "start")
            {
                svg_content = "";
                svg_coords = new List<Vector2>();
            }
            else if (state == "end")
            {
                var mol_id = message.GetGuid();
                EventManager.Singleton.StructureReceiveCompleted(mol_id);
            }
            else if (state == "svg")
            {
                var line = message.GetString();
                svg_content += line + "\n";
            }
            else // pos
            {
                var num_atoms = message.GetUShort();
                for (int i = 0; i < num_atoms; i++)
                {
                    var x = message.GetFloat();
                    var y = message.GetFloat();
                    svg_coords.Add(new Vector2(x, y));
                    //Debug.Log($"x:{x}   y:{y}");
                }
            }
        }


        public static void serializeGenericObject(ushort messageSignature, sGenericObject data, ushort chunkSize_, bool toServer = false, int toClientID = -1)
        {
            // prepare clients for the messages'
            Message startMessage = Message.Create(MessageSendMode.Reliable, messageSignature);
            startMessage.AddString("start");
            if (toServer)
            {
                NetworkManagerClient.Singleton.Client.Send(startMessage);
            }
            else
            {
                if (toClientID < 0)
                {
                    NetworkManagerServer.Singleton.Server.SendToAll(startMessage);
                }
                else
                {
                    NetworkManagerServer.Singleton.Server.Send(startMessage, (ushort)toClientID);
                }
            }

            var totalBytes = Serializer.Serialize(data);
            uint totalLength = (uint)totalBytes.Length; // first
            ushort rest = (ushort)(totalBytes.Length % chunkSize_);
            ushort numPieces = rest == 0 ? (ushort)(totalBytes.Length / chunkSize_) : (ushort)((totalBytes.Length / chunkSize_) + 1); // second
                                                                                                                                      //
            List<ushort> bytesPerPiece = new List<ushort>();
            for (ushort j = 0; j < (numPieces - 1); j++)
            {
                bytesPerPiece.Add(chunkSize_);
            }
            if (rest != 0)
            {
                bytesPerPiece.Add(rest);
            }
            else
            {
                bytesPerPiece.Add(chunkSize_);
            }

            // create pieces and messages
            for (ushort j = 0; j < numPieces; j++)
            {
                var currentPieceID = j; // third
                var piece = totalBytes[..bytesPerPiece[j]]; // forth
                totalBytes = totalBytes[bytesPerPiece[j]..];
                Message message = Message.Create(MessageSendMode.Reliable, messageSignature);
                message.AddString("data");
                message.AddUInt(totalLength);
                message.AddUShort(numPieces);
                message.AddUShort(currentPieceID);
                message.AddBytes(piece);
                if (toServer)
                {
                    NetworkManagerClient.Singleton.Client.Send(message);
                }
                else
                {
                    if (toClientID < 0)
                    {
                        NetworkManagerServer.Singleton.Server.SendToAll(message);
                    }
                    else
                    {
                        NetworkManagerServer.Singleton.Server.Send(message, (ushort)toClientID);
                    }
                }
            }

            Message endMessage = Message.Create(MessageSendMode.Reliable, messageSignature);
            endMessage.AddString("end");
            if (toServer)
            {
                NetworkManagerClient.Singleton.Client.Send(endMessage);
            }
            else
            {
                if (toClientID < 0)
                {
                    NetworkManagerServer.Singleton.Server.SendToAll(endMessage);
                }
                else
                {
                    NetworkManagerServer.Singleton.Server.Send(endMessage, (ushort)toClientID);
                }
            }
        }

        public static void deserializeGenericObject(Message message, ref byte[] cmlBytes_, ref sGenericObject genericObject_, ushort chunkSize_)
        {
            var state = message.GetString();
            if (state == "start")
            {
                genericObject_ = new sGenericObject();
            }
            else if (state == "end")
            {
                // do the load
                GenericObject.createFromSerialized(genericObject_);
            }
            else
            {
                // get rest of message
                var totalLength = message.GetUInt();
                var numPieces = message.GetUShort();
                var currentChunkID = message.GetUShort();
                var currentChunk = message.GetBytes();
                if (currentChunkID == 0)
                {
                    cmlBytes_ = new byte[totalLength];
                    currentChunk.CopyTo(cmlBytes_, 0);
                }
                else if (currentChunkID == numPieces - 1)
                {
                    currentChunk.CopyTo(cmlBytes_, currentChunkID * chunkSize_);
                    genericObject_ = Serializer.Deserialize<sGenericObject>(cmlBytes_);
                }
                else
                {
                    currentChunk.CopyTo(cmlBytes_, currentChunkID * chunkSize_);
                }
            }
        }


        #endregion
    }
}