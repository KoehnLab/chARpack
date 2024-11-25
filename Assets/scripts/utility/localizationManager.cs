using UnityEngine;
#if CHARPACK_LOCALIZATION
using System.Globalization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
#endif
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

#if CHARPACK_LOCALIZATION
        private Locale currentLocale;

        private void Awake()
        {
            Singleton = this;
            // make sure that numbers are printed with a dot as required by any post-processing with standard software
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            CultureInfo.CurrentUICulture = new CultureInfo("en-US", false);

            currentLocale = LocalizationSettings.SelectedLocale;
        }
#else
        private void Awake()
        {
            Singleton = this;
        }
#endif


#if CHARPACK_LOCALIZATION
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
#endif

        /// <summary>
        /// Gets the appropriate version of given text for the current loacle.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>a localized version of the given text</returns>
        public static string GetLocalizedString(string text)
        {
#if CHARPACK_LOCALIZATION
            return LocalizationSettings.StringDatabase.GetLocalizedString("My Strings", text);
#else
            return text;
#endif
        }

        /// <summary>
        /// Gets the localized version of the atom's element name.
        /// </summary>
        /// <param name="text">the key corresponding to the correct entry in the "Elements" table</param>
        /// <returns>a string with the localized element name</returns>
        public static string GetLocalizedElementName(string text)
        {
#if CHARPACK_LOCALIZATION
            return LocalizationSettings.StringDatabase.GetLocalizedString("Elements", text);
#else
            return text;
#endif
        }
    }
}
