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
        public static Color structureFormulaNormal = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        public static Color structureFormulaSelect = new Color(1f, 1f, 0f, 0.6f);
        public static Color notEnabledColor = new Color(0.5f, 0.5f, 0.5f, 0f);
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