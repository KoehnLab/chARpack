using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServerScalingSlider : MonoBehaviour
{
    public Button closeButton;
    [HideInInspector] public RectTransform rect;
    [HideInInspector] public Molecule linkedMolecule;
    public GameObject title;
    public Vector3 localPosition = new Vector3(0, 0, 0);
    [HideInInspector] public Canvas UI;

    // Start is called before the first frame update
    void Start()
    {
        var UIthing = GameObject.Find("UICanvas");
        UI = UIthing.GetComponent<Canvas>();
        transform.SetParent(UI.transform);
        closeButton.onClick.AddListener(delegate { Destroy(gameObject); });
        var drag = title.gameObject.AddComponent<Draggable>();
        drag.target = transform;
        rect = transform as RectTransform;
        RectTransform canvasRectTransform = UI.GetComponent<RectTransform>();
        transform.localScale = new Vector2(1, 1);
        if (localPosition != new Vector3(0, 0, 0))
        {
            rect.localPosition = localPosition;
        }
        else
        {
            Vector2 save = SpawnManager.Singleton.GetSpawnLocalPosition(rect);
            rect.position = save;
        }
    }

    public void resetScale()
    {
        GetComponentInChildren<Slider>().value = 1.0f;
    }
}
