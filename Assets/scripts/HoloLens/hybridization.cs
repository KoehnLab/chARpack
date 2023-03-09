using TMPro;
using UnityEngine;

public class hybridization : MonoBehaviour
{

    public GameObject value;

    private void Start()
    {
        value.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.curHybrid.ToString();
    }

    public void increase()
    {
        if (GlobalCtrl.Singleton.curHybrid < 6)
        {
            GlobalCtrl.Singleton.curHybrid += 1;
            value.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.curHybrid.ToString();
        }
    }

    public void decrease()
    {
        if (GlobalCtrl.Singleton.curHybrid > 0)
        {
            GlobalCtrl.Singleton.curHybrid -= 1;
            value.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.curHybrid.ToString();
        }
    }

}
