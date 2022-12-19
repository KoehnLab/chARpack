using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtomWorldSpatialCorrection : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        transform.position = LoginData.offsetPos;
        transform.rotation = LoginData.offsetRot;
    }

}
