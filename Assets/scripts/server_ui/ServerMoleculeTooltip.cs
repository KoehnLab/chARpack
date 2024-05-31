using chARpackColorPalette;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
public class ServerMoleculeTooltip : MonoBehaviour
{
    public Button collapse_button;
    public TMP_Text ToolTipText;
    public Button closeButton;
    public Button deleteButton;
    public Button freezeButton;
    public Button scaleButton;
    public Button copyButton;
    public Button toggleDummiesButton;
    public RectTransform rect;
    public Molecule linkedMolecule;
    public GameObject userbox;
    public Vector3 localPosition = new Vector3(0, 0, 0);

    public Boolean isSmall = false;
    public GameObject title;
    public GameObject infobox;
    public int focus_id = -1;

    public Canvas UI;
    // Start is called before the first frame update
    void Start()
    {
        var UIthing = GameObject.Find("UICanvas");
        UI = UIthing.GetComponent<Canvas>();
        this.transform.SetParent(UI.transform);
        collapse_button.onClick.AddListener(delegate { resize(); });
        var drag = title.gameObject.AddComponent<Draggable>();
        drag.target = transform;
        rect = transform as RectTransform;
        RectTransform canvasRectTransform = UI.GetComponent<RectTransform>();
        this.transform.localScale = new Vector2(1, 1);
        if (localPosition != new Vector3(0, 0, 0))
        {
            rect.localPosition = localPosition;
        }
        else
        {
            Vector2 save = SpawnManager.Singleton.GetSpawnLocalPosition(rect);
            rect.position = save;
        }
        assignColour(focus_id);
    }

    // Update is called once per frame
    public void resize()
    {
        if (isSmall)
        {
            isSmall = false;
            deleteButton.gameObject.SetActive(true);
            freezeButton.gameObject.SetActive(true);
            copyButton.gameObject.SetActive(true);
            scaleButton.gameObject.SetActive(true);
            infobox.SetActive(true);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - 250);
        }
        else
        {
            isSmall = true;
            deleteButton.gameObject.SetActive(false);
            freezeButton.gameObject.SetActive(false);
            copyButton.gameObject.SetActive(false);
            scaleButton.gameObject.SetActive(false);
            infobox.SetActive(false);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + 250);
        }
    }
    public void assignColour(int focus_id)
    {
        userbox.GetComponent<RawImage>().color = FocusColors.getColor(focus_id);
    }
}
