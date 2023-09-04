using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Utilities;

public class Measurment : MonoBehaviour
{

    public LineRenderer Line;
    public TextMeshPro Label;

    [HideInInspector] private Atom startAtom;
    [HideInInspector] private Atom endAtom;

    public Atom StartAtom
    {
        get => startAtom;
        set
        {
            startAtom = value;
            Line.SetPosition(0, value.transform.position);
        }
    }
    public Atom EndAtom
    {
        get => endAtom;
        set
        {
            endAtom = value;
            Line.SetPosition(1, value.transform.position);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Line.positionCount = 2;
        Line.startWidth = 0.002f;
        Line.endWidth = 0.002f;
    }

    // Update is called once per frame
    void Update()
    {
        float dist = 0f;
        if (StartAtom != null && EndAtom == null)
        {
            var indexPos = HandTracking.Singleton.getIndexTip();
            Line.SetPosition(0, startAtom.transform.position);
            Line.SetPosition(1, indexPos);
            dist = Vector3.Magnitude(indexPos - startAtom.transform.position) * (1f / ForceField.scalingfactor);
            Label.transform.position = (indexPos - startAtom.transform.position) / 2f + startAtom.transform.position;
        }
        else if (StartAtom != null && EndAtom != null)
        {
            Line.SetPosition(0, startAtom.transform.position);
            Line.SetPosition(1, endAtom.transform.position);
            dist = Vector3.Magnitude(endAtom.transform.position - startAtom.transform.position) * (1f / ForceField.scalingfactor);
            Label.transform.position = (endAtom.transform.position - startAtom.transform.position) / 2f + startAtom.transform.position;
        }
        Label.text = (dist*0.01f).ToString("F2") + " Å"; // conversion to Angstrom
        Label.transform.forward = GlobalCtrl.Singleton.currentCamera.transform.forward;
    }
}
