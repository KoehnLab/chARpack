using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;

/// <summary>
/// This class provides tools for dynamically measuring the distance between atoms.
/// </summary>
public class DistanceMeasurement : MonoBehaviour
{
    public static GameObject distMeasurementPrefab;
    public static GameObject angleMeasurementPrefab;
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

    public static bool Create(ushort mol1_id, ushort atom1_id, ushort mol2_id, ushort atom2_id)
    {
        var startMol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol1_id, null);
        var endMol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol2_id, null);
        if (startMol == null || endMol == null) return false;
        var startAtom = startMol.atomList.ElementAtOrNull(atom1_id, null);
        var endAtom = endMol.atomList.ElementAtOrNull(atom2_id, null);
        if (startAtom == null || endAtom == null) return false;

        var distMeasurementGO = Instantiate(distMeasurementPrefab);
        var distMeasurement = distMeasurementGO.GetComponent<DistanceMeasurement>();
        distMeasurement.StartAtom = startAtom;
        distMeasurement.EndAtom = endAtom;
        var otherDistanceMeasurementsStart = GlobalCtrl.Singleton.getDistanceMeasurmentsOf(startAtom);
        var otherDistanceMeasurmentsFromEnd = GlobalCtrl.Singleton.getDistanceMeasurmentsOf(endAtom);


        GlobalCtrl.Singleton.distMeasurementDict[distMeasurement] = new Tuple<Atom, Atom>(startAtom, endAtom);
        if (otherDistanceMeasurementsStart.Count > 0)
        {
            foreach (var m in otherDistanceMeasurementsStart)
            {
                var angleMeasurementGO = Instantiate(angleMeasurementPrefab);
                var angleMeasurement = angleMeasurementGO.GetComponent<AngleMeasurement>();
                angleMeasurement.originAtom = startAtom;
                angleMeasurement.distMeasurement1 = m;
                if (m.StartAtom != startAtom)
                {
                    angleMeasurement.distMeasurement1Sign = -1f;
                }
                angleMeasurement.distMeasurement2 = distMeasurement;
                GlobalCtrl.Singleton.angleMeasurementDict[angleMeasurement] = new Triple<Atom, DistanceMeasurement, DistanceMeasurement>(startAtom, m, distMeasurement);
            }
        }
        if (otherDistanceMeasurmentsFromEnd.Count > 0)
        {
            foreach (var m in otherDistanceMeasurmentsFromEnd)
            {
                var angleMeasurementGO = Instantiate(angleMeasurementPrefab);
                var angleMeasurement = angleMeasurementGO.GetComponent<AngleMeasurement>();
                angleMeasurement.originAtom = endAtom;
                angleMeasurement.distMeasurement1 = m;
                if (m.StartAtom != endAtom)
                {
                    angleMeasurement.distMeasurement1Sign = -1f;
                }
                angleMeasurement.distMeasurement2 = distMeasurement;
                angleMeasurement.distMeasurement2Sign = -1f;
                GlobalCtrl.Singleton.angleMeasurementDict[angleMeasurement] = new Triple<Atom, DistanceMeasurement, DistanceMeasurement>(endAtom, m, distMeasurement);
            }
        }
        return true;
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
            Vector3 indexPos = Vector3.zero;
            if (HandTracking.Singleton)
            {
                indexPos = HandTracking.Singleton.getIndexTip();
            }
            else
            {
                Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f);
                indexPos = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(newPosition) + StartAtom.mouse_offset;
            }
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
        Label.text = (dist * (1f/ForceField.scalingfactor) * 0.01f * (1f/startAtom.m_molecule.transform.localScale.x)).ToString("F2") + " �"; // conversion to Angstrom
        Label.transform.forward = GlobalCtrl.Singleton.currentCamera.transform.forward;
    }

    public Vector3 getWeightedDirection()
    {
        return wDirection;
    }

    /// <summary>
    /// Returns the distance between StartAtom and EndAtom in picometers.
    /// </summary>
    /// <returns>the distance in picometers</returns>
    public float getDistance()
    {
        return dist;
    }

    /// <summary>
    /// Returns the distance between StartAtom and EndAtom in Angstrom.
    /// </summary>
    /// <returns>the distance in Angstrom</returns>
    public float getDistanceInAngstrom()
    {
        return dist * (1f / ForceField.scalingfactor) * 0.01f * (1f / startAtom.m_molecule.transform.localScale.x);
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
