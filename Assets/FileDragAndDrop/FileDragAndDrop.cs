using System.Collections.Generic;
using UnityEngine;
using B83.Win32;
using UnityEngine.Events;

#if  UNITY_EDITOR_WIN
using UnityEditor;
#endif

public class FileDragAndDrop : MonoBehaviour
{
    public delegate void FileDropAction(string[] files);
    public static event FileDropAction OnFileDrop;
    public static void FilesDrop(string[] files)
    {
        OnFileDrop?.Invoke(files);
    }

    public string[] m_filesDropped;
    private string[] m_currentPathsHold;

    public void Update()
    {

#if  UNITY_EDITOR_WIN
        string[] paths = DragAndDrop.paths;

        if (paths != null && m_currentPathsHold != null && paths.Length == 0 && m_currentPathsHold.Length > 0)
        {

            m_filesDropped = m_currentPathsHold;
            FilesDrop(m_filesDropped);
        }
        m_currentPathsHold = paths;
#endif
    }

    void Awake()
    {
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;
    }
    void OnDestroy()
    {
        UnityDragAndDropHook.UninstallHook();
    }

    void OnFiles(List<string> aFiles, POINT aPos)
    {
        m_filesDropped = aFiles.ToArray();
        FilesDrop(m_filesDropped);

    }


}