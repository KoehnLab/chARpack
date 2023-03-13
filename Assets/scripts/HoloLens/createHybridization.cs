using TMPro;
using UnityEngine;

public class createHybridization : MonoBehaviour
{

    public GameObject valueGO;

    private void Start()
    {
        valueGO.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.curHybrid.ToString();
    }

    public void increase()
    {
        if (GlobalCtrl.Singleton.curHybrid < 6)
        {
            GlobalCtrl.Singleton.curHybrid += 1;
            valueGO.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.curHybrid.ToString();
        }
    }

    public void decrease()
    {
        if (GlobalCtrl.Singleton.curHybrid > 0)
        {
            GlobalCtrl.Singleton.curHybrid -= 1;
            valueGO.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.curHybrid.ToString();
        }
    }

}
