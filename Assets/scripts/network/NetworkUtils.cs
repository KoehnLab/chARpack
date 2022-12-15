using RiptideNetworking;
using StructClass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class NetworkUtils
{

    #region cmlData




    public static void deserializeCmlData(Message message, ref byte[] cmlBytes_, ref List<cmlData> cmlWorld_, ushort chunkSize_, bool clearScene = true)
    {
        var state = message.GetString();
        if (state == "start")
        {
            Debug.Log("[NetworkManagerClient] Receiving atom world");
            cmlWorld_ = new List<cmlData>();
        }
        else if (state == "end")
        {
            if (clearScene)
            {
                GlobalCtrl.Singleton.DeleteAll();
                GlobalCtrl.Singleton.rebuildAtomWorld(cmlWorld_);
            }
            else
            {
                GlobalCtrl.Singleton.rebuildAtomWorld(cmlWorld_, true);
            }
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
        Message startMessage = Message.Create(MessageSendMode.reliable, messageSignature);
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
                Message message = Message.Create(MessageSendMode.reliable, messageSignature);
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
        Message endMessage = Message.Create(MessageSendMode.reliable, messageSignature);
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



    #endregion


}
