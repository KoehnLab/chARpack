using System.Collections.Generic;
using UnityEngine;

namespace chARpackColorPalette
{
    public struct chARpackColors
    {
        public static Color orange = new Color(1.0f, 0.5f, 0.0f);
        public static Color yellow = Color.yellow;
        public static Color clear = Color.clear;
        public static Color grey = Color.grey;
        public static Color gray = Color.gray;
        public static Color magenta = Color.magenta;
        public static Color cyan = Color.cyan;
        public static Color red = Color.red;
        public static Color black = Color.black;
        public static Color white = Color.white;
        public static Color blue = Color.blue;
        public static Color green = Color.green;
        public static Color lightblue = new Color(0.3f, 0.7f, 1.0f);
        public static Color violet = new Color(0.4f, 0.0f, 0.8f);
        public static Color darkblue = new Color(0.0f, 0.2f, 0.8f);
        public static Color gold = new Color(1.0f, 0.9f, 0.4f);
        public static Color darkgrey = new Color(0.2f, 0.2f, 0.2f);
        public static Color orangered = new Color(0.9f, 0.3f, 0.1f);
        public static Color yelloworange = new Color(1.0f, 0.7f, 0.0f);
        public static Color lilac = new Color(0.8f, 0.4f, 1.0f);
    }

    public struct ColorPalette
    {
        public static Color atomSelectionColor = chARpackColors.yellow;
        public static Color singleBondSelectionColor = chARpackColors.orange;
        public static Color angleBondSelectionColor = chARpackColors.red;
        public static Color torsionBondSelectionColor = chARpackColors.green;
        public static Color structureFormulaNormal = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        public static Color structureFormulaSelect = new Color(1f, 1f, 0f, 0.6f);
        public static Color notEnabledColor = new Color(0.5f, 0.5f, 0.5f, 0f);
        public static Color defaultIndicatorColor = chARpackColors.yelloworange; // Needed for use in login menu
        public static Color atomGrabColor = chARpackColors.blue;
        public static Color inactiveIndicatorColor = SettingsData.colorScheme == ColorScheme.MONOCHROME ? chARpackColors.darkgrey : chARpackColors.gray;
        public static Color activeIndicatorColor = chARpackColors.yelloworange;
    }

    public enum ColorScheme
    {
        DARKBLUE,
        TURQUOISE,
        GOLD,
        MONOCHROME,
        RAINBOW,
        HEAT,
        VIOLET,
        GREEN
    }
    public struct chARpackColorSchemes
    {
        public static int numberOfColorSchemes = 8; // Needed to switch forward and backward
    }

    public class FocusColors
    {
        private static List<string> availableColors = new List<string> { "#fc8d62", "#8da0cb", "#e78ac3", "#e5c494" };
        private static int current = 0;

        private static string serverFocusColor = "#66c2a5";


        private static Color getNext()
        {
            if (current == availableColors.Count)
            {
                current = 0;
            }

            Color col;
            ColorUtility.TryParseHtmlString(availableColors[current], out col);

            return col;
        }

        public static Color getColor(int id)
        {
            if (id < 0)
            {
                Color col;
                ColorUtility.TryParseHtmlString(serverFocusColor, out col);
                return col;
            }
            else if (id >= availableColors.Count)
            {
                return Color.white;
            }
            else
            {
                Color col;
                ColorUtility.TryParseHtmlString(availableColors[id], out col);
                return col;
            }
        }
    }
}