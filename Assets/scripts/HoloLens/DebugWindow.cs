using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using TMPro;
using UnityEngine;

public class DebugWindow : MonoBehaviour
{
    public GameObject gridObjCollectionGO;
    public GameObject debugWindow;
    public GameObject logEntryPrefab;
    public GameObject scrollingObjectCollection;

    private bool isEnabled = false;
    private bool showStackTrace = false;

    private enum Color { red, green, blue, black, white, yellow, orange };

    void Awake()
    {
        debugWindow.SetActive(false);
        if (!isEnabled)
        {
            Application.logMessageReceived += LogMessage;
            isEnabled = true;
        }
    }

    void OnDestroy()
    {
        if (isEnabled)
        {
            Application.logMessageReceived -= LogMessage;
            isEnabled = false;
        }
    }

    public void LogMessage(string message, string stackTrace, LogType type)
    {

        string colored_message;
        if (type == LogType.Error)
        {
            colored_message = String.Format("{0}{1}{2}{3}{4}", "<color=", (Color.red).ToString(), ">", message, "</color>");
        }
        else if (type == LogType.Warning)
        {
            colored_message = String.Format("{0}{1}{2}{3}{4}", "<color=", (Color.orange).ToString(), ">", message, "</color>");
        }
        else
        {
            colored_message = String.Format("{0}{1}{2}{3}{4}", "<color=", (Color.white).ToString(), ">", message, "</color>");
        }

        // create log entry into grid collection
        GameObject newLogEntry = Instantiate(logEntryPrefab, Vector3.zero, Quaternion.identity);
        newLogEntry.GetComponent<showStackTrace>().log_message = colored_message;
        newLogEntry.GetComponent<showStackTrace>().stack_trace = stackTrace;
        newLogEntry.GetComponent<showStackTrace>().log_type = type;
        // By default the SetParent function tries to maintain the same world position the object had before gaining its new parent. The false fixes that
        newLogEntry.transform.SetParent(gridObjCollectionGO.transform, false);


        //IMPORTANT: in scroll collection use the safe AddContent methode ....



        // find how many objects are already active and offset the position
        //newLogEntry.transform.position -= new Vector3(0.0f, 1.0f, 0.0f) * 3.0f * 0.032f * (gridObjCollectionGO.transform.childCount - 1);
        // update collection also repositions all content

        gridObjCollectionGO.GetComponent<GridObjectCollection>().UpdateCollection();
        scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();



    }
    public void toggleVisible()
    {
        debugWindow.SetActive(!debugWindow.activeSelf);
    }
    public void toggleStackTrace()
    {
        showStackTrace = !showStackTrace;
    }

    public void enable()
    {
        if (!isEnabled)
        {
            Application.logMessageReceived += LogMessage;
            isEnabled = true;
        }
    }

    public void disable()
    {
        if (isEnabled)
        {
            Application.logMessageReceived -= LogMessage;
            isEnabled = false;
        }
    }

}