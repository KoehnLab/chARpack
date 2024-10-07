using UnityEngine;
using UnityEngine.UI;

namespace chARpack
{
    public class ServerScalingSlider : ServerTooltip
    {
        [HideInInspector] public Molecule linkedMolecule;

        public Slider slider;

        public override void Start()

        {
            base.Start();
            closeButton.onClick.AddListener(delegate { Destroy(gameObject); });
        }

        public void resetScale()
        {
            slider.value = SettingsData.defaultMoleculeSize;
        }
    }
}
