using TMPro;
using UnityEngine;

public class modifyHybridization : MonoBehaviour
{
    private ushort _hyb;

    public GameObject valueGO;

    public ushort hyb { get => _hyb; set { _hyb = value; valueGO.GetComponent<TextMeshPro>().text = _hyb.ToString(); } }

    private Atom _currentAtom;
    public Atom currentAtom { get => _currentAtom; set { _currentAtom = value; hyb = _currentAtom.m_data.m_hybridization;  } }



    public void increase()
    {
        if (hyb < 6)
        {
            //Debug.Log("[modifyHybridization:increase] Pressed");
            hyb += 1;
            GlobalCtrl.Singleton.modifyHybridUI(currentAtom, hyb);
        }
    }

    public void decrease()
    {
        if (hyb > 0)
        {
            //Debug.Log("[modifyHybridization:decrease] Pressed");
            hyb -= 1;
            GlobalCtrl.Singleton.modifyHybridUI(currentAtom, hyb);
        }
    }

}
