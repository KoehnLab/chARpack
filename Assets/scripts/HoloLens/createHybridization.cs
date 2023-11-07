using TMPro;
using UnityEngine;

/// <summary>
/// This script allows for the manipulation of the hybridization
/// used globally for atoms.
/// The hybridization is an integer value between 0 and 6.
/// The default value is 1.
/// </summary>
public class createHybridization : MonoBehaviour
{
    /// <summary>
    /// The GameObject containing the text field to use.
    /// </summary>
    public GameObject valueGO;

    private void Start()
    {
        valueGO.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.curHybrid.ToString();
    }

    /// <summary>
    /// Increases the current hybridization by 1.
    /// </summary>
    public void increase()
    {
        if (GlobalCtrl.Singleton.curHybrid < 6)
        {
            GlobalCtrl.Singleton.curHybrid += 1;
            valueGO.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.curHybrid.ToString();
        }
    }

    /// <summary>
    /// Decreases the current hybridization by 1.
    /// </summary>
    public void decrease()
    {
        if (GlobalCtrl.Singleton.curHybrid > 0)
        {
            GlobalCtrl.Singleton.curHybrid -= 1;
            valueGO.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.curHybrid.ToString();
        }
    }

}
