using System.Collections;
using UnityEngine;

public class TransitionManager : MonoBehaviour
{

    public enum SyncMode
    {
        Sync = 0,
        Async = 1
    }

    public enum TransitionMode
    {
        FULL_3D = 0,
        DESKTOP_2D = 1,
        INSTANT = 2
    }

    public enum ImmersiveTarget
    {
        HAND = 0,
        CAMERA = 1
    }

    public enum DesktopTarget
    {
        NONE = 0
    }

    public enum TransitionAnimation
    {
        NONE = 0 << 0,
        SCALE = 1 << 0,
        ROTATION = 1 << 1,
        BOTH = SCALE | ROTATION
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


    static public float zDistance = 1f;
    private bool grabHold = false;
    private void grab(Vector2 ss_coords)
    {
        grabHold = true;
        grabScreenWPos = screenAlignment.Singleton.getCurrentProjectedIndexPos();
    }

    public void release()
    {
        grabHold = false;
        grabScreenWPos = null;
    }


    private Molecule hoverMol;
    private GenericObject hoverGenericObject;
    Vector2? current_ss_coords = null;
    public void hover(Vector2 ss_coords)
    {
        current_ss_coords = ss_coords;
        // var wpos = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(ss_coords.x, ss_coords.y, 0.36f)); // z component is target distance from camera
        //Ray ray = new Ray();
        //ray.direction = GlobalCtrl.Singleton.currentCamera.transform.forward;
        //ray.origin = wpos;
        // using the forward vector of the camera is only properly working in the middle of the screen
        // better use:
        var ray = GlobalCtrl.Singleton.currentCamera.ScreenPointToRay(new Vector3(ss_coords.x, ss_coords.y, 0.36f));

        RaycastHit hit;
        if (Physics.SphereCast(ray, 0.04f, out hit))
        {
            var mol = hit.transform.GetComponentInParent<Molecule>();
            var go = hit.transform.GetComponentInParent<GenericObject>();
            if (mol != null)
            {
                hoverMol = mol;
                hoverMol.Hover(true);
                if (hoverGenericObject != null)
                {
                    hoverGenericObject.Hover(false);
                    hoverGenericObject = null;
                }
            }
            else if (go != null)
            {
                hoverGenericObject = go;
                hoverGenericObject.Hover(true);
                if (hoverMol != null)
                {
                    hoverMol.Hover(false);
                    hoverMol = null;
                }
            }
            else // some kind of other object
            {
                if (hoverMol != null)
                {
                    hoverMol.Hover(false);
                    hoverMol = null;
                }
                if (hoverGenericObject != null)
                {
                    hoverGenericObject.Hover(false);
                    hoverGenericObject = null;
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
            if (hoverGenericObject != null)
            {
                hoverGenericObject.Hover(false);
                hoverGenericObject = null;
            }
        }
    }


    public void initializeTransitionServer(Vector2 ss_coords)
    {
        grabHold = true;
        var wpos = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(ss_coords.x, ss_coords.y, 0.36f)); // z component is target distance from camera

        // debug blink
        StartCoroutine(blinkOnScreen(ss_coords, wpos));

        //Ray ray = new Ray();
        //ray.direction = GlobalCtrl.Singleton.currentCamera.transform.forward;
        //ray.origin = wpos;
        // using the forward vector of the camera is only properly working in the middle of the screen
        // better use:
        var ray = GlobalCtrl.Singleton.currentCamera.ScreenPointToRay(new Vector3(ss_coords.x, ss_coords.y, 0.36f));

        RaycastHit hit;
        if (Physics.SphereCast(ray, 0.04f, out hit))
        {
            Transform trans = null; 
            var mol_test = hit.transform.GetComponentInParent<Molecule>();
            GenericObject go_test = null;
            if (mol_test != null)
            {
                trans = mol_test.transform;
            }
            if (mol_test == null) {
                go_test = hit.transform.GetComponentInParent<GenericObject>();
                if (go_test != null)
                {
                    trans = go_test.transform;
                }
                else
                {
                    Debug.Log("[TransitionManager] Got Something unexpected.");
                    return;
                }
            }


            if (SettingsData.transitionMode == TransitionMode.FULL_3D)
            {
                StartCoroutine(moveAndTransition(trans, wpos));
            }
            else
            {
                if (mol_test != null)
                {
                    EventManager.Singleton.TransitionMolecule(mol_test);
                }
                else
                {
                    EventManager.Singleton.TransitionGenericObject(go_test);
                }
            }
        }
    }

    private IEnumerator blinkOnScreen(Vector2 ss_coords, Vector3 wpos)
    {
        // Debug Position
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = Vector3.one * 0.04f;
        cube.GetComponent<Renderer>().material.color = new Color(1f, 0f, 0f, 1f);
        cube.transform.position = wpos;
        //Debug.Log($"[blink] ss: {ss_coords}; w: {wpos}");
        yield return new WaitForSeconds(0.1f);
        DestroyImmediate(cube);
    }


    public void initializeTransitionClient(Transform trans)
    {
        grabHold = true;
        //get target size on screen
        // if object is larger than screen it should only take 0.5*screen_hight
        var box = trans.GetComponent<myBoundingBox>();
        var ss_bounds = box.getScreenSpaceBounds();
        var ss_size_y = ss_bounds.w - ss_bounds.y;
        float target_scale = 1.0f;
        if (ss_size_y > 0.5f * SettingsData.serverViewport.y)
        {
            target_scale = SettingsData.serverViewport.y / (2f * ss_size_y);
            Debug.Log($"[initializeTransitionClient] Object larger than screen. Scale factor {target_scale}");
        }

        if (SettingsData.transitionMode == TransitionMode.INSTANT)
        {
            trans.localScale *= target_scale;
            trans.position = screenAlignment.Singleton.getScreenCenter();
            var mol = trans.GetComponent<Molecule>();
            if (mol != null)
            {
                EventManager.Singleton.TransitionMolecule(mol);
            }
            else
            {
                var go = trans.GetComponent<GenericObject>();
                EventManager.Singleton.TransitionGenericObject(go);
            }
        }
        else
        {
            StartCoroutine(moveToScreenAndTransition(trans));
            if (SettingsData.transitionAnimation == (TransitionAnimation.SCALE | TransitionAnimation.BOTH))
            {
                StartCoroutine(scaleWhileMoving(trans, trans.localScale.x * target_scale));
            }
        }
    }

    private Vector3? grabScreenWPos = null;


    public void getMoleculeTransitionClient(Molecule mol)
    {
        getTransitionClient(mol.transform);
    }

    public void getGenericObjectTransitionClient(GenericObject go)
    {
        getTransitionClient(go.transform);
    }

    private void getTransitionClient(Transform trans)
    {
        if (grabScreenWPos != null)
        {
            if (SettingsData.transitionMode == TransitionMode.INSTANT)
            {
                if (SettingsData.immersiveTarget == ImmersiveTarget.HAND)
                {
                    trans.position = HandTracking.Singleton.getIndexTip();
                }
                else
                {
                    trans.position = GlobalCtrl.Singleton.getCurrentSpawnPos();
                }
            }
            else
            {
                // TODO test if this is necessary
                if (SettingsData.transitionMode == TransitionMode.FULL_3D)
                {
                    // init position different from ss position
                    trans.position = grabScreenWPos.Value;
                }

                if (SettingsData.immersiveTarget == ImmersiveTarget.HAND)
                {
                    StartCoroutine(moveToHand(trans));
                }
                else
                {
                    StartCoroutine(moveToUser(trans));
                }
                if (SettingsData.transitionAnimation == (TransitionAnimation.SCALE | TransitionAnimation.BOTH))
                {
                    StartCoroutine(scaleWhileMoving(trans));
                }
            }
        }
    }

    public void getMoleculeTransitionServer(Molecule mol)
    {
        getTransitionServer(mol.transform);
    }

    public void getGenericObjectTransitionServer(GenericObject go)
    {
        getTransitionServer(go.transform);
    }

    private void getTransitionServer(Transform trans)
    {
        if (current_ss_coords == null)
        {
            trans.position = GlobalCtrl.Singleton.getCurrentSpawnPos();
        }
        else
        {
            trans.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(current_ss_coords.Value.x, current_ss_coords.Value.y, 0.4f));
            Debug.Log($"[getTransitionServer] Setting ss coords: {current_ss_coords.Value.x} {current_ss_coords.Value.y};");
        }

        if (SettingsData.transitionMode == TransitionManager.TransitionMode.FULL_3D)
        {
            StartCoroutine(moveAway(trans));
        }
    }

    private IEnumerator scaleWhileMoving(Transform trans, float target_scale = 1f)
    {
        var startTime = Time.time;
        var initial_scale = trans.localScale.x;
        Debug.Log($"[scaleWhileMoving] initial scale {initial_scale}");
        while (!trans.localScale.x.approx(target_scale, 0.005f) && trans != null)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold)
                {
                    StartCoroutine(scaleAnimation(trans, initial_scale));
                    yield break;
                }
            }
            var elapsed = Time.time - startTime;
            if (elapsed > (0.75f * SettingsData.transitionAnimationDuration))
            {
                trans.localScale = target_scale * Vector3.one;
                yield break;
            }
            float t = elapsed / (0.5f * SettingsData.transitionAnimationDuration);
            var scale_diff = target_scale - trans.localScale.x;
            var scale_per_step = Mathf.SmoothStep(0.0001f, 0.01f, t);
            trans.localScale += Mathf.Sign(scale_diff) * scale_per_step * Vector3.one;
            yield return null;  // wait for next frame
        }
    }

    private IEnumerator scaleAnimation(Transform trans, float target_scale = 1f)
    {
        var startTime = Time.time;
        while (!trans.localScale.x.approx(target_scale, 0.005f) && trans != null)
        {
            var elapsed = Time.time - startTime;
            if (elapsed > (0.5f * SettingsData.transitionAnimationDuration))
            {
                trans.localScale = target_scale * Vector3.one;
                yield break;
            }
            float t = elapsed / (0.25f * SettingsData.transitionAnimationDuration);
            var scale_diff = target_scale - trans.localScale.x;
            var scale_per_step = Mathf.SmoothStep(0.0001f, 0.01f, t);
            trans.localScale += Mathf.Sign(scale_diff) * scale_per_step * Vector3.one;
            yield return null;  // wait for next frame
        }
    }

    private IEnumerator moveAndTransition(Transform trans, Vector3 pos)
    {
        var startTime = Time.time;
        var dist = Vector3.Distance(trans.position, pos);
        var dir = (pos - trans.position).normalized;
        while (dist > 0.005f)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold) yield break;
            }
            dist = Vector3.Distance(trans.position, pos);
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            trans.position += dir * dist_per_step;
            yield return null; // wait for next frame
        }
        var mol = trans.GetComponent<Molecule>();
        if (mol != null)
        {
            EventManager.Singleton.TransitionMolecule(mol);
        }
        else
        {
            var go = trans.GetComponent<GenericObject>();
            EventManager.Singleton.TransitionGenericObject(go);
        }
    }

    private IEnumerator moveToScreenAndTransition(Transform trans)
    {
        var center = screenAlignment.Singleton.getScreenCenter();

        var startTime = Time.time;
        var dist = Vector3.Distance(trans.position, center);
        var dir = (center - trans.position).normalized;
        while (dist > 0.005f)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold) yield break;
            }
            var pos = screenAlignment.Singleton.getCurrentProjectedIndexPos();
            if (!screenAlignment.Singleton.contains(pos))
            {
                pos = center;
            }
            dist = Vector3.Distance(trans.position, pos);
            dir = (pos - trans.position).normalized;
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            trans.position += dir * dist_per_step;

            yield return null; // wait for next frame
        }
        var mol = trans.GetComponent<Molecule>();
        if (mol != null)
        {
            EventManager.Singleton.TransitionMolecule(mol);
        }
        else
        {
            var go = trans.GetComponent<GenericObject>();
            EventManager.Singleton.TransitionGenericObject(go);
        }
    }

    private IEnumerator moveToHand(Transform trans)
    {
        var startTime = Time.time;
        var pos = HandTracking.Singleton.getIndexTip();
        var dist = Vector3.Distance(trans.position, pos);
        var relQuat = Quaternion.identity;
        var mol = trans.GetComponent<Molecule>();
        if (mol != null)
        {
            relQuat = mol.relQuatBeforeTransition;
        }
        else
        {
            var go = trans.GetComponent<GenericObject>();
            relQuat = go.relQuatBeforeTransition;
        }
        while (dist > 0.005f)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold) yield break;
            }
            pos = HandTracking.Singleton.getIndexTip();
            dist = Vector3.Distance(trans.position, pos);
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dir = (pos - trans.position).normalized;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            trans.position += dir * dist_per_step;

            if (SettingsData.transitionAnimation == (TransitionAnimation.ROTATION | TransitionAnimation.BOTH))
            {
                var head_to_obj = Quaternion.LookRotation(trans.position - GlobalCtrl.Singleton.currentCamera.transform.position);
                trans.rotation = head_to_obj * relQuat;
            }
            yield return null; // wait for next frame
        }
    }

    private IEnumerator moveToUser(Transform trans)
    {
        var startTime = Time.time;
        var pos = GlobalCtrl.Singleton.getCurrentSpawnPos();
        var dist = Vector3.Distance(trans.position, pos);
        var relQuat = Quaternion.identity;
        var mol = trans.GetComponent<Molecule>();
        if (mol != null)
        {
            relQuat = mol.relQuatBeforeTransition;
        }
        else
        {
            var go = trans.GetComponent<GenericObject>();
            relQuat = go.relQuatBeforeTransition;
        }
        while (dist > 0.005f)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold) yield break;
            }
            pos = GlobalCtrl.Singleton.getCurrentSpawnPos();
            dist = Vector3.Distance(trans.position, pos);
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dir = (pos - trans.position).normalized;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            trans.position += dir * dist_per_step;

            if (SettingsData.transitionAnimation == (TransitionAnimation.ROTATION | TransitionAnimation.BOTH))
            {
                var head_to_obj = Quaternion.LookRotation(trans.position - GlobalCtrl.Singleton.currentCamera.transform.position);
                trans.rotation = head_to_obj * relQuat;
            }
            yield return null; // wait for next frame
        }
    }

    private IEnumerator moveAway(Transform trans)
    {
        var startTime = Time.time;
        var destination = GlobalCtrl.Singleton.getCurrentSpawnPos();
        var dist = Vector3.Distance(trans.position, destination);
        while (dist > 0.005f)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold) yield break;
            }
            dist = Vector3.Distance(trans.position, destination);
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dir = (destination - trans.position).normalized;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            trans.position += dir * dist_per_step;
            yield return null; // wait for next frame
        }
    }

}
