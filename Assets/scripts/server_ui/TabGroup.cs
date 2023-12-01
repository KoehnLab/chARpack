using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabGroup : MonoBehaviour
{
    public List<tabButton> buttons;
    public List<GameObject> pagesToSwitch;
    public tabButton selectedTab;

    private Color selectedColor = Color.black;
    private Color hoverColor = Color.black;
    private Color inactiveColor = Color.grey;

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
