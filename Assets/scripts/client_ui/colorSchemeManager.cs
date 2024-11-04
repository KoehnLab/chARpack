using UnityEngine;
using chARpack.ColorPalette;

namespace chARpack
{
    public class colorSchemeManager : MonoBehaviour
    {
        /// <summary>
        /// singleton of colorSchemeManager
        /// </summary>
        private static colorSchemeManager _singleton;
        public static colorSchemeManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"[{nameof(colorSchemeManager)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        private void Awake()
        {
            Singleton = this;
            initColorPalettes();
        }

        private Texture darkBlueSpectrum;
        private Texture turquoiseSpectrum;
        private Texture rainbowSpectrum;
        private Texture goldSpectrum;
        private Texture monochromeSpectrum;
        private Texture violetSpectrum;
        private Texture heatSpectrum;
        private Texture greenSpectrum;

        public Material HolographicBackplateMaterial;
        public Material HolographicBackplateMaterialGrabbed;
        public Material HolographicBackplateMaterialToggle;

        private void initColorPalettes()
        {
            darkBlueSpectrum = (Texture)Resources.Load("textures/DarkBlueGradient");
            turquoiseSpectrum = (Texture)Resources.Load("textures/TurquoiseGradient");
            goldSpectrum = (Texture)Resources.Load("textures/GoldGradient");
            monochromeSpectrum = (Texture)Resources.Load("textures/MonochromeGradient");
            rainbowSpectrum = (Texture)Resources.Load("textures/RainbowGradient");
            violetSpectrum = (Texture)Resources.Load("textures/VioletGradient");
            heatSpectrum = (Texture)Resources.Load("textures/HeatGradient");
            greenSpectrum = (Texture)Resources.Load("textures/GreenGradient");

            setColorPalette(SettingsData.colorScheme);
        }

        public void setColorPalette(ColorScheme color)
        {
            SettingsData.colorScheme = color;
            switch (color)
            {
                case ColorScheme.DARKBLUE:
                    foreach (Material m in new Material[] { HolographicBackplateMaterial, HolographicBackplateMaterialGrabbed, HolographicBackplateMaterialToggle })
                    {
                        m.SetTexture("_IridescentSpectrumMap", darkBlueSpectrum);
                    }
                    ColorPalette.ColorPalette.activeIndicatorColor = chARpackColors.orange;
                    break;
                case ColorScheme.TURQUOISE:
                    foreach (Material m in new Material[] { HolographicBackplateMaterial, HolographicBackplateMaterialGrabbed, HolographicBackplateMaterialToggle })
                    {
                        m.SetTexture("_IridescentSpectrumMap", turquoiseSpectrum);
                    }
                    ColorPalette.ColorPalette.activeIndicatorColor = chARpackColors.orangered;
                    break;
                case ColorScheme.GOLD:
                    foreach (Material m in new Material[] { HolographicBackplateMaterial, HolographicBackplateMaterialGrabbed, HolographicBackplateMaterialToggle })
                    {
                        m.SetTexture("_IridescentSpectrumMap", goldSpectrum);
                    }
                    ColorPalette.ColorPalette.activeIndicatorColor = chARpackColors.lightblue;
                    break;
                case ColorScheme.MONOCHROME:
                    foreach (Material m in new Material[] { HolographicBackplateMaterial, HolographicBackplateMaterialGrabbed, HolographicBackplateMaterialToggle })
                    {
                        m.SetTexture("_IridescentSpectrumMap", monochromeSpectrum);
                    }
                    ColorPalette.ColorPalette.activeIndicatorColor = chARpackColors.white;
                    break;
                case ColorScheme.RAINBOW:
                    foreach (Material m in new Material[] { HolographicBackplateMaterial, HolographicBackplateMaterialGrabbed, HolographicBackplateMaterialToggle })
                    {
                        m.SetTexture("_IridescentSpectrumMap", rainbowSpectrum);
                    }
                    ColorPalette.ColorPalette.activeIndicatorColor = chARpackColors.orange;
                    break;
                case ColorScheme.VIOLET:
                    foreach (Material m in new Material[] { HolographicBackplateMaterial, HolographicBackplateMaterialGrabbed, HolographicBackplateMaterialToggle })
                    {
                        m.SetTexture("_IridescentSpectrumMap", violetSpectrum);
                    }
                    ColorPalette.ColorPalette.activeIndicatorColor = chARpackColors.yelloworange;
                    break;
                case ColorScheme.HEAT:
                    foreach (Material m in new Material[] { HolographicBackplateMaterial, HolographicBackplateMaterialGrabbed, HolographicBackplateMaterialToggle })
                    {
                        m.SetTexture("_IridescentSpectrumMap", heatSpectrum);
                    }
                    ColorPalette.ColorPalette.activeIndicatorColor = chARpackColors.yelloworange;
                    break;
                case ColorScheme.GREEN:
                    foreach (Material m in new Material[] { HolographicBackplateMaterial, HolographicBackplateMaterialGrabbed, HolographicBackplateMaterialToggle })
                    {
                        m.SetTexture("_IridescentSpectrumMap", greenSpectrum);
                    }
                    ColorPalette.ColorPalette.activeIndicatorColor = chARpackColors.gold;
                    break;
            }
        }
    }
}
