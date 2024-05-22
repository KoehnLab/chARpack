using UnityEngine;

public class LoginData
{
    [SerializeField] static public string ip;
    [SerializeField] static public ushort port = 7777;
    [SerializeField] static public ushort maxConnections = 10;
    [SerializeField] static public ushort discoveryPort = 7778;
    [SerializeField] static public bool normal_mode = true;
    [SerializeField] static public long uniqueID = 123456789;
    [SerializeField] static public Vector3 offsetPos = Vector3.zero;
    [SerializeField] static public Quaternion offsetRot = Quaternion.identity;
}
