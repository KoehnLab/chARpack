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

    public void initialize(Vector2 ss_coords)
    {

        var wpos = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(ss_coords.x, ss_coords.y, 0.36f)); // z component is target distance from camera

        // Debug Position
        //var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //cube.transform.localScale = Vector3.one * 0.04f;
        //cube.GetComponent<Renderer>().material.color = new Color(1f, 0f, 0f, 1f);
        //Debug.Log($"[getGrabOnScreen] Got: {ss_coords}");
        //cube.transform.position = wpos;
        //Debug.Log($"[getGrabOnScreen] World pos: {wpos}");

        Ray ray = new Ray();
        ray.direction = GlobalCtrl.Singleton.currentCamera.transform.forward;
        ray.origin = wpos;

        // Cast a sphere wrapping character controller 10 meters forward
        // to see if it is about to hit anything.
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

            StartCoroutine(moveMol(mol, wpos));
        }
    }


    int moveStep;
    private IEnumerator moveMol(Molecule mol, Vector3 pos)
    {
        var dist = Vector3.Distance(mol.transform.position, pos);
        var dir = (pos - mol.transform.position).normalized;
        int num_steps = 60 * 3;
        var dist_per_step = dist / (float)num_steps;
        moveStep = 0;
        while (moveStep < num_steps)
        {
            mol.transform.position += dir * dist_per_step;
            yield return null; // wait for next fraem
            moveStep++;
        }
    }

}
