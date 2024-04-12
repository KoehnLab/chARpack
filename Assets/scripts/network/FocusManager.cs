using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusManager : MonoBehaviour
{

    private Dictionary<ushort, int> focus_ids;
    private List<int> available_ids;

    private void Start()
    {
        focus_ids = new Dictionary<ushort, int>();
        available_ids = new List<int>{ 0, 1, 2, 3 };

    }

    public void addClient(ushort client_id)
    {
        if (focus_ids.Count > 4)
        {
            Debug.Log("[FocusManager:addClient] Only 4 clients are allowed.");
        }
        else
        {
            focus_ids[client_id] = available_ids[0];
            available_ids.Remove(available_ids[0]);
        }
    }

    public void removeClient(ushort client_id)
    {
        if (!focus_ids.ContainsKey(client_id))
        {
            Debug.Log("[FocusManager:removeClient] Cannot remove client.");
        }
        else
        {
            available_ids.Add(focus_ids[client_id]);
            focus_ids.Remove(client_id);
        }

    }

    private int getFocusID(ushort client_id)
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

    private int getNumClients()
    {
        return focus_ids.Count;
    }



}
