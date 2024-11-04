using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace chARpack
{
    /// <summary>
    /// This class offers the functionality of the atom menu.
    /// It features scrollability by pagination.
    /// </summary>
    public class atomMenuScrollable : myScrollObject
    {

        [HideInInspector] public GameObject atomMenuScrollablePrefab;
        [HideInInspector] public GameObject atomEntryPrefab;

        private string[] atomNames = new string[] {
        "C", "O", "F",
        "N", "Cl", "S",
        "Si", "P", "I",
        "Br", "Ca", "B",
        "Na", "Fe", "Zn",
        "Ag", "Au", "Mg",
        "Ti"
    };

        private static atomMenuScrollable _singleton;

        public static atomMenuScrollable Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                {
                    _singleton = value;
                }
                else if (_singleton != value)
                {
                    Debug.Log($"[{nameof(atomMenuScrollable)}] Instance already exists, destroying duplicate!");
                    Destroy(value.gameObject);
                }

            }
        }

        private void Awake()
        {
            Singleton = this;
            // change positioning parameters
            //var radView = scrollParent.GetComponent<RadialView>();
            //radView.MaxDistance = 0.6f;
            //radView.MaxViewDegrees = 20f;
            // add atom buttons
            atomMenuScrollablePrefab = (GameObject)Resources.Load("prefabs/AtomMenuScrollable");
            atomEntryPrefab = (GameObject)Resources.Load("prefabs/AtomButton");
            generateAtomEntries();

        }

        private void Start()
        {
            // also set starting position
            transform.position = GlobalCtrl.Singleton.mainCamera.transform.position + 0.35f * GlobalCtrl.Singleton.mainCamera.transform.forward;
            transform.forward = GlobalCtrl.Singleton.mainCamera.transform.forward;
        }

        /// <summary>
        /// Destroys the atom menu.
        /// Called when pressing the close button.
        /// </summary>
        public void close()
        {
            Destroy(gameObject);
        }
        /// <summary>
        /// Refreshes the menu content.
        /// </summary>
        public void refresh()
        {
            generateAtomEntries();
        }

        /// <summary>
        /// Generates buttons ordered in a GridObjectCollection for the different atom types.
        /// </summary>
        public void generateAtomEntries()
        {
            clearEntries();
            // get old scale
            var oldScale = scrollingObjectCollection.transform.parent.localScale;
            //reset scale 
            scrollingObjectCollection.transform.parent.localScale = Vector3.one;

            foreach (var atom in atomNames)
            {
                var entry = Instantiate(atomEntryPrefab);
                var button = entry.GetComponent<PressableButtonHoloLens2>();
                button.GetComponent<ButtonConfigHelper>().MainLabelText = $"{atom}";
                button.GetComponent<ButtonConfigHelper>().IconStyle = ButtonIconStyle.None;
                button.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.createAtomUI(atom); });
                button.transform.parent = gridObjectCollection.transform;

            }
            // update on collection places all items in order
            gridObjectCollection.GetComponent<GridObjectCollection>().UpdateCollection();
            // update on scoll content makes the list scrollable
            scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
            // update clipping for out of sight entries
            updateClipping();
            // scale after setting everything up
            scrollingObjectCollection.transform.parent.localScale = oldScale;
            // reset rotation
            resetRotation();
        }

    }
}