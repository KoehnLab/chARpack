using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginData : MonoBehaviour
{
    [SerializeField] static public string ip;
    [SerializeField] static public ushort port = 6665;
    [SerializeField] static public ushort maxConnections = 10;
    [SerializeField] static public ushort discoveryPort = 6664;
    [SerializeField] static public bool normal_mode = true;
    [SerializeField] static public long uniqueID = 123456789;
}
