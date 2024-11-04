using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace chARpack
{
    public class localizationManager : MonoBehaviour
    {

        /// <summary>
        /// singleton of localizationManager
        /// </summary>
        private static localizationManager _singleton;
        public static localizationManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"[{nameof(localizationManager)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        private Locale currentLocale;

        private void Awake()
        {
            Singleton = this;
            // make sure that numbers are printed with a dot as required by any post-processing with standard software
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            CultureInfo.CurrentUICulture = new CultureInfo("en-US", false);

            currentLocale = LocalizationSettings.SelectedLocale;
        }



        private void Update()
        {
            if (currentLocale != LocalizationSettings.SelectedLocale && GlobalCtrl.Singleton != null)
            {
                GlobalCtrl.Singleton.regenerateTooltips();
                currentLocale = LocalizationSettings.SelectedLocale;
                if (SceneManager.GetActiveScene().name.Equals("MainScene"))
                {
                    appSettings.Singleton.updateVisuals();
                }
            }
        }

        /// <summary>
        /// Gets the appropriate version of given text for the current loacle.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>a localized version of the given text</returns>
        public string GetLocalizedString(string text)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString("My Strings", text);
        }
    }
}
