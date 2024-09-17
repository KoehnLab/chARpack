using UnityEngine;
using UnityEngine.UI;


public class ServerSnapTooltip : ServerTooltip
{
    public Button collapseButton;
    public Button snapButton;
    public Button mergeButton;
    public Molecule mol1;
    public Molecule mol2;


    public override void Start()

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
            mergeButton.gameObject.SetActive(true);
            infobox.SetActive(true);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - 160);
        }
        else
        {
            isSmall = true;
            snapButton.gameObject.SetActive(false);
            mergeButton.gameObject.SetActive(false);
            infobox.SetActive(false);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + 160);
        }
    }
}
