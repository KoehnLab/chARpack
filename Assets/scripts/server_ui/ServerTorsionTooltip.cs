using chARpackColorPalette;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Microsoft.MixedReality.Toolkit;

public class ServerTorsionTooltip : MonoBehaviour
{
    public Button collapse_button;
    public TMP_Text ToolTipText;
    public Button closeButton;
    public Button modifyButton;
    public RectTransform rect;
    public Vector3 localPosition = new Vector3 (0,0,0);
    public Molecule linkedMolecule;
    public GameObject userbox;
    

    public Boolean isSmall = false;
    public GameObject title;
    public GameObject infobox;
    // Start is called before the first frame update
    public Canvas UI;
    void Start()
    {
        var UIthing = GameObject.Find("UICanvas");
        UI = UIthing.GetComponent<Canvas>();
        this.transform.SetParent(UI.transform);
        collapse_button.onClick.AddListener(delegate { resize();});
        var drag = title.gameObject.AddComponent<Draggable>();
        drag.target = transform;
        rect = transform as RectTransform;
        RectTransform canvasRectTransform = UI.GetComponent<RectTransform>();
        this.transform.localScale = new Vector2(1, 1);
        if(localPosition != new Vector3 (0,0,0))
        {
            rect.localPosition = localPosition;
        }
        assignColour(linkedMolecule.focus_id_tracker);        
    }

    // Update is called once per frame
    public void resize() 
    {
        if (isSmall) 
        {
        isSmall = false;
        modifyButton.gameObject.SetActive(true);
        infobox.SetActive(true);
        rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y -130);
        }
        else 
        {
        isSmall = true;
        modifyButton.gameObject.SetActive(false);
        infobox.SetActive(false);
        rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y +130);
        }
    }
    public void assignColour(int focus_id)
    {
        userbox.GetComponent<RawImage>().color = FocusColors.getColor(focus_id);
    }
    
}