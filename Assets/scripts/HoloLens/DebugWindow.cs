using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugWindow : myScrollObject
{
    // only needed to count double messages and show the counter
    private class LogStack
    {
        public string message;
        public int count;
        public GameObject entry;

        public void increaseCount()
        {
            count += 1;
            entry.GetComponent<showStackTrace>().log_message = message + $"({count})";
        }

        public LogStack(string message_, GameObject entry_)
        {
            message = message_;
            count = 1;
            entry = entry_;
        }
    }


    private static DebugWindow _singleton;

    public static DebugWindow Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(DebugWindow)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    public GameObject debugWindow;
    public GameObject logEntryPrefab;

    private bool isEnabled = false;
    private bool showStackTrace = false;

    private List<LogStack> logStack = new List<LogStack>();

    private enum Color { red, green, blue, black, white, yellow, orange };

    void Awake()
    {
        Singleton = this;
        debugWindow.SetActive(false);
        if (!isEnabled)
        {
            Application.logMessageReceived += LogMessage;
            isEnabled = true;
        }
        // we will keep the debug alive
        DontDestroyOnLoad(gameObject);
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


        // check for duplication
        for (int i = 0; i < logStack.Count; i++)
        {
            if (colored_message == logStack[i].message)
            {
                logStack[i].increaseCount();
                return;
            }
        }


        // create log entry into grid collection
        GameObject newLogEntry = Instantiate(logEntryPrefab, Vector3.zero, Quaternion.identity);
        newLogEntry.GetComponent<showStackTrace>().log_message = colored_message;
        newLogEntry.GetComponent<showStackTrace>().stack_trace = stackTrace;
        newLogEntry.GetComponent<showStackTrace>().log_type = type;
        // By default the SetParent function tries to maintain the same world position the object had before gaining its new parent. The false fixes that
        //newLogEntry.transform.SetParent(gridObjectCollection.transform, false);
        newLogEntry.transform.parent = gridObjectCollection.transform;
        logStack.Add(new LogStack(colored_message, newLogEntry));

        //IMPORTANT: in scroll collection use the safe AddContent methode ....



        // find how many objects are already active and offset the position
        //newLogEntry.transform.position -= new Vector3(0.0f, 1.0f, 0.0f) * 3.0f * 0.032f * (gridObjCollectionGO.transform.childCount - 1);
        // update collection also repositions all content

        // update on collection places all items in order
        gridObjectCollection.GetComponent<GridObjectCollection>().UpdateCollection();
        // update on scoll content makes the list scrollable
        scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
        // add all renderers of a log entry to the clipping box renderer list to make the buttons disappear when out of bounds
        updateClipping();

        // make sure the log is not rotated
        newLogEntry.transform.localRotation = Quaternion.identity;

    }
    public void toggleVisible()
    {
        debugWindow.SetActive(!debugWindow.activeSelf);
        // somehow the renderer list for clipping gets emptied after disable
        updateClipping();
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

    public void scrollAllDown()
    {
        // scrolls down by as many elements as there are in the log stack
        // this is usually too much, but ScrollingObjectCollection handles scrolling too far in either direction
        ScrollByTier(logStack.Count);
    }
}