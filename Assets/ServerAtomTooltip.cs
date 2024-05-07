using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using chARpackColorPalette;

public class ServerAtomTooltip : MonoBehaviour
{
    public Button collapse_button;
    public TMP_Text TooltipText;
    public Button closeButton;
    public Button deleteButton;
    public Button freezeButton;
    public Button hybridUp;
    public Button hybridDown;
    public TMP_Text currentHybrid;
    public Boolean isSmall = false;
    public GameObject title;
    public GameObject infobox;
    public GameObject hybrid;
    public Canvas UI;
    public Atom linkedAtom;
    public RectTransform rect;
    public Vector3 localPosition = new Vector3(0,0,0);


    private ushort _hyb;


    public ushort hyb { get => _hyb; set { _hyb = value; currentHybrid.text = _hyb.ToString(); } }


    public Atom currentAtom { get => linkedAtom; set { linkedAtom = value; hyb = linkedAtom.m_data.m_hybridization; } }

    public void increase()
    {
        if (hyb < 6)
        {
            Debug.Log("[modifyHybridization:increase] Pressed");
            hyb += 1;
            GlobalCtrl.Singleton.modifyHybridUI(currentAtom, hyb);
        }
    }

    /// <summary>
    /// Decreases the current hybridization by 1 and applies the change to the current atom.
    /// </summary>
    public void decrease()
    {
        if (hyb > 0)
        {
            Debug.Log("[modifyHybridization:decrease] Pressed");
            hyb -= 1;
            GlobalCtrl.Singleton.modifyHybridUI(currentAtom, hyb);
        }
    }

    public void Start()
    {
        hyb = linkedAtom.m_data.m_hybridization;
        currentHybrid.text = hyb.ToString();
        var UIthing = GameObject.Find("UICanvas");
        UI = UIthing.GetComponent<Canvas>();
        this.transform.SetParent(UI.transform);
        collapse_button.onClick.AddListener(delegate { resize();});
        var drag = title.gameObject.AddComponent<Draggable>();
        drag.target = transform;
        rect = transform as RectTransform;
        RectTransform canvasRectTransform = UI.GetComponent<RectTransform>();
        rect.anchorMin= new Vector2(1,0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(1, 0.5f);
        rect.anchoredPosition = new Vector2(0, 0);
        this.transform.localScale = new Vector2(1, 1);
        if(localPosition != new Vector3 (0,0,0))
        {
            rect.localPosition = localPosition;
        }

        
    }

    public void resize() 
    {
        if (isSmall) 
        {
        isSmall = false;
        deleteButton.gameObject.SetActive(true);
        freezeButton.gameObject.SetActive(true);
        hybrid.SetActive(true);
        infobox.SetActive(true);
        rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y -230);
        }
        else 
        {
        isSmall = true;
        deleteButton.gameObject.SetActive(false);
        freezeButton.gameObject.SetActive(false);
        hybrid.SetActive(false);
        infobox.SetActive(false);
        rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y +230);
        }
    }
    public void assignColour(int focus_id)
    {
        title.GetComponent<Image>().color = FocusColors.getColor(focus_id);
    }
}
