using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class FocusManager
{
    private static int maxFocusIDs = 4;
    private static Dictionary<ushort, int> focus_ids = new Dictionary<ushort, int>();
    private static List<int> available_ids = new List<int> { 0, 1, 2, 3 };
    public enum HighlightType { None = 0, Focus = 1, Grab = 2, Select = 3, ServerFocus = 4 };

    public static int addClient(ushort client_id)
    {
        if (focus_ids.Count > maxFocusIDs)
        {
            Debug.Log("[FocusManager:addClient] Only 4 clients are allowed.");
            return -1;
        }
        else
        {
            focus_ids[client_id] = available_ids[0];
            available_ids.Remove(available_ids[0]);
            if (focus_ids.Count > 1)
            {
                increaseNumOutlines();
                EventManager.Singleton.SetNumOutlines(currentNumOutlines);
            }
        }
        return getFocusID(client_id);
    }

    public static void silentAddClient(ushort client_id, int focus_id)
    {
        focus_ids[client_id] = focus_id;
        available_ids.Remove(focus_id);
    }

    public static void removeClient(ushort client_id)
    {
        if (!focus_ids.ContainsKey(client_id))
        {
            Debug.Log("[FocusManager:removeClient] Cannot remove client.");
        }
        else
        {
            if (focus_ids.Count > 1)
            {
                decreaseNumOutlines();
                EventManager.Singleton.SetNumOutlines(currentNumOutlines);
            }
            available_ids.Add(focus_ids[client_id]);
            available_ids.Sort();
            focus_ids.Remove(client_id);
        }
    }

    public static void silentRemoveClient(ushort client_id)
    {
        focus_ids.Remove(client_id);
        available_ids.Add(focus_ids[client_id]);
        available_ids.Sort();
    }

    private static int getFocusID(ushort client_id)
    {
        if (!focus_ids.ContainsKey(client_id))
        {
            Debug.Log("[FocusManager:getFocusID] Client does not exist.");
            return -1;
        }
        else
        {
            return focus_ids[client_id];
        }
    }

    public static List<int> getForcusIDsInUse()
    {
        return focus_ids.Values.ToList();
    }

    public static List<ushort> getClientIDsInUse()
    {
        return focus_ids.Keys.ToList();
    }

    private static int getNumClients()
    {
        return focus_ids.Count;
    }


    #region num_outlines
    private static int currentNumOutlines_ = 1;
    public static int currentNumOutlines { get => currentNumOutlines_; set { currentNumOutlines_ = value; GlobalCtrl.Singleton.changeNumOutlines(value); } }

    private static int _maxNumOutlines = 4;
    public static int maxNumOutlines { get => _maxNumOutlines; }

    public static void increaseNumOutlines()
    {
        currentNumOutlines = currentNumOutlines + 1;
    }

    public static void decreaseNumOutlines()
    {
        currentNumOutlines = currentNumOutlines - 1;
    }

    public static int getPosInArray(int focus_id)
    {
        var list = focus_ids.Values.ToList();
        if (list.Count < 1) return 0;
        return list.IndexOf(focus_id);
    }

    public static int getMyFocusID()
    {
        if (NetworkManagerClient.Singleton)
        {
            return UserClient.list[NetworkManagerClient.Singleton.Client.Id].highlightFocusID;
        }
        else
        {
            if (NetworkManagerServer.Singleton)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }

    #endregion


}
