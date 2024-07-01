using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;
using chARpackColorPalette;
using Unity.VisualScripting;


/// <summary>
/// This class contains the functionality for an MRTK window showing the application's debug log.
/// It offers scrollability both by pagination and by touch.
/// </summary>
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
                Destroy(value.gameObject);
            }

        }
    }

    public GameObject debugWindow;
    public GameObject logEntryPrefab;
    public GameObject debugIndicator;

    private bool isEnabled = false;
    private bool showStackTrace = false;

    private List<LogStack> logStack = new List<LogStack>();

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

    /// <summary>
    /// Creates a new entry in the debug log window containing the start
    /// of the debug message (most often to long to show in full).
    /// The entry can be clicked to show the full text in a separate window.
    /// Messages are logged in different colors dependinf on their type 
    /// (informative, warning or error message).
    /// </summary>
    /// <param name="message">the debug log message</param>
    /// <param name="stackTrace">the stack trace of the debug message</param>
    /// <param name="type">the message type (informative, Warning or Error)</param>
    public void LogMessage(string message, string stackTrace, LogType type)
    {

        string colored_message;
        if (type == LogType.Error)
        {
            colored_message = $"<color=#{chARpackColors.red.ToHexString().Substring(0, 6).ToLower()}>{message}</color>";
        }
        else if (type == LogType.Warning)
        {
            colored_message = $"<color=#{chARpackColors.orange.ToHexString().Substring(0, 6).ToLower()}>{message}</color>";
        }
        else
        {
            colored_message = $"<color=#{chARpackColors.white.ToHexString().Substring(0,6).ToLower()}>{message}</color>";
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
    /// <summary>
    /// Toggles visibility of the debug log.
    /// </summary>
    public void toggleVisible()
    {
        debugWindow.SetActive(!debugWindow.activeSelf);
        setVisual(debugIndicator, debugWindow.activeSelf);
        // somehow the renderer list for clipping gets emptied after disable
        updateClipping();
    }

    /// <summary>
    /// Set the indicator's color depending on whether the debug log is on or off.
    /// </summary>
    /// <param name="indicator"></param>
    /// <param name="value"></param>
    public void setVisual(GameObject indicator, bool value)
    {
        if (value)
        {
            indicator.GetComponent<MeshRenderer>().material.color = chARpackColorPalette.ColorPalette.activeIndicatorColor;
        }
        else
        {
            indicator.GetComponent<MeshRenderer>().material.color = chARpackColorPalette.ColorPalette.inactiveIndicatorColor;
        }
    }

    /// <summary>
    /// Toggles whether the stack trace of a message is shown.
    /// The default value is false.
    /// </summary>
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

    /// <summary>
    /// Extends the scrolling functionality by providing a way to
    /// scroll all the way down to the latest message.
    /// </summary>
    public void scrollAllDown()
    {
        // scrolls down by as many elements as there are in the log stack
        // this is usually too much, but ScrollingObjectCollection handles scrolling too far in either direction
        ScrollByTier(logStack.Count);
    }
}