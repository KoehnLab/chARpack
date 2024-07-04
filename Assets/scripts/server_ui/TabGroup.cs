using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using chARpackColorPalette;

public class TabGroup : MonoBehaviour
{
    public List<tabButton> buttons;
    public List<GameObject> pagesToSwitch;
    public tabButton selectedTab;

    private Color selectedColor = chARpackColors.black;
    private Color hoverColor = chARpackColors.black;
    private Color inactiveColor = chARpackColors.grey;

    public void Subscribe(tabButton button)
    {
        if(buttons == null)
        {
            buttons = new List<tabButton>();
        }
        buttons.Add(button);

        if(button == selectedTab)
        {
            button.text.color = selectedColor;
        }
    }

    public void OnTabSelected(tabButton button)
    {
        selectedTab = button;
        ResetTabColors();
        button.text.color = selectedColor;

        int index = button.transform.GetSiblingIndex();
        for(int i=0; i<pagesToSwitch.Count; i++)
        {
            if (i == index) { pagesToSwitch[i].SetActive(true); }
            else { pagesToSwitch[i].SetActive(false); }
        }
    }
    public void OnTabExit(tabButton button)
    {
        ResetTabColors();
        if (selectedTab == null || button != selectedTab)
        {
            button.text.color = inactiveColor;
        }
    }
    public void OnTabHover(tabButton button)
    {
        ResetTabColors();
        if (selectedTab == null || button != selectedTab)
        {
            button.text.color = hoverColor;
        }
    }

    public void ResetTabColors()
    {
        foreach(tabButton button in buttons)
        {
            if(selectedTab!=null && selectedTab == button) { continue; }
            button.text.color = inactiveColor;
        }
    }
}
