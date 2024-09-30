using IngameDebugConsole;
using UnityEngine;

public class UICanvas : MonoBehaviour
{
    private static UICanvas _singleton;

    public static UICanvas Singleton
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
                Debug.Log($"[{nameof(UICanvas)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    private void Awake()
    {
        Singleton = this;
    }

    public GameObject inspector;
    public GameObject hierarchy;

    private void Start()
    {
        DebugLogConsole.AddCommand("hi", "Toggle Inspector and Hierarchy", toggleInspectorAndHierarchy);
    }

    private void toggleInspectorAndHierarchy()
    {
        inspector.SetActive(!inspector.activeSelf);
        hierarchy.SetActive(!hierarchy.activeSelf);
    }


}
