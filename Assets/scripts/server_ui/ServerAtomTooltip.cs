using System;
using chARpackColorPalette;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerAtomTooltip : MonoBehaviour
{
    public Button collapse_button;
    public TMP_Text TooltipText;
    public Button closeButton;
    public Button deleteButton;
    public Button freezeButton;
    public Button hybridUp;
    public Button hybridDown;
    public Button modify;
    public TMP_Text currentHybrid;
    public Boolean isSmall = false;
    public GameObject title;
    public GameObject infobox;
    public GameObject hybrid;
    public Canvas UI;
    public Atom linkedAtom;
    public GameObject userBox;
    public RectTransform rect;
    public Vector3 localPosition = new Vector3(0, 0, 0);
    public int focus_id = -1;


    private ushort _hyb;


    public ushort hyb { get => _hyb; set { _hyb = value; currentHybrid.text = _hyb.ToString(); } }


    public Atom currentAtom { get => linkedAtom; set { linkedAtom = value; hyb = linkedAtom.m_data.m_hybridization; } }

    public void increase()
    {
        if (hyb < 6)
        {
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
        transform.SetParent(UI.transform);
        collapse_button.onClick.AddListener(delegate { resize(); });
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

        if(linkedAtom.m_data.m_abbre == "Dummy")
        {
            modify.gameObject.SetActive(true);
            modify.GetComponentInChildren<TextMeshProUGUI>().text = localizationManager.Singleton.GetLocalizedString("ToHydrogen");
            modify.onClick.AddListener(delegate { linkedAtom.toolTipHelperChangeAtom("H"); });
            modify.onClick.AddListener(delegate { updateModifyText(); });
        }
        else if (linkedAtom.m_data.m_abbre == "H")
        {
            modify.gameObject.SetActive(true);
            modify.GetComponentInChildren<TextMeshProUGUI>().text = localizationManager.Singleton.GetLocalizedString("ToDummy");
            modify.onClick.AddListener(delegate { linkedAtom.toolTipHelperChangeAtom("Dummy"); });
            modify.onClick.AddListener(delegate { updateModifyText(); });
        }
        assignColor();
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
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - 230);
        }
        else
        {
            isSmall = true;
            deleteButton.gameObject.SetActive(false);
            freezeButton.gameObject.SetActive(false);
            hybrid.SetActive(false);
            infobox.SetActive(false);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + 230);
        }
    }
    public void assignColor()
    {
        var colorHolder = FocusColors.getColor(focus_id);
        userBox.GetComponent<RawImage>().color = colorHolder;
    }

    private void updateModifyText()
    {
        if (linkedAtom.m_data.m_abbre == "Dummy")
        {
            modify.GetComponentInChildren<TextMeshProUGUI>().text = localizationManager.Singleton.GetLocalizedString("ToHydrogen");
            modify.onClick.AddListener(delegate { linkedAtom.toolTipHelperChangeAtom("H"); });
        }
        else if (linkedAtom.m_data.m_abbre == "H")
        {
            modify.GetComponentInChildren<TextMeshProUGUI>().text = localizationManager.Singleton.GetLocalizedString("ToDummy");
            modify.onClick.AddListener(delegate { linkedAtom.toolTipHelperChangeAtom("Dummy"); });
        }
    }
}
