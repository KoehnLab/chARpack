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
        HAND_FOLLOW = 0,
        HAND_FIXED = 1,
        CAMERA = 2,
        FRONT_OF_SCREEN = 3
    }

    public enum DesktopTarget
    {
        CENTER_OF_SCREEN = 0,
        HOVER = 1,
        CURSOR_POSITION = 2
    }

    public enum TransitionAnimation
    {
        NONE = 0 << 0,
        SCALE = 1 << 0,
        ROTATION = 1 << 1,
        BOTH = SCALE | ROTATION
    }

    public enum InteractionType
    {
        BUTTON_PRESS = 0,
        CLOSE_GRAB = 1,
        DISTANT_GRAB = 2
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
    private void grab(Vector2 ss_coords, bool distant)
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
        // var wpos = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(ss_coords.x, ss_coords.y, GlobalCtrl.Singleton.currentCamera.nearClipPlane + 0.0001f)); // z component is target distance from camera
        //Ray ray = new Ray();
        //ray.direction = GlobalCtrl.Singleton.currentCamera.transform.forward;
        //ray.origin = wpos;
        // using the forward vector of the camera is only properly working in the middle of the screen
        // better use:
        var ray = GlobalCtrl.Singleton.currentCamera.ScreenPointToRay(new Vector3(ss_coords.x, ss_coords.y, GlobalCtrl.Singleton.currentCamera.nearClipPlane + 0.0001f));

        RaycastHit hit;
        if (Physics.SphereCast(ray, 0.04f, out hit))
        {
            var mol = hit.transform.GetComponentInParent<Molecule>();
            var go = hit.transform.GetComponentInParent<GenericObject>();
            if (mol != null)
            {
                if (hoverMol != mol)
                {
                    if (hoverMol != null) hoverMol.Hover(false);
                    hoverMol = mol;
                    hoverMol.Hover(true);
                }

                if (hoverGenericObject != null)
                {
                    hoverGenericObject.Hover(false);
                    hoverGenericObject = null;
                }
            }
            else if (go != null)
            {
                if (hoverGenericObject != go)
                {
                    if (hoverGenericObject != null) hoverGenericObject.Hover(false);
                    hoverGenericObject = go;
                    hoverGenericObject.Hover(true);
                }

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


    public void initializeTransitionServer(Vector2 ss_coords, InteractionType triggered_by)
    {
        grabHold = true;
        var wpos = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(ss_coords.x, ss_coords.y, GlobalCtrl.Singleton.currentCamera.nearClipPlane + 0.0001f)); // z component is target distance from camera

        // debug blink
        StartCoroutine(blinkOnScreen(ss_coords, wpos));

        //Ray ray = new Ray();
        //ray.direction = GlobalCtrl.Singleton.currentCamera.transform.forward;
        //ray.origin = wpos;
        // using the forward vector of the camera is only properly working in the middle of the screen
        // better use:
        var ray = GlobalCtrl.Singleton.currentCamera.ScreenPointToRay(new Vector3(ss_coords.x, ss_coords.y, GlobalCtrl.Singleton.currentCamera.nearClipPlane + 0.0001f));

        RaycastHit hit;
        if (Physics.SphereCast(ray, 0.04f, out hit))
        {
            Transform trans = null; 
            var mol_test = hit.transform.GetComponentInParent<Molecule>();
            GenericObject go_test = null;
            if (mol_test != null)
            {
                Debug.Log("[initializeTransitionServer] hit Molecule");
                trans = mol_test.transform;
            }
            else
            {
                go_test = hit.transform.GetComponentInParent<GenericObject>();
                if (go_test != null)
                {
                    Debug.Log("[initializeTransitionServer] hit GenericObject");
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
                StartCoroutine(moveAndTransition(trans, wpos, triggered_by));
            }
            else
            {
                if (mol_test != null)
                {
                    EventManager.Singleton.TransitionMolecule(mol_test, triggered_by);
                    return;
                }
                if (go_test != null)
                {
                    EventManager.Singleton.TransitionGenericObject(go_test, triggered_by);
                    return;
                }
            }
        }
    }

    public void initializeTransitionServer(Molecule mol, InteractionType triggered_by)
    {
        var wpos = GlobalCtrl.Singleton.getIdealSpawnPos(mol.transform);
        if (SettingsData.transitionMode == TransitionMode.FULL_3D)
        {
            StartCoroutine(moveAndTransition(mol.transform, wpos, triggered_by));
        }
        else
        {
            EventManager.Singleton.TransitionMolecule(mol, triggered_by);
        }
    }

    public void initializeTransitionServer(GenericObject go, InteractionType triggered_by)
    {
        var wpos = GlobalCtrl.Singleton.getIdealSpawnPos(go.transform);
        if (SettingsData.transitionMode == TransitionMode.FULL_3D)
        {
            StartCoroutine(moveAndTransition(go.transform, wpos, triggered_by));
        }
        else
        {
            EventManager.Singleton.TransitionGenericObject(go, triggered_by);
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


    public void initializeTransitionClient(Transform trans, InteractionType triggered_by)
    {
        grabHold = true;
        //get target size on screen
        // if object is larger than screen it should only take 0.5*screen_hight
        var box = trans.GetComponent<myBoundingBox>();
        var ss_bounds = box.getScreenSpaceBounds();
        var ss_size_y = ss_bounds.w - ss_bounds.y;
        float target_scale_factor = 1.0f;
        if (ss_size_y > 0.5f * SettingsData.serverViewport.y)
        {
            target_scale_factor = SettingsData.serverViewport.y / (2f * ss_size_y);
            Debug.Log($"[initializeTransitionClient] Object larger than screen. Scale factor {target_scale_factor}");
        }

        if (SettingsData.transitionMode == TransitionMode.INSTANT || triggered_by == InteractionType.CLOSE_GRAB)
        {
            trans.localScale *= target_scale_factor;
            trans.position = screenAlignment.Singleton.getScreenCenter();
            var mol = trans.GetComponent<Molecule>();
            if (mol != null)
            {
                EventManager.Singleton.TransitionMolecule(mol, triggered_by);
            }
            else
            {
                var go = trans.GetComponent<GenericObject>();
                EventManager.Singleton.TransitionGenericObject(go, triggered_by);
            }
        }
        else
        {
            if (SettingsData.transitionAnimation == TransitionAnimation.SCALE || SettingsData.transitionAnimation == TransitionAnimation.BOTH)
            {
                Debug.Log("[initializeTransitionClient] Animating scale.");
                StartCoroutine(scaleWhileMoving(trans, trans.localScale.x * target_scale_factor));
            }
            StartCoroutine(moveToScreenAndTransition(trans, triggered_by));
        }
    }

    private Vector3? grabScreenWPos = null;


    public void getMoleculeTransitionClient(Molecule mol, InteractionType triggered_by)
    {
        Debug.Log("[getMoleculeTransitionClient] triggered");
        getTransitionClient(mol.transform, triggered_by);
    }

    public void getGenericObjectTransitionClient(GenericObject go, InteractionType triggered_by)
    {
        Debug.Log("[getGenericObjectTransitionClient] triggered");
        getTransitionClient(go.transform, triggered_by);
    }

    private void getTransitionClient(Transform trans, InteractionType triggered_by)
    {
        if (triggered_by == InteractionType.CLOSE_GRAB)
        {
            StartCoroutine(attachToGrip(trans));
            manualSetGrip(trans, true);
            return;
        }
        if (SettingsData.transitionMode == TransitionMode.INSTANT)
        {
            if (SettingsData.immersiveTarget == ImmersiveTarget.HAND_FIXED || SettingsData.immersiveTarget == ImmersiveTarget.HAND_FIXED)
            {
                var index_pos = HandTracking.Singleton.getIndexTip();
                var proj_index = screenAlignment.Singleton.projectWSPointToScreen(index_pos);
                if (screenAlignment.Singleton.contains(proj_index))
                {
                    trans.position = index_pos;
                }
                else
                {
                    trans.position = GlobalCtrl.Singleton.getCurrentSpawnPos();
                }

            }
            else if (SettingsData.immersiveTarget == ImmersiveTarget.CAMERA)
            {
                trans.position = GlobalCtrl.Singleton.getCurrentSpawnPos();
            }
            else // ImmersiveTarget.FRONT_OF_SCREEN
            {
                var screenSize = screenAlignment.Singleton.getScreenSizeWS();
                float dist_to_move = 0.5f * GlobalCtrl.Singleton.getLongestBBoxEdge(trans);
                dist_to_move = Mathf.Max(dist_to_move, screenSize.y);
                trans.position += dist_to_move * screenAlignment.Singleton.getScreenNormal();
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

            //if (init_by_button)
            //{
            //    Debug.Log($"[getTransitionClient] init by button: true; moving object to user");
            //    StartCoroutine(moveToUser(trans, true));
            //}

            if (SettingsData.immersiveTarget == ImmersiveTarget.HAND_FOLLOW)
            {
                StartCoroutine(moveToHand(trans));
            }
            else if (SettingsData.immersiveTarget == ImmersiveTarget.HAND_FIXED)
            {
                var index_pos = HandTracking.Singleton.getIndexTip();
                StartCoroutine(moveToPos(trans, index_pos));
            }
            else if (SettingsData.immersiveTarget == ImmersiveTarget.CAMERA)
            {
                StartCoroutine(moveToUser(trans));
            }
            else // SettingsData.immersiveTarget == ImmersiveTarget.FRONT_OF_SCREEN
            {
                var screenSize = screenAlignment.Singleton.getScreenSizeWS();
                float dist_to_move = 0.5f * GlobalCtrl.Singleton.getLongestBBoxEdge(trans);
                dist_to_move = Mathf.Max(dist_to_move, screenSize.y);
                var pos = trans.position + dist_to_move * screenAlignment.Singleton.getScreenNormal();
                StartCoroutine(moveToPos(trans, pos));
            }
            if (SettingsData.transitionAnimation == TransitionAnimation.SCALE || SettingsData.transitionAnimation == TransitionAnimation.BOTH)
            {
                Debug.Log("[getTransitionClient] Animating scale.");
                StartCoroutine(scaleWhileMoving(trans, 1f));
            }
        }
    }

    public void getMoleculeTransitionServer(Molecule mol, InteractionType triggered_by)
    {
        getTransitionServer(mol.transform, triggered_by);
    }

    public void getGenericObjectTransitionServer(GenericObject go, InteractionType triggered_by)
    {
        getTransitionServer(go.transform, triggered_by);
    }

    private void getTransitionServer(Transform trans, InteractionType triggered_by)
    {
        if (triggered_by != TransitionManager.InteractionType.CLOSE_GRAB)
        {
            if (current_ss_coords == null)
            {
                trans.position = GlobalCtrl.Singleton.getIdealSpawnPos(trans);
            }
            else
            {
                // TODO extra case for init by button?
                if (SettingsData.desktopTarget == DesktopTarget.CENTER_OF_SCREEN)
                {
                    trans.position = GlobalCtrl.Singleton.getIdealSpawnPos(trans);
                }
                else if (SettingsData.desktopTarget == DesktopTarget.HOVER)
                {
                    trans.position = GlobalCtrl.Singleton.getIdealSpawnPos(trans, current_ss_coords.Value);
                }
                else // cursor position
                {
                    trans.position = GlobalCtrl.Singleton.getIdealSpawnPos(trans, Input.mousePosition);
                }
            }
        }

        if (SettingsData.transitionMode == TransitionManager.TransitionMode.FULL_3D)
        {
            StartCoroutine(moveAway(trans));
        }
    }

    private IEnumerator scaleWhileMoving(Transform trans, float target_scale = 1f, bool override_grab_hold = false)
    {
        var startTime = Time.time;
        var initial_scale = trans.localScale.x;
        Debug.Log($"[scaleWhileMoving] initial scale {initial_scale}");
        while (!trans.localScale.x.approx(target_scale, 0.005f) && trans != null)
        {
            if (SettingsData.requireGrabHold && !override_grab_hold)
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

    private IEnumerator moveAndTransition(Transform trans, Vector3 pos, InteractionType triggered_by)
    {
        var startTime = Time.time;
        var dist = Vector3.Distance(trans.position, pos);
        var dir = (pos - trans.position).normalized;
        var old_pos = trans.position;
        while (dist > 0.005f && Vector3.Dot(pos - old_pos, pos - transform.position) > 0f)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold) yield break;
            }
            dist = Vector3.Distance(trans.position, pos);
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.1f, t);
            old_pos = trans.position;
            trans.position += dir * dist_per_step;
            yield return null; // wait for next frame
        }
        var mol = trans.GetComponent<Molecule>();
        if (mol != null)
        {
            EventManager.Singleton.TransitionMolecule(mol, triggered_by);
        }
        else
        {
            var go = trans.GetComponent<GenericObject>();
            EventManager.Singleton.TransitionGenericObject(go, triggered_by);
        }
    }

    private IEnumerator moveToPos(Transform trans, Vector3 pos, bool override_grab_hold = false)
    {
        var startTime = Time.time;
        var dist = Vector3.Distance(trans.position, pos);
        var dir = (pos - trans.position).normalized;
        var old_pos = trans.position;
        while (dist > 0.005f && Vector3.Dot(pos - old_pos, pos - transform.position) > 0f)
        {
            if (SettingsData.requireGrabHold && !override_grab_hold)
            {
                if (!grabHold) yield break;
            }
            dist = Vector3.Distance(trans.position, pos);
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.1f, t);
            old_pos = trans.position;
            trans.position += dir * dist_per_step;
            yield return null; // wait for next frame
        }
    }

    private IEnumerator moveToScreenAndTransition(Transform trans, InteractionType triggered_by)
    {
        var center = screenAlignment.Singleton.getScreenCenter();

        var startTime = Time.time;
        var dist = Vector3.Distance(trans.position, center);
        var pos = center;
        var old_pos = trans.position;
        while (dist > 0.005f && Vector3.Dot(pos - old_pos, pos - transform.position) > 0f)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold) yield break;
            }

            if (SettingsData.desktopTarget == DesktopTarget.HOVER)
            {
                pos = screenAlignment.Singleton.getCurrentProjectedIndexPos();
                //if (!screenAlignment.Singleton.contains(pos)) ...
            }
            if (SettingsData.desktopTarget == DesktopTarget.CURSOR_POSITION)
            {
                Debug.Log($"[moveToScreenAndTransition] Server mouse position {NetworkManagerClient.Singleton.ServerMousePosition}");
                pos = screenAlignment.Singleton.getWorldSpaceCoords(NetworkManagerClient.Singleton.ServerMousePosition);
            }
            dist = Vector3.Distance(trans.position, pos);
            var dir = (pos - trans.position).normalized;
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            old_pos = trans.position;
            trans.position += dir * dist_per_step;

            yield return null; // wait for next frame
        }
        var mol = trans.GetComponent<Molecule>();
        if (mol != null)
        {
            EventManager.Singleton.TransitionMolecule(mol, triggered_by);
        }
        else
        {
            var go = trans.GetComponent<GenericObject>();
            EventManager.Singleton.TransitionGenericObject(go, triggered_by);
        }
    }

    private IEnumerator moveToHand(Transform trans)
    {
        var startTime = Time.time;
        var pos = HandTracking.Singleton.getIndexTip();
        var old_pos = trans.position;
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
        while (dist > 0.005f && Vector3.Dot(pos - old_pos, pos - transform.position) > 0f)
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
            old_pos = trans.position;
            trans.position += dir * dist_per_step;

            if (SettingsData.transitionAnimation == TransitionAnimation.ROTATION || SettingsData.transitionAnimation == TransitionAnimation.BOTH)
            {
                Debug.Log("[moveToHand] Animating rotation.");
                var head_to_obj = Quaternion.LookRotation(trans.position - GlobalCtrl.Singleton.currentCamera.transform.position);
                trans.rotation = head_to_obj * relQuat;
            }
            yield return null; // wait for next frame
        }
    }

    private IEnumerator moveToUser(Transform trans, bool overwrite_grab_hold = false)
    {
        var startTime = Time.time;
        var pos = GlobalCtrl.Singleton.getCurrentSpawnPos();
        var old_pos = trans.position;
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
        while (dist > 0.005f && Vector3.Dot(pos - old_pos, pos - transform.position) > 0f)
        {
            if (SettingsData.requireGrabHold && !overwrite_grab_hold)
            {
                Debug.Log($"[moveToUser] checking for grab hold");
                if (!grabHold) yield break;
            }
            pos = GlobalCtrl.Singleton.getCurrentSpawnPos();
            dist = Vector3.Distance(trans.position, pos);
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dir = (pos - trans.position).normalized;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            old_pos = trans.position;
            trans.position += dir * dist_per_step;

            if (SettingsData.transitionAnimation == TransitionAnimation.ROTATION || SettingsData.transitionAnimation == TransitionAnimation.BOTH)
            {
                Debug.Log("[moveToUser] Animating rotation.");
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
        var old_pos = trans.position;
        var dist = Vector3.Distance(trans.position, destination);
        while (dist > 0.005f && Vector3.Dot(destination - old_pos, destination - transform.position) > 0f)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold) yield break;
            }
            dist = Vector3.Distance(trans.position, destination);
            float t = (Time.time - startTime) / SettingsData.transitionAnimationDuration;
            var dir = (destination - trans.position).normalized;
            var dist_per_step = Mathf.SmoothStep(0.001f, 0.01f, t);
            old_pos = trans.position;
            trans.position += dir * dist_per_step;
            yield return null; // wait for next frame
        }
    }

    private IEnumerator attachToGrip(Transform trans)
    {
        var isGrabbed = true;
        while (isGrabbed)
        {
            isGrabbed = HandTracking.Singleton.isIndexGrabbed();
            trans.position = HandTracking.Singleton.getIndexTip();
            yield return null;
        }
        manualSetGrip(trans, false);
    }

    private void manualSetGrip(Transform trans, bool isGrabbed)
    {
        var mol = trans.GetComponent<Molecule>();
        if (mol != null)
        {
            mol.isGrabbed = isGrabbed;
        }
        else
        {
            var go = trans.GetComponent<GenericObject>();
            go.isGrabbed = isGrabbed;
        }
    }

}
