using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Utilities;

public class AngleMeasurment : MonoBehaviour
{

    public LineRenderer Line;
    public TextMeshPro Label;

    [HideInInspector] public DistanceMeasurment distMeasurment1;
    [HideInInspector] public float distMeasurment1Sign = 1f;
    [HideInInspector] public DistanceMeasurment distMeasurment2;
    [HideInInspector] public float distMeasurment2Sign = 1f;
    [HideInInspector] public Atom originAtom;

    private float angle = 0f;
    private float radius = 0f;

    // Start is called before the first frame update
    void Start()
    {
        Line.positionCount = 10;
        Line.startWidth = 0.002f;
        Line.endWidth = 0.002f;
    }


    void renderCircleSegment(Vector3 start, Vector3 end)
    {
        var theta1 = Mathf.Acos(start.z); // already normalized
        var theta2 = Mathf.Acos(end.z);
        var phi1 = Mathf.Sign(start.y) * Mathf.Acos(start.x / Mathf.Sqrt(Mathf.Pow(start.x, 2) + Mathf.Pow(start.y, 2)));
        var phi2 = Mathf.Sign(end.y) * Mathf.Acos(end.x / Mathf.Sqrt(Mathf.Pow(end.x, 2) + Mathf.Pow(end.y, 2)));
        var deltaTheta = theta1 - theta2;
        var deltaPhi = phi1 - phi2;
        var thetaStep = deltaTheta / Line.positionCount;
        var phiStep = deltaPhi / Line.positionCount;
        for (int i = 0; i < Line.positionCount; i++)
        {
            var x = radius * Mathf.Sin(theta1 + i * thetaStep) * Mathf.Cos(phi1 + i * phiStep) + originAtom.transform.position.x;
            var y = radius * Mathf.Sin(theta1 + i * thetaStep) * Mathf.Sin(phi1 + i * phiStep) + originAtom.transform.position.y;
            var z = radius * Mathf.Cos(theta1 + i * thetaStep) + originAtom.transform.position.z;
            Line.SetPosition(i, new Vector3(x,y,z));
        }
    }

    void renderStaightSegment(Vector3 start, Vector3 end)
    {
        Line.positionCount = 2;
        Line.SetPosition(0, start);
        Line.SetPosition(1, end);
    }

    // Update is called once per frame
    void Update()
    {
        if (distMeasurment1 != null && distMeasurment2 != null && originAtom != null)
        {
            var norm1 = distMeasurment1Sign * distMeasurment1.getNormalizedDirection();
            var norm2 = distMeasurment2Sign * distMeasurment2.getNormalizedDirection();
            radius = 0.3f * Mathf.Min(distMeasurment1.getDistance(), distMeasurment2.getDistance());
            var lineStart = norm1 * radius + originAtom.transform.position;
            var lineEnd = norm2 * radius + originAtom.transform.position;


            angle = Mathf.Acos(Vector3.Dot(norm1, norm2));

            //renderCircleSegment(norm1, norm2);
            renderStaightSegment(lineStart, lineEnd);


            Label.transform.position = norm1 * radius * 1.2f + originAtom.transform.position;
            Label.text = (Mathf.Rad2Deg * angle).ToString("F2") + "°"; // conversion to Angstrom
            Label.transform.forward = GlobalCtrl.Singleton.currentCamera.transform.forward;
        }
    }
}
