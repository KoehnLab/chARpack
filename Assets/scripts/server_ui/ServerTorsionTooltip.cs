using UnityEngine;
using UnityEngine.UI;


public class ServerTorsionTooltip : ServerTooltip
{
    public Button collapseButton;
    public Button modifyButton;
    public Bond linkedBond;


    public override void Start()

    {
        base.Start();
        collapseButton.onClick.AddListener(delegate { resize(); });
    }

    // Update is called once per frame
    public void resize()
    {
        if (isSmall)
        {
            isSmall = false;
            modifyButton.gameObject.SetActive(true);
            infobox.SetActive(true);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - 130);
        }
        else
        {
            isSmall = true;
            modifyButton.gameObject.SetActive(false);
            infobox.SetActive(false);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + 130);
        }
    }
}