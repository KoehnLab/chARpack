using chARpackColorPalette;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ServerBondTooltip : ServerTooltip
{
    public Button collapse_button;
    public Button deleteButton;
    public Button modifyButton;
    public Bond linkedBond;

    // Start is called before the first frame update
    new void Start()
    {
        base.Start();
        collapse_button.onClick.AddListener(delegate { resize(); });
    }


    public void resize()
    {
        if (isSmall)
        {
            isSmall = false;
            deleteButton.gameObject.SetActive(true);
            modifyButton.gameObject.SetActive(true);
            infobox.SetActive(true);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - 160);
        }
        else
        {
            isSmall = true;
            deleteButton.gameObject.SetActive(false);
            modifyButton.gameObject.SetActive(false);
            infobox.SetActive(false);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + 160);
        }
    }
}
