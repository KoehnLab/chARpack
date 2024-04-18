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

    public class ColorPalette
    {
        public Color atomSelectionColor = chARpackColors.yellow;
        public Color singleBondSelectionColor = chARpackColors.orange;
        public Color angleBondSelectionColor = chARpackColors.red;
        public Color torsionBondSelectionColor = chARpackColors.green;
    }

    public class FocusColors
    {
        private static List<Color> availableColors;
        private static int current = 0;


        private static void genColors()
        {
            var input = new List<string>();
            input.Add("#3BBCD9");
            input.Add("#88E8F2");
            input.Add("#F2B705");
            input.Add("#BF9075");
            input.Add("#F24141");

            availableColors = new List<Color>();
            foreach (var col in input)
            {
                Color newCol;
                if (ColorUtility.TryParseHtmlString(col, out newCol))
                {
                    availableColors.Add(newCol);
                }
            }

        }

        public static Color getNext()
        {
            genColors();
            if (current == availableColors.Count)
            {
                current = 0;
            }

            var col = availableColors[current];
            current++;

            return col;
        }

    }
}