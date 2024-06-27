using chARpackColorPalette;
using UnityEngine;

public class SettingsData
{
    [SerializeField] static public ushort bondStiffness = 1;
    [SerializeField] static public float repulsionScale = 0.5f;
    [SerializeField] static public bool forceField = true;
    [SerializeField] static public bool spatialMesh = false;
    [SerializeField] static public bool handMesh = true;
    [SerializeField] static public bool handJoints = false;
    [SerializeField] static public bool handRay = false;
    [SerializeField] static public bool handMenu = true;
    [SerializeField] static public string language = "en";
    [SerializeField] static public bool gazeHighlighting = false;
    [SerializeField] static public bool pointerHighlighting = true;
    [SerializeField] static public bool showAllHighlightsOnClients = true;
    [SerializeField] private static int _highlightColorMap = 0;
    [SerializeField] static public bool rightHandMenu = false;
    [SerializeField] static public ForceField.Method integrationMethod = ForceField.Method.MidPoint;
    [SerializeField] static public float[] timeFactors = new float[] { /*Euler*/0.6f, /*SV*/0.75f, /*RK*/0.25f, /*MP*/0.2f };
    [SerializeField] static public GlobalCtrl.InteractionModes interactionMode = GlobalCtrl.InteractionModes.NORMAL;
    [SerializeField] static public bool[] coop = new bool[] { /*User box*/true, /*User ray*/true };
    [SerializeField] static public bool networkMeasurements = true;
    [SerializeField] static public bool interpolateColors = true;
    [SerializeField] static public bool useAngstrom = true;
    [SerializeField] static public bool licoriceRendering = false;
    [SerializeField] static public ColorScheme colorScheme = ColorScheme.GOLD;
    [SerializeField] static public bool videoPassThrough = true;
    [SerializeField] static public bool autogenerateStructureFormulas = false; // Only for server, not broadcast via network



    public static int highlightColorMap { get => _highlightColorMap; set
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
}
