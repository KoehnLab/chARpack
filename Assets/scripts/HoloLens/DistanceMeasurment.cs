using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Utilities;

public class DistanceMeasurment : MonoBehaviour
{

    public LineRenderer Line;
    public TextMeshPro Label;

    [HideInInspector] private Atom startAtom;
    [HideInInspector] private Atom endAtom;

    private Vector3 wDirection = Vector3.zero;
    private float dist = 0f;

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
        if (StartAtom != null && EndAtom == null)
        {
            var indexPos = HandTracking.Singleton.getIndexTip();
            Line.SetPosition(0, startAtom.transform.position);
            Line.SetPosition(1, indexPos);
            dist = Vector3.Magnitude(indexPos - startAtom.transform.position);
            Label.transform.position = (indexPos - startAtom.transform.position) / 2f + startAtom.transform.position;
            wDirection = indexPos - startAtom.transform.position;
        }
        else if (StartAtom != null && EndAtom != null)
        {
            Line.SetPosition(0, startAtom.transform.position);
            Line.SetPosition(1, endAtom.transform.position);
            dist = Vector3.Magnitude(endAtom.transform.position - startAtom.transform.position);
            Label.transform.position = (endAtom.transform.position - startAtom.transform.position) / 2f + startAtom.transform.position;
            wDirection = endAtom.transform.position - startAtom.transform.position;
        }
        Label.text = (dist * (1f/ForceField.scalingfactor) * 0.01f * (1f/startAtom.m_molecule.transform.localScale.x)).ToString("F2") + " Å"; // conversion to Angstrom
        Label.transform.forward = GlobalCtrl.Singleton.currentCamera.transform.forward;
    }

    public Vector3 getWeightedDirection()
    {
        return wDirection;
    }

    public float getDistance()
    {
        return dist;
    }

    public Vector3 getNormalizedDirection()
    {
        return Vector3.Normalize(wDirection);
    }

    private void OnDestroy()
    {
        GlobalCtrl.Singleton.deleteAngleMeasurmentsOf(this);
    }

}
