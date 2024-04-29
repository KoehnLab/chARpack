using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class collapseButton : MonoBehaviour
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


    private ushort _hyb;


    public ushort hyb { get => _hyb; set { _hyb = value; currentHybrid.text = _hyb.ToString(); } }

    private Atom _currentAtom;
    public Atom currentAtom { get => _currentAtom; set { _currentAtom = value; hyb = _currentAtom.m_data.m_hybridization; } }

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
        var UIthing = GameObject.Find("UICanvas");
        UI = UIthing.GetComponent<Canvas>();
        this.transform.SetParent(UI.transform);
        collapse_button.onClick.AddListener(delegate { resize();});
        var drag = title.gameObject.AddComponent<Draggable>();
        drag.target = transform;
        hybridUp.onClick.AddListener(delegate { increase(); });
        hybridDown.onClick.AddListener(delegate {  decrease(); });
        
    }

    public void resize() 
    {
        if (isSmall) 
        {
        
        }
        else 
        {
        
        }
    }

}
