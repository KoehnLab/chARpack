using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace chARpack
{
    public class AtomSF : MonoBehaviour
    {
        [HideInInspector]
        public Atom atom;
        private Material focus2Dmaterial;
        private static HashSet<AtomSF> registeredFocusComponents = new HashSet<AtomSF>();

        private static int numFoci = 1;

        private bool needsUpdate;

        public void NeedsUpdate()
        {
            needsUpdate = true;
        }

        [SerializeField]
        private Color[] fociColors = new Color[4] { Color.white, Color.red, Color.blue, Color.green };
        public Color[] FociColors
        {
            get { return fociColors; }
            set
            {
                fociColors = value;
                needsUpdate = true;
            }
        }

        private void Start()
        {
            var btn = GetComponent<Button>();
            btn.onClick.AddListener(delegate { selectOnClick(); });
            focus2Dmaterial = Instantiate(Resources.Load<Material>("materials/Focus2DMaterial"));
            GetComponent<Image>().material = focus2Dmaterial;
            registeredFocusComponents.Add(this);
        }

        void Update()
        {
            if (needsUpdate)
            {
                needsUpdate = false;

                UpdateMaterialProperties();
            }
        }

        public static void setNumFoci(int num)
        {
            if (num > 4 || num < 1)
            {
                Debug.LogError("[Atom2D:setNumFoci] Minimum: 1, Maximum: 4.");
                return;
            }
            numFoci = num;
            foreach (var comp in registeredFocusComponents)
            {
                comp.NeedsUpdate();
            }
        }

        public static int getNumFoci()
        {
            return numFoci;
        }

        private void selectOnClick()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                atom.markAtomUI(!atom.isMarked);
            }
            else
            {
                atom.serverFocusByStructure = !atom.serverFocusByStructure;
                atom.serverFocusHighlightUI(!atom.serverFocus);
            }
        }

        private void UpdateMaterialProperties()
        {
            focus2Dmaterial.SetInt("_NumFoci", numFoci);
            focus2Dmaterial.SetColorArray("_FociColors", fociColors);
        }

        private void OnDestroy()
        {
            registeredFocusComponents.Remove(this);
        }
    }
}
