using TMPro;
using UnityEngine;

public class DebugWindow : MonoBehaviour
{
    TextMeshPro textMesh;
    public GameObject descriptionTextGO;
    public GameObject debugWindow;

    private bool isEnabled = false;

    void OnEnable()
    {
        //debugWindow.SetActive(false);
        textMesh = descriptionTextGO.GetComponent<TextMeshPro>();
        if (!isEnabled)
        {
            Application.logMessageReceived += LogMessage;
            isEnabled = true;
        }
    }

    void OnDisable()
    {
        if (isEnabled)
        {
            Application.logMessageReceived -= LogMessage;
            isEnabled = false;
        }
    }

    public void LogMessage(string message, string stackTrace, LogType type)
    {
        if (textMesh != null)
        {
            textMesh.text += stackTrace + "#" +message + "\n";
        }
        else
        {
            Debug.LogError("[DebugWindow] TextMesh(Pro) is null.");
        }
    }
    public void toggle()
    {
        debugWindow.SetActive(!debugWindow.activeSelf);
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