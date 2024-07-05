using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngineInternal;

public class TransitionManager : MonoBehaviour
{

    public enum SyncMode
    {
        Sync = 0,
        Async = 1
    }

    private static TransitionManager _singleton;

    public static TransitionManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(TransitionManager)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }


    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        if (EventManager.Singleton != null)
        {
            EventManager.Singleton.OnGrabOnScreen += grab;
            EventManager.Singleton.OnReleaseGrabOnScreen += release;
        }
    }


    private bool grabHold = false;
    private void grab(Vector2 ss_coords)
    {
        grabHold = true;
        grabScreenWPos = screenAlignment.Singleton.getCurrentProjectedPos();
    }

    public void release()
    {
        grabHold = false;
        grabScreenWPos = null;
    }

    private Molecule hoverMol;
    public void hover(Vector2 ss_coords)
    {
        var wpos = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(ss_coords.x, ss_coords.y, 0.36f)); // z component is target distance from camera
        Ray ray = new Ray();
        ray.direction = GlobalCtrl.Singleton.currentCamera.transform.forward;
        ray.origin = wpos;

        RaycastHit hit;
        if (Physics.SphereCast(ray, 0.04f, out hit))
        {
            var mol = hit.transform.GetComponentInParent<Molecule>();
            if (mol != null)
            {
                hoverMol = mol;
                hoverMol.Hover(true);
            }
            else
            {
                if (hoverMol != null)
                {
                    hoverMol.Hover(false);
                    hoverMol = null;
                }
            }
        }
        else
        {
            if (hoverMol != null)
            {
                hoverMol.Hover(false);
                hoverMol = null;
            }
        }
    }


    public void initializeTransitionServer(Vector2 ss_coords)
    {
        grabHold = true;
        var wpos = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(ss_coords.x, ss_coords.y, 0.36f)); // z component is target distance from camera



        Ray ray = new Ray();
        ray.direction = GlobalCtrl.Singleton.currentCamera.transform.forward;
        ray.origin = wpos;

        RaycastHit hit;
        if (Physics.SphereCast(ray, 0.04f, out hit))
        {
            var mol = hit.transform.GetComponentInParent<Molecule>();
            if (mol != null)
            {
                Debug.Log("[TransitionManager] Got Mol!");
            }
            else
            {
                Debug.Log("[TransitionManager] Got Something unexpected.");
                return;
            }

            StartCoroutine(moveMolAndTransition(mol, wpos));
        }
    }

    private IEnumerator blinkOnScreen(Vector2 ss_coords, Vector3 wpos)
    {
        // Debug Position
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = Vector3.one * 0.04f;
        cube.GetComponent<Renderer>().material.color = new Color(1f, 0f, 0f, 1f);
        //Debug.Log($"[getGrabOnScreen] Got: {ss_coords}");
        cube.transform.position = wpos;
        //Debug.Log($"[getGrabOnScreen] World pos: {wpos}");
        yield return new WaitForSeconds(0.1f);
        DestroyImmediate(cube);
    }


    public void initializeTransitionClient(Molecule mol)
    {
        grabHold = true;

        StartCoroutine(moveMolToScreenAndTransition(mol));
    }

    private Vector3? grabScreenWPos = null;

    public void getTransitionClient(Molecule mol)
    {
        if (grabScreenWPos != null)
        {
            mol.transform.position = grabScreenWPos.Value;
            StartCoroutine(moveMolToHand(mol));
        }
    }

    public void getTransitionServer(Molecule mol)
    {
        mol.transform.position = GlobalCtrl.Singleton.currentCamera.transform.position + 0.36f * GlobalCtrl.Singleton.currentCamera.transform.forward;
        StartCoroutine(moveMolAway(mol));
    }

    private IEnumerator moveMolAndTransition(Molecule mol, Vector3 pos)
    {
        var startTime = Time.time;
        var duration = 3f;
        var dist = Vector3.Distance(mol.transform.position, pos);
        var dir = (pos - mol.transform.position).normalized;
        while (dist > 0.005f)
        {
            if (!grabHold) yield break;
            dist = Vector3.Distance(mol.transform.position, pos);
            float t = (Time.time - startTime) / duration;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            mol.transform.position += dir * dist_per_step;
            yield return null; // wait for next frame
        }
        EventManager.Singleton.TransitionMolecule(mol);
    }

    private IEnumerator moveMolToScreenAndTransition(Molecule mol)
    {
        var center = screenAlignment.Singleton.getScreenCenter();


        var startTime = Time.time;
        var duration = 3f;
        var dist = Vector3.Distance(mol.transform.position, center);
        var dir = (center - mol.transform.position).normalized;
        while (dist > 0.005f)
        {
            if (!grabHold) yield break;
            var pos = screenAlignment.Singleton.getCurrentProjectedPos();
            if (!screenAlignment.Singleton.contains(pos))
            {
                pos = center;
            }
            dist = Vector3.Distance(mol.transform.position, pos);
            dir = (pos - mol.transform.position).normalized;
            float t = (Time.time - startTime) / duration;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            mol.transform.position += dir * dist_per_step;
            yield return null; // wait for next frame
        }
        EventManager.Singleton.TransitionMolecule(mol);
    }

    private IEnumerator moveMolToHand(Molecule mol)
    {
        var startTime = Time.time;
        var duration = 3f;
        var pos = HandTracking.Singleton.getIndexTip();
        var dist = Vector3.Distance(mol.transform.position, pos);
        while (dist > 0.005f)
        {
            if (!grabHold) yield break;
            pos = HandTracking.Singleton.getIndexTip();
            dist = Vector3.Distance(mol.transform.position, pos);
            float t = (Time.time - startTime) / duration;
            var dir = (pos - mol.transform.position).normalized;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            mol.transform.position += dir * dist_per_step;
            yield return null; // wait for next frame
        }
    }

    private IEnumerator moveMolAway(Molecule mol)
    {
        var startTime = Time.time;
        var duration = 3f;
        var destination = GlobalCtrl.Singleton.currentCamera.transform.position + 2f * GlobalCtrl.Singleton.currentCamera.transform.forward;
        var dist = Vector3.Distance(mol.transform.position, destination);
        while (dist > 0.005f)
        {
            if (!grabHold) yield break;
            dist = Vector3.Distance(mol.transform.position, destination);
            float t = (Time.time - startTime) / duration;
            var dir = (destination - mol.transform.position).normalized;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            mol.transform.position += dir * dist_per_step;
            yield return null; // wait for next frame
        }
    }

}
