using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class toggleAllAtomMode : MonoBehaviour
{
    public GameObject globalCtrlGO;
    private GlobalCtrl globalCtrl;

    private void Start()
    {
        globalCtrl = globalCtrlGO.GetComponent<GlobalCtrl>();
    }
    public void toggle()
    {
        globalCtrl.allAtomMode = !globalCtrl.allAtomMode;
    }
}
