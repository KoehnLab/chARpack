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
}
