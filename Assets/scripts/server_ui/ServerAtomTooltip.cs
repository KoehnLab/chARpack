using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace chARpack
{
    public class ServerAtomTooltip : ServerTooltip
    {
        public Button collapseButton;
        public Button deleteButton;
        public Button freezeButton;
        public Button hybridUp;
        public Button hybridDown;
        public Button modify;
        public TMP_Text currentHybrid;
        public GameObject hybrid;
        public Atom linkedAtom;

        private ushort _hyb;


        public ushort hyb { get => _hyb; set { _hyb = value; currentHybrid.text = _hyb.ToString(); } }


        public Atom currentAtom { get => linkedAtom; set { linkedAtom = value; hyb = linkedAtom.m_data.m_hybridization; } }

        public void increase()
        {
            if (hyb < 6)
            {
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
                hyb -= 1;
                GlobalCtrl.Singleton.modifyHybridUI(currentAtom, hyb);
            }
        }


        public override void Start()

        {
            base.Start();

            hyb = linkedAtom.m_data.m_hybridization;
            currentHybrid.text = hyb.ToString();

            collapseButton.onClick.AddListener(delegate { resize(); });

            if (linkedAtom.m_data.m_abbre == "Dummy")
            {
                modify.gameObject.SetActive(true);
                modify.GetComponentInChildren<TextMeshProUGUI>().text = localizationManager.Singleton.GetLocalizedString("ToHydrogen");
                modify.onClick.AddListener(delegate { linkedAtom.toolTipHelperChangeAtom("H"); });
                modify.onClick.AddListener(delegate { updateModifyText(); });
            }
            else if (linkedAtom.m_data.m_abbre == "H")
            {
                modify.gameObject.SetActive(true);
                modify.GetComponentInChildren<TextMeshProUGUI>().text = localizationManager.Singleton.GetLocalizedString("ToDummy");
                modify.onClick.AddListener(delegate { linkedAtom.toolTipHelperChangeAtom("Dummy"); });
                modify.onClick.AddListener(delegate { updateModifyText(); });
            }
            assignColor(focus_id);
        }

        public void resize()
        {
            if (isSmall)
            {
                isSmall = false;
                deleteButton.gameObject.SetActive(true);
                freezeButton.gameObject.SetActive(true);
                hybrid.SetActive(true);
                infobox.SetActive(true);
                rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - 230);
            }
            else
            {
                isSmall = true;
                deleteButton.gameObject.SetActive(false);
                freezeButton.gameObject.SetActive(false);
                hybrid.SetActive(false);
                infobox.SetActive(false);
                rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + 230);
            }
        }

        private void updateModifyText()
        {
            if (linkedAtom.m_data.m_abbre == "Dummy")
            {
                modify.GetComponentInChildren<TextMeshProUGUI>().text = localizationManager.Singleton.GetLocalizedString("ToHydrogen");
                modify.onClick.AddListener(delegate { linkedAtom.toolTipHelperChangeAtom("H"); });
            }
            else if (linkedAtom.m_data.m_abbre == "H")
            {
                modify.GetComponentInChildren<TextMeshProUGUI>().text = localizationManager.Singleton.GetLocalizedString("ToDummy");
                modify.onClick.AddListener(delegate { linkedAtom.toolTipHelperChangeAtom("Dummy"); });
            }
        }
    }
}
