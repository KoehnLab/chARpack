using chARpackColorPalette;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ServerSnapTooltip : ServerTooltip
{
    public Button collapseButton;
    public Button snapButton;
    public Molecule mol1;
    public Molecule mol2;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        collapseButton.onClick.AddListener(delegate { resize(); });
    }


    public void resize()
    {
        if (isSmall)
        {
            isSmall = false;
            snapButton.gameObject.SetActive(true);
            infobox.SetActive(true);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - 160);
        }
        else
        {
            isSmall = true;
            snapButton.gameObject.SetActive(false);
            infobox.SetActive(false);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + 160);
        }
    }
}
