using UnityEngine;
using UnityEngine.UI;


public class ServerMoleculeTooltip : ServerTooltip
{
    public Button collapseButton;
    public Button deleteButton;
    public Button freezeButton;
    public Button scaleButton;
    public Button copyButton;
    public Button toggleDummiesButton;
    public Button structureFormulaButton;
    [HideInInspector] public Molecule linkedMolecule;


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
            deleteButton.gameObject.SetActive(true);
            freezeButton.gameObject.SetActive(true);
            copyButton.gameObject.SetActive(true);
            scaleButton.gameObject.SetActive(true);
            toggleDummiesButton.gameObject.SetActive(true);
            structureFormulaButton.gameObject.SetActive(true);
            infobox.SetActive(true);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - 285);
        }
        else
        {
            isSmall = true;
            deleteButton.gameObject.SetActive(false);
            freezeButton.gameObject.SetActive(false);
            copyButton.gameObject.SetActive(false);
            scaleButton.gameObject.SetActive(false);
            toggleDummiesButton.gameObject.SetActive(false);
            structureFormulaButton.gameObject.SetActive(false);
            infobox.SetActive(false);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + 285);
        }
    }
}
