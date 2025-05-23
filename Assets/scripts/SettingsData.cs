using chARpack.ColorPalette;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace chARpack
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SettingsData
    {
        public enum Protocol
        {
            UDP,
            TCP
        }

        public enum DeviceType : ushort
        {
            Unknown = 0,
            AR = 1,
            Mobile = 2,
            PC = 3,
            VR = 4,
            XR = 5
        }

        [JsonProperty] public static ushort bondStiffness = 1;
        [JsonProperty] public static float repulsionScale = 0.5f;
        [JsonProperty] public static bool forceField = true;
        [JsonProperty] public static bool spatialMesh = false;
        [JsonProperty] public static bool handMesh = true;
        [JsonProperty] public static bool handJoints = false;
        [JsonProperty] public static bool handRay = false;
        [JsonProperty] public static bool handMenu = true;
        [JsonProperty] public static string language = "en";
        [JsonProperty] public static bool gazeHighlighting = false;
        [JsonProperty] public static bool pointerHighlighting = true;
        [JsonProperty] public static bool showAllHighlightsOnClients = true;
        private static int _highlightColorMap = 0; // Somehow this is treated as a property
        [JsonProperty] public static bool rightHandMenu = false;
        [JsonProperty] public static ForceField.Method integrationMethod = ForceField.Method.MidPoint;
        [JsonProperty] public static float[] timeFactors = new float[] { /*Euler*/0.6f, /*SV*/0.75f, /*RK*/0.25f, /*MP*/0.2f };
        [JsonProperty] public static GlobalCtrl.InteractionModes interactionMode = GlobalCtrl.InteractionModes.NORMAL;
        [JsonProperty] public static bool[] coop = new bool[] { /*User box*/true, /*User ray*/true };
        [JsonProperty] public static bool networkMeasurements = true;
        [JsonProperty] public static bool interpolateColors = true;
        [JsonProperty] public static bool useAngstrom = true;
        [JsonProperty] public static bool licoriceRendering = false;
        [JsonProperty] public static ColorScheme colorScheme = ColorScheme.GOLD;
        [JsonProperty] public static bool videoPassThrough = true;
        [JsonProperty] public static bool autogenerateStructureFormulas = false; // Only for server, not broadcast via network
        [JsonProperty] static public bool exclusiveFullscreen = false; // Only for server, not broadcast via network
        public static Vector2 serverViewport = new Vector2(1920, 1080);
        [JsonProperty] public static TransitionManager.SyncMode syncMode = TransitionManager.SyncMode.Sync;
        [JsonProperty] public static TransitionManager.TransitionMode transitionMode = TransitionManager.TransitionMode.DESKTOP_2D;
        [JsonProperty] public static TransitionManager.ImmersiveTarget immersiveTarget = TransitionManager.ImmersiveTarget.HAND_FIXED;
        [JsonProperty] public static bool requireGrabHold = true;
        [JsonProperty] public static HandTracking.Handedness handedness = HandTracking.Handedness.Both;
        [JsonProperty] public static TransitionManager.TransitionAnimation transitionAnimation = TransitionManager.TransitionAnimation.BOTH;
        [JsonProperty] public static float transitionAnimationDuration = 1.5f;
        [JsonProperty] public static TransitionManager.DesktopTarget desktopTarget = TransitionManager.DesktopTarget.CENTER_OF_SCREEN;
        [JsonProperty] public static int randomSeed = 1234;
        [JsonProperty] public static TransitionManager.InteractionType allowedTransitionInteractions = TransitionManager.InteractionType.ALL;
        [JsonProperty] public static bool allowThrowing = true;
        [JsonProperty] public static bool hoverGazeAsSelection = true;
        [JsonProperty] public static float defaultMoleculeSize = 0.8f;
        public static Protocol currentNetworkingProtocol = Protocol.TCP;
        [JsonProperty] public static bool UseKabsch = false;
        [JsonProperty] public static bool twoDimensionalMode = false;
        [JsonProperty] public static bool useToonShading = true;

        public static int highlightColorMap
        {
            get => _highlightColorMap; set
            {

                if (StructureFormulaManager.Singleton)
                {
                    StructureFormulaManager.Singleton.setColorMap(value);
                }
                _highlightColorMap = value;
            }
        }

        //TODO: This can probably be implemented more elegantly
        public static void switchIntegrationMethodForward()
        {
            switch (integrationMethod)
            {
                case ForceField.Method.Euler:
                    integrationMethod = ForceField.Method.Verlet;
                    break;
                case ForceField.Method.Verlet:
                    integrationMethod = ForceField.Method.RungeKutta;
                    break;
                case ForceField.Method.RungeKutta:
                    integrationMethod = ForceField.Method.Heun;
                    break;
                case ForceField.Method.Heun:
                    integrationMethod = ForceField.Method.Ralston;
                    break;
                case ForceField.Method.Ralston:
                    integrationMethod = ForceField.Method.SteepestDescent;
                    break;
                case ForceField.Method.SteepestDescent:
                    integrationMethod = ForceField.Method.MidPoint;
                    break;
                default:
                    integrationMethod = ForceField.Method.Euler;
                    break;
            }
        }
        public static void switchIntegrationMethodBackward()
        {
            switch (integrationMethod)
            {
                case ForceField.Method.Euler:
                    integrationMethod = ForceField.Method.MidPoint;
                    break;
                case ForceField.Method.Verlet:
                    integrationMethod = ForceField.Method.Euler;
                    break;
                case ForceField.Method.RungeKutta:
                    integrationMethod = ForceField.Method.Verlet;
                    break;
                case ForceField.Method.Heun:
                    integrationMethod = ForceField.Method.RungeKutta;
                    break;
                case ForceField.Method.Ralston:
                    integrationMethod = ForceField.Method.Heun;
                    break;
                case ForceField.Method.SteepestDescent:
                    integrationMethod = ForceField.Method.Ralston;
                    break;
                default:
                    integrationMethod = ForceField.Method.SteepestDescent;
                    break;
            }
        }


        public static void dumpSettingsToJSON(string path)
        {

            var settingsDataObject = new SettingsData();
            //Convert the Names to Json to make it easier to access when reading it
            //string settingsJson = JsonUtility.ToJson(settingsDataObject);
            string settingsJson = JsonConvert.SerializeObject(settingsDataObject);
            settingsJson = JValue.Parse(settingsJson).ToString(Formatting.Indented); // make it pretty

            //Save the json as file
            File.WriteAllText(path, settingsJson);
        }

        public static void readSettingsFromJSON(string path)
        {
            string file_content;
            if (path.Contains("Resources/"))
            {
                var reduced = path.Split("Resources/")[1];
                var resources_file = Path.Join(Path.GetDirectoryName(reduced), Path.GetFileNameWithoutExtension(reduced));
                var file = Resources.Load<TextAsset>(resources_file);
                file_content = file.text;
            }
            else
            {
                StreamReader reader = new StreamReader(path);
                file_content = reader.ReadToEnd();
                reader.Close();
            }

            var source = JsonConvert.DeserializeObject<JToken>(file_content);

            var destinatonFields = new List<FieldInfo>();
            var type = typeof(SettingsData);
            foreach (var f in type.GetFields())
            {

                var v = f.GetValue(null);
                var hasAttribute = Attribute.IsDefined(f, typeof(JsonIgnoreAttribute));
                if (!hasAttribute)
                {
                    destinatonFields.Add(f);
                }
            }

            foreach (JProperty field in source)
            {
                var destinationField = destinatonFields
                    .SingleOrDefault(p => p.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase));

                object destination_value;
                if (destinationField.FieldType.IsEnum)
                {
                    var element_type = destinationField.FieldType.GetEnumUnderlyingType();
                    var value = ((JValue)field.Value).Value;
                    var cast_value = Convert.ChangeType(value, element_type);
                    if (Enum.IsDefined(destinationField.FieldType, cast_value))
                    {
                        destination_value = Enum.ToObject(destinationField.FieldType, cast_value);
                    }
                    else
                    {
                        destination_value = Enum.GetValues(destinationField.FieldType).GetValue(0);
                    }
                }
                else if (destinationField.FieldType.IsArray)
                {
                    var element_type = destinationField.FieldType.GetElementType();
                    // create array
                    var jarray = (JArray)field.Value;
                    var array = new object[jarray.Count];
                    for (int i = 0; i < jarray.Count; i++)
                    {
                        array[i] = Convert.ChangeType(((JValue)jarray[i]).Value, element_type);
                    }

                    // convert array
                    Array filledArray = Array.CreateInstance(element_type, array.Length);
                    Array.Copy(array, filledArray, array.Length);
                    destination_value = filledArray;
                }
                else
                {
                    var value = ((JValue)field.Value).Value;
                    destination_value = Convert.ChangeType(value, destinationField.FieldType);
                }
                destinationField.SetValue(null, destination_value);
            }

        }
    }
}
