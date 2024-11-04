using TMPro;
using UnityEngine;

namespace chARpack
{
    /// <summary>
    /// This script allows for the manipulation of the hybridization of single atoms.
    /// The hybridization is an integer value between 0 and 6.
    /// The default value is 1.
    /// </summary>
    public class modifyHybridization : MonoBehaviour
    {
        private ushort _hyb;

        public GameObject valueGO;

        public ushort hyb { get => _hyb; set { _hyb = value; valueGO.GetComponent<TextMeshPro>().text = _hyb.ToString(); } }

        private Atom _currentAtom;
        public Atom currentAtom { get => _currentAtom; set { _currentAtom = value; hyb = _currentAtom.m_data.m_hybridization; } }


        /// <summary>
        /// Increases the current hybridization by 1 and applies the change to the current atom.
        /// </summary>
        public void increase()
        {
            if (hyb < 6)
            {
                //Debug.Log("[modifyHybridization:increase] Pressed");
                hyb += 1;
                GlobalCtrl.Singleton.modifyHybridUI(currentAtom, hyb);
            }
        }

        /// <summary>
        /// Decreases the current hybridization by 1 and applies the change to the current atom.
        /// </summary>
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
}
