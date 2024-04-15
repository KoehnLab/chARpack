using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            if ((maxFocusIDs - available_ids.Count) > 1) increaseNumOutlines();

        }
        return getFocusID(client_id);
    }

    public static void removeClient(ushort client_id)
    {
        if (!focus_ids.ContainsKey(client_id))
        {
            Debug.Log("[FocusManager:removeClient] Cannot remove client.");
        }
        else
        {
            available_ids.Add(focus_ids[client_id]);
            focus_ids.Remove(client_id);
            if ((maxFocusIDs - available_ids.Count) > 1) decreaseNumOutlines();
        }
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

    private static int getNumClients()
    {
        return focus_ids.Count;
    }


    #region num_outlines
    private static int currentNumOutlines_ = 1;
    public static int currentNumOutlines { get => currentNumOutlines_; set { currentNumOutlines_ = value; GlobalCtrl.Singleton.changeNumOutlines(value); } }


    public static void increaseNumOutlines()
    {
        currentNumOutlines = currentNumOutlines + 1;
    }

    public static void decreaseNumOutlines()
    {
        currentNumOutlines = currentNumOutlines - 1;
    }

    public static int getOutlinePosition(int focus_id)
    {
        if (available_ids.Count == 4 - getNumClients())
        {
            return focus_id;
        }
        if (focus_id >= getNumClients())
        {
            if (focus_id + 1 - getNumClients() == 2)
            {
                return focus_id - 2;
            }
            if (focus_id + 1 - getNumClients() == 1)
            {
                return focus_id - 1;
            }
        }
        return -1;
    }

    public static int getMyFocusID()
    {
        if (NetworkManagerClient.Singleton)
        {
            return UserClient.list[NetworkManagerClient.Singleton.Client.Id].highlightFocusID;
        }
        else
        {
            if (NetworkManagerClient.Singleton)
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
