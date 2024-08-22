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

    [System.Flags]
    public enum TransitionAnimation
    {
        NONE = 0,
        SCALE = 1 << 0,
        ROTATION = 1 << 1,
        BOTH = SCALE | ROTATION
    }

    [System.Flags]
    public enum InteractionType
    {
        NONE = 0,
        BUTTON_PRESS = 1 << 0,
        CLOSE_GRAB = 1 << 1,
        DISTANT_GRAB = 1 << 2,
        ONSCREEN_PULL = 1 << 3,
        THROW = 1 << 4,
        ALL = BUTTON_PRESS | CLOSE_GRAB | DISTANT_GRAB | ONSCREEN_PULL | THROW
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

    AudioClip doTransition;
    AudioClip getTransition;
    AudioClip moveToTransitionClip;
    AudioClip moveFromTransitionClip;

    private void Start()
    {
        doTransition = Resources.Load<AudioClip>("audio/wine_short");
        getTransition = Resources.Load<AudioClip>("audio/reverse_wine_short");
        moveToTransitionClip = Resources.Load<AudioClip>("audio/wine_loop");
        moveFromTransitionClip = Resources.Load<AudioClip>("audio/reverse_wine_loop");
        if (EventManager.Singleton != null)
        {
            EventManager.Singleton.OnTransitionGrab += grab;
            EventManager.Singleton.OnReleaseTransitionGrab += release;
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

    private bool transitionOnCooldown = false;
    IEnumerator startCooldown()
    {
        transitionOnCooldown = true;
        yield return new WaitForSeconds(1.5f);
        transitionOnCooldown = false;
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

        var ray = GlobalCtrl.Singleton.currentCamera.ScreenPointToRay(ss_coords);
        Debug.DrawRay(ray.origin, ray.direction, Color.green);
        RaycastHit hit;
        if (Physics.SphereCast(ray, 0.05f, out hit))
        //if (Physics.Raycast(ray, out hit)) 
        {
            var mol = hit.collider.GetComponentInParent<Molecule>();
            var go = hit.collider.GetComponentInParent<GenericObject>();
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

    public Transform getCurrentHoverTarget()
    {
        if (hoverMol != null)
        {
            return hoverMol.transform;
        }
        if (hoverGenericObject != null)
        {
            return hoverGenericObject.transform;
        }
        return null;
    }


    public void initializeTransitionServer(Vector2 ss_coords, InteractionType triggered_by)
    {
        if (transitionOnCooldown) return;
        StartCoroutine(startCooldown());

        grabHold = true;
        var wpos = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(ss_coords.x, ss_coords.y, GlobalCtrl.Singleton.currentCamera.nearClipPlane + 0.1f)); // z component is target distance from camera

        // debug blink
        //StartCoroutine(blinkOnScreen(ss_coords, wpos));

        //Ray ray = new Ray();
        //ray.direction = GlobalCtrl.Singleton.currentCamera.transform.forward;
        //ray.origin = wpos;
        // using the forward vector of the camera is only properly working in the middle of the screen
        // better use:
        //var ray = GlobalCtrl.Singleton.currentCamera.ScreenPointToRay(new Vector3(ss_coords.x, ss_coords.y, GlobalCtrl.Singleton.currentCamera.nearClipPlane + 0.0001f));
        var ray = GlobalCtrl.Singleton.currentCamera.ScreenPointToRay(ss_coords);

        var sphere_radius = triggered_by == InteractionType.CLOSE_GRAB? 0.08f : 0.05f;
        RaycastHit hit;
        if (Physics.SphereCast(ray, sphere_radius, out hit))
        {
            Transform trans = null; 
            var mol_test = hit.collider.GetComponentInParent<Molecule>();
            GenericObject go_test = null;
            if (mol_test != null)
            {
                if (!mol_test.getIsInteractable())
                {
                    mol_test = null;
                }
                else
                {
                    Debug.Log("[initializeTransitionServer] hit Molecule");
                    trans = mol_test.transform;
                }
            }
            else
            {
                go_test = hit.collider.GetComponentInParent<GenericObject>();
                if (go_test != null)
                {
                    if (!go_test.getIsInteractable())
                    {
                        go_test = null;
                    }
                    else
                    {
                        Debug.Log("[initializeTransitionServer] hit GenericObject");
                        trans = go_test.transform;
                    }
                }
                else
                {
                    if (StudyTaskManager.Singleton)
                    {
                        StudyTaskManager.Singleton.logTransitionGrab(null, null, triggered_by);
                    }
                    return; // neither mol nor go hit, but some other type of object
                }
            }

            if (StudyTaskManager.Singleton)
            {
                StudyTaskManager.Singleton.logTransitionGrab(mol_test, go_test, triggered_by);
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

        if (StudyTaskManager.Singleton)
        {
            StudyTaskManager.Singleton.logTransitionGrab(null, null, triggered_by);
        }
    }

    public void initializeTransitionServer(Transform trans, InteractionType triggered_by)
    {
        var mol = trans.GetComponent<Molecule>();
        if (mol != null)
        {
            initializeTransitionServer(mol, triggered_by);
            return;
        }
        var go = trans.GetComponent<GenericObject>();
        if (go != null)
        {
            initializeTransitionServer(go, triggered_by);
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
            if (StudyTaskManager.Singleton) StudyTaskManager.Singleton.logTransition(mol.name, triggered_by);
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
            if (StudyTaskManager.Singleton) StudyTaskManager.Singleton.logTransition(go.name, triggered_by);
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
        if (transitionOnCooldown) return;
        StartCoroutine(startCooldown());

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
        if (triggered_by == InteractionType.CLOSE_GRAB || triggered_by == InteractionType.THROW)
        {
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
            AudioSource.PlayClipAtPoint(doTransition, trans.position);
        } else if (SettingsData.transitionMode == TransitionMode.INSTANT)
        {
            // TODO check this pos and scale ...
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
            AudioSource.PlayClipAtPoint(doTransition, trans.position);
        }
        else
        {
            if (SettingsData.transitionAnimation.HasFlag(TransitionAnimation.SCALE))
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
        getTransitionClient(mol.transform, triggered_by, mol.initial_scale);
    }

    public void getGenericObjectTransitionClient(GenericObject go, InteractionType triggered_by)
    {
        Debug.Log("[getGenericObjectTransitionClient] triggered");
        getTransitionClient(go.transform, triggered_by, go.initial_scale);
    }

    private void getTransitionClient(Transform trans, InteractionType triggered_by, float initial_scale)
    {
        StartCoroutine(startCooldown());
        Debug.Log($"[TransitionManager:getTransitionClient] trigger: {triggered_by}; immersive traget: {SettingsData.immersiveTarget}");
        if (triggered_by == InteractionType.CLOSE_GRAB)
        {
            StartCoroutine(attachToGrip(trans));
            screenAlignment.Singleton.addObjectToGrow(trans, initial_scale);
            AudioSource.PlayClipAtPoint(getTransition, trans.position);
            return;
        }
        if (SettingsData.transitionMode == TransitionMode.INSTANT)
        {
            if (SettingsData.immersiveTarget == ImmersiveTarget.HAND_FIXED || SettingsData.immersiveTarget == ImmersiveTarget.HAND_FOLLOW)
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
                //var screenSize = screenAlignment.Singleton.getScreenSizeWS();
                //float dist_to_move = GlobalCtrl.Singleton.getLongestBBoxEdge(trans);
                var dist_to_move = 0.3f * screenAlignment.Singleton.getScreenSizeWS().y;
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

            bool override_grab_hold = triggered_by == InteractionType.BUTTON_PRESS ? true : false;

            if (SettingsData.immersiveTarget == ImmersiveTarget.HAND_FOLLOW)
            {
                StartCoroutine(moveToHand(trans, override_grab_hold));
            }
            else if (SettingsData.immersiveTarget == ImmersiveTarget.HAND_FIXED)
            {
                var index_pos = HandTracking.Singleton.getIndexTip();
                StartCoroutine(moveToPos(trans, index_pos, override_grab_hold));
            }
            else if (SettingsData.immersiveTarget == ImmersiveTarget.CAMERA)
            {
                StartCoroutine(moveToUser(trans, override_grab_hold));
            }
            else // SettingsData.immersiveTarget == ImmersiveTarget.FRONT_OF_SCREEN
            {
                
                //var longest_edge = GlobalCtrl.Singleton.getLongestBBoxEdge(trans);
                var dist_to_move = 0.3f * screenAlignment.Singleton.getScreenSizeWS().y;
                //float dist_to_move = // half_screen_y > longest_edge ? 3f * longest_edge : half_screen_y;
                var fos_pos = trans.position + dist_to_move * screenAlignment.Singleton.getScreenNormal();
                StartCoroutine(moveToPos(trans, fos_pos, true));
            }
            if (SettingsData.transitionAnimation.HasFlag(TransitionAnimation.SCALE))
            {
                Debug.Log("[getTransitionClient] Animating scale.");
                StartCoroutine(scaleWhileMoving(trans, 1f, override_grab_hold));
            }
        }
        AudioSource.PlayClipAtPoint(getTransition, trans.position);
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
        StartCoroutine(startCooldown());

        if (StudyTaskManager.Singleton)
        {
            StudyTaskManager.Singleton.logTransition(trans.name, triggered_by);
        }
        if (triggered_by != InteractionType.CLOSE_GRAB)
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

        if (SettingsData.transitionMode == TransitionMode.FULL_3D)
        {
            StartCoroutine(moveAway(trans));
        }
    }

    private IEnumerator scaleWhileMoving(Transform trans, float target_scale = 1f, bool override_grab_hold = false)
    {
        float start_scale = trans.localScale.x;
        float elapsedTime = 0f;
        AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        while (elapsedTime < SettingsData.transitionAnimationDuration)
        {
            if (SettingsData.requireGrabHold && !override_grab_hold)
            {
                if (!grabHold)
                {
                    StartCoroutine(scaleAnimation(trans, start_scale));
                    yield break;
                }
            }
            // Increment elapsed time
            elapsedTime += Time.fixedDeltaTime;
            // Calculate the normalized time (0 to 1)
            float normalizedTime = Mathf.Clamp01(elapsedTime / SettingsData.transitionAnimationDuration);
            // Evaluate the curve at the normalized time
            float curveValue = animationCurve.Evaluate(normalizedTime);
            // Interpolate between start and end points
            trans.localScale = Mathf.Lerp(start_scale, target_scale, curveValue) * Vector3.one;

            yield return null;  // wait for next frame
        }
    }

    private IEnumerator scaleAnimation(Transform trans, float target_scale = 1f)
    {
        float start_scale = trans.localScale.x;
        float elapsedTime = 0f;
        AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        while (elapsedTime < SettingsData.transitionAnimationDuration)
        {
            // Increment elapsed time
            elapsedTime += Time.fixedDeltaTime;
            // Calculate the normalized time (0 to 1)
            float normalizedTime = Mathf.Clamp01(elapsedTime / SettingsData.transitionAnimationDuration);
            // Evaluate the curve at the normalized time
            float curveValue = animationCurve.Evaluate(normalizedTime);
            // Interpolate between start and end points
            trans.localScale = Mathf.Lerp(start_scale, target_scale, curveValue) * Vector3.one;

            yield return null;  // wait for next frame
        }
    }

    private IEnumerator moveAndTransition(Transform trans, Vector3 target_pos, InteractionType triggered_by, bool override_grab_hold = false)
    {
        var audio_source = trans.GetComponent<AudioSource>();
        if (audio_source == null)
        {
            audio_source = trans.gameObject.AddComponent<AudioSource>();
        }
        audio_source.clip = moveFromTransitionClip;
        audio_source.loop = true;
        audio_source.volume = 0f;
        audio_source.Play();


        AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        float elapsedTime = 0f;
        var start_pos = trans.position;
        var initial_distance = Vector3.Distance(start_pos, target_pos);
        while (elapsedTime < SettingsData.transitionAnimationDuration)
        {
            if (SettingsData.requireGrabHold && !override_grab_hold)
            {
                if (!grabHold)
                {
                    audio_source.Stop();
                    correctAnimationAbort(trans);
                    yield break;
                }
            }
            // Increment elapsed time
            elapsedTime += Time.fixedDeltaTime;

            // Calculate the normalized time (0 to 1)
            float normalizedTime = Mathf.Clamp01(elapsedTime / SettingsData.transitionAnimationDuration);

            // Evaluate the curve at the normalized time
            float curveValue = animationCurve.Evaluate(normalizedTime);

            // Interpolate between start and end points
            trans.position = Vector3.Lerp(start_pos, target_pos, curveValue);

            var current_distance = Vector3.Distance(trans.position, target_pos);
            audio_source.volume = Mathf.Clamp01(1f - current_distance / initial_distance) * 0.75f;

            yield return null; // wait for next frame
        }
        audio_source.Stop();

        var mol = trans.GetComponent<Molecule>();
        if (mol != null)
        {
            if (StudyTaskManager.Singleton) StudyTaskManager.Singleton.logTransition(mol.name, triggered_by);
            EventManager.Singleton.TransitionMolecule(mol, triggered_by);
        }
        else
        {
            var go = trans.GetComponent<GenericObject>();
            if (StudyTaskManager.Singleton) StudyTaskManager.Singleton.logTransition(go.name, triggered_by);
            EventManager.Singleton.TransitionGenericObject(go, triggered_by);
        }
        AudioSource.PlayClipAtPoint(doTransition, trans.position);
    }

    private IEnumerator moveToPos(Transform trans, Vector3 target_pos, bool override_grab_hold = false)
    {
        var audio_source = trans.GetComponent<AudioSource>();
        if (audio_source == null)
        {
            audio_source = trans.gameObject.AddComponent<AudioSource>();
        }
        audio_source.clip = moveFromTransitionClip;
        audio_source.loop = true;
        audio_source.volume = 0f;
        audio_source.Play();


        AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        float elapsedTime = 0f;
        var start_pos = trans.position;
        var initial_distance = Vector3.Distance(start_pos, target_pos);
        while (elapsedTime < SettingsData.transitionAnimationDuration)
        {
            if (SettingsData.requireGrabHold && !override_grab_hold)
            {
                if (!grabHold)
                {
                    audio_source.Stop();
                    correctAnimationAbort(trans);
                    yield break;
                }
            }
            // Increment elapsed time
            elapsedTime += Time.fixedDeltaTime;

            // Calculate the normalized time (0 to 1)
            float normalizedTime = Mathf.Clamp01(elapsedTime / SettingsData.transitionAnimationDuration);

            // Evaluate the curve at the normalized time
            float curveValue = animationCurve.Evaluate(normalizedTime);

            // Interpolate between start and end points
            trans.position = Vector3.Lerp(start_pos, target_pos, curveValue);

            var current_distance = Vector3.Distance(trans.position, target_pos);
            audio_source.volume = Mathf.Clamp01(1f - current_distance / initial_distance) * 0.75f;

            yield return null; // wait for next frame
        }
        audio_source.Stop();
    }

    private IEnumerator moveToScreenAndTransition(Transform trans, InteractionType triggered_by, bool override_grab_hold = false)
    {
        var center = screenAlignment.Singleton.getScreenCenter();
        var target_pos = center;

        var audio_source = trans.GetComponent<AudioSource>();
        if (audio_source == null)
        {
            audio_source = trans.gameObject.AddComponent<AudioSource>();
        }
        audio_source.clip = moveFromTransitionClip;
        audio_source.loop = true;
        audio_source.volume = 0f;
        audio_source.Play();

        AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        float elapsedTime = 0f;
        var start_pos = trans.position;
        var initial_distance = Vector3.Distance(start_pos, target_pos);
        while (elapsedTime < SettingsData.transitionAnimationDuration)
        {
            if (SettingsData.requireGrabHold && !override_grab_hold)
            {
                if (!grabHold)
                {
                    audio_source.Stop();
                    correctAnimationAbort(trans);
                    yield break;
                }
            }
            // Increment elapsed time
            elapsedTime += Time.fixedDeltaTime;

            // Calculate the normalized time (0 to 1)
            float normalizedTime = Mathf.Clamp01(elapsedTime / SettingsData.transitionAnimationDuration);

            // Evaluate the curve at the normalized time
            float curveValue = animationCurve.Evaluate(normalizedTime);

            if (SettingsData.desktopTarget == DesktopTarget.HOVER)
            {
                target_pos = screenAlignment.Singleton.getCurrentProjectedIndexPos();
                //if (!screenAlignment.Singleton.contains(pos)) ...
            }
            if (SettingsData.desktopTarget == DesktopTarget.CURSOR_POSITION)
            {
                target_pos = screenAlignment.Singleton.getWorldSpaceCoords(NetworkManagerClient.Singleton.ServerMousePosition);
            }

            // Interpolate between start and end points
            trans.position = Vector3.Lerp(start_pos, target_pos, curveValue);

            var current_distance = Vector3.Distance(trans.position, target_pos);
            audio_source.volume = Mathf.Clamp01(1f - current_distance / initial_distance) * 0.75f;

            yield return null; // wait for next frame
        }
        audio_source.Stop();

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
        AudioSource.PlayClipAtPoint(doTransition, trans.position);
    }

    private IEnumerator moveToHand(Transform trans, bool override_grab_hold = false)
    {
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

        var audio_source = trans.GetComponent<AudioSource>();
        if (audio_source == null)
        {
            audio_source = trans.gameObject.AddComponent<AudioSource>();
        }
        audio_source.clip = moveFromTransitionClip;
        audio_source.loop = true;
        audio_source.volume = 0f;
        audio_source.Play();

        AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        float elapsedTime = 0f;
        var start_pos = trans.position;
        var target_pos = HandTracking.Singleton.getIndexTip();
        var initial_distance = Vector3.Distance(trans.position, target_pos);
        while (elapsedTime < SettingsData.transitionAnimationDuration)
        {
            if (SettingsData.requireGrabHold && !override_grab_hold)
            {
                if (!grabHold)
                {
                    audio_source.Stop();
                    correctAnimationAbort(trans);
                    yield break;
                }
            }
            // Increment elapsed time
            elapsedTime += Time.fixedDeltaTime;

            // Calculate the normalized time (0 to 1)
            float normalizedTime = Mathf.Clamp01(elapsedTime / SettingsData.transitionAnimationDuration);

            // Evaluate the curve at the normalized time
            float curveValue = animationCurve.Evaluate(normalizedTime);

            target_pos = HandTracking.Singleton.getIndexTip();
            // Interpolate between start and end points
            trans.position = Vector3.Lerp(start_pos, target_pos, curveValue);

            var current_distance = Vector3.Distance(trans.position, target_pos);
            audio_source.volume = Mathf.Clamp01(1f - current_distance / initial_distance) * 0.75f;

            if (SettingsData.transitionAnimation.HasFlag(TransitionAnimation.ROTATION))
            {
                Debug.Log("[moveToHand] Animating rotation.");
                var head_to_obj = Quaternion.LookRotation(trans.position - GlobalCtrl.Singleton.currentCamera.transform.position);
                trans.rotation = head_to_obj * relQuat;
            }
            yield return null; // wait for next frame
        }
        audio_source.Stop();
    }

    private void correctAnimationAbort(Transform trans)
    {
        var target_dist = 0.3f * screenAlignment.Singleton.getScreenSizeWS().y;
        float current_dist = screenAlignment.Singleton.getDistanceFromScreen(trans.position);
        if (current_dist < target_dist)
        {
            var target_proj = screenAlignment.Singleton.projectWSPointToScreen(trans.position);
            var target_pos = target_proj + target_dist * screenAlignment.Singleton.getScreenNormal();
            StartCoroutine(moveToPos(trans, target_pos, true));
        }
    }

    private IEnumerator moveToUser(Transform trans, bool override_grab_hold = false)
    {
        var target_pos = GlobalCtrl.Singleton.getCurrentSpawnPos();
        var initial_distance = Vector3.Distance(target_pos, trans.position);
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

        var audio_source = trans.GetComponent<AudioSource>();
        if (audio_source == null)
        {
            audio_source = trans.gameObject.AddComponent<AudioSource>();
        }
        audio_source.clip = moveFromTransitionClip;
        audio_source.loop = true;
        audio_source.volume = 0f;
        audio_source.Play();

        AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        float elapsedTime = 0f;
        var start_pos = trans.position;
        while (elapsedTime < SettingsData.transitionAnimationDuration)
        {
            if (SettingsData.requireGrabHold && !override_grab_hold)
            {
                if (!grabHold)
                {
                    audio_source.Stop();
                    correctAnimationAbort(trans);
                    yield break;
                }
            }
            // Increment elapsed time
            elapsedTime += Time.fixedDeltaTime;

            // Calculate the normalized time (0 to 1)
            float normalizedTime = Mathf.Clamp01(elapsedTime / SettingsData.transitionAnimationDuration);

            // Evaluate the curve at the normalized time
            float curveValue = animationCurve.Evaluate(normalizedTime);

            //target_pos = GlobalCtrl.Singleton.getCurrentSpawnPos();
            // Interpolate between start and end points
            trans.position = Vector3.Lerp(start_pos, target_pos, curveValue);

            var current_distance = Vector3.Distance(trans.position, target_pos);
            audio_source.volume = Mathf.Clamp01(1f - current_distance/initial_distance) * 0.75f;

            if (SettingsData.transitionAnimation.HasFlag(TransitionAnimation.ROTATION))
            {
                Debug.Log("[moveToUser] Animating rotation.");
                var head_to_obj = Quaternion.LookRotation(trans.position - GlobalCtrl.Singleton.currentCamera.transform.position);
                trans.rotation = head_to_obj * relQuat;
            }
            yield return null; // wait for next frame
        }
        audio_source.Stop();
    }

    private IEnumerator moveAway(Transform trans)
    {
        var destination = GlobalCtrl.Singleton.getCurrentSpawnPos();
        AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        float elapsedTime = 0f;
        var start_pos = trans.position;
        while (elapsedTime < SettingsData.transitionAnimationDuration)
        {
            if (SettingsData.requireGrabHold)
            {
                if (!grabHold) yield break;
            }
            // Increment elapsed time
            elapsedTime += Time.fixedDeltaTime;

            // Calculate the normalized time (0 to 1)
            float normalizedTime = Mathf.Clamp01(elapsedTime / SettingsData.transitionAnimationDuration);

            // Evaluate the curve at the normalized time
            float curveValue = animationCurve.Evaluate(normalizedTime);

            // Interpolate between start and end points
            trans.position = Vector3.Lerp(start_pos, destination, curveValue);

            yield return null; // wait for next frame
        }
    }

    private IEnumerator attachToGrip(Transform trans)
    {
        manualSetGrip(trans, true);
        var isGrabbed = true;
        while (isGrabbed)
        {
            if(trans == null) yield break;
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
