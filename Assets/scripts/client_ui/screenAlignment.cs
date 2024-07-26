using Microsoft.MixedReality.Toolkit.Input;
using RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public class screenAlignment : MonoBehaviour, IMixedRealityPointerHandler
{

    private static screenAlignment _singleton;

    public static screenAlignment Singleton
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
                Debug.Log($"[{nameof(screenAlignment)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public delegate void ScreenInitializedAction();
    public event ScreenInitializedAction OnScreenInitialized;

    public GameObject screenQuad;
    public GameObject indicator1;
    public GameObject indicator2;
    public GameObject indicator3;
    public GameObject indicator4;
    private BoxCollider collider;

    private void Awake()
    {
        Singleton = this;
    }

    [HideInInspector] public AudioClip confirmClip;
    [HideInInspector] public GameObject screenPrefab;

    // Start is called before the first frame update
    void Start()
    {
        confirmClip = (AudioClip)Resources.Load("audio/confirmation");
        screenPrefab = (GameObject)Resources.Load("prefabs/ScreenPrefab");

        screenQuad.SetActive(false);
        indicator1.SetActive(false);
        indicator2.SetActive(false);
        indicator3.SetActive(false);
        indicator4.SetActive(false);

        OnScreenInitialized += OnInitializationComplete;

        // we will keep the screen alive
        DontDestroyOnLoad(gameObject);

        // on scene change
        SceneManager.activeSceneChanged += OnSceneChange;
    }

    System.Diagnostics.Stopwatch screenScanStopwatch;
    Vector3 oldIndexPos = Vector3.zero;
    Vector3[] screenVertices;
    Vector3 screenNormal;
    bool fullyInitialized = false;
    Vector3 screenCenter = Vector3.zero;
    public void startScreenAlignment()
    {
        if (fullyInitialized)
        {
            fullyInitialized = false;
            screenQuad.SetActive(false);
            indicator1.SetActive(false);
            indicator2.SetActive(false);
            indicator3.SetActive(false);
            indicator4.SetActive(false);
        }
        HandTracking.Singleton.gameObject.SetActive(true);
        screenVertices = new Vector3[4] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
        screenScanStopwatch = System.Diagnostics.Stopwatch.StartNew();
        StartCoroutine(timedCheck());
    }

    private IEnumerator timedCheck()
    {
        bool run_check = true;
        while (run_check)
        {
            var index_pos = HandTracking.Singleton.getIndexTip(); // for scanning screen use tip
            if (Vector3.Distance(index_pos, oldIndexPos) <= 0.001f)
            {
                screenScanStopwatch.Stop();
                if (screenScanStopwatch.ElapsedMilliseconds > 2000)
                {
                    if (screenVertices[0] == Vector3.zero)
                    {
                        screenVertices[0] = index_pos;
                        AudioSource.PlayClipAtPoint(confirmClip, index_pos);
                        indicator1.SetActive(true);
                        indicator1.transform.position = index_pos;
                        screenScanStopwatch.Restart();
                    }
                    else if (screenVertices[1] == Vector3.zero)
                    {
                        screenVertices[1] = index_pos;
                        AudioSource.PlayClipAtPoint(confirmClip, index_pos);
                        indicator2.SetActive(true);
                        indicator2.transform.position = index_pos;
                        screenScanStopwatch.Restart();
                    }
                    else
                    {
                        screenVertices[2] = index_pos;
                        screenCenter = screenVertices[2] + 0.5f * (screenVertices[1] - screenVertices[2]);
                        screenVertices[3] = screenVertices[0] + 2f * (screenCenter - screenVertices[0]);
                        //calc normal
                        var dir_x = screenVertices[2] - screenVertices[0];
                        var dir_y = screenVertices[1] - screenVertices[0];
                        screenNormal = -Vector3.Cross(dir_x, dir_y).normalized;
                        // indicators
                        AudioSource.PlayClipAtPoint(confirmClip, index_pos);
                        indicator3.SetActive(true);
                        indicator3.transform.position = index_pos;
                        indicator4.SetActive(true);
                        indicator4.transform.position = screenVertices[3];
                        // quad
                        screenQuad.transform.position = Vector3.zero;
                        var vertices = new Vector3[4];
                        vertices[0] = screenQuad.transform.InverseTransformPoint(screenVertices[0]);
                        vertices[1] = screenQuad.transform.InverseTransformPoint(screenVertices[2]);
                        vertices[2] = screenQuad.transform.InverseTransformPoint(screenVertices[1]);
                        vertices[3] = screenQuad.transform.InverseTransformPoint(screenVertices[3]);
                        screenQuad.GetComponent<MeshFilter>().mesh.vertices = vertices;
                        screenQuad.GetComponent<MeshFilter>().mesh.RecalculateNormals();
                        screenQuad.GetComponent<MeshFilter>().mesh.RecalculateBounds();
                        screenQuad.SetActive(true);
                        OnScreenInitialized?.Invoke();
                        run_check = false;
                    }
                }
                else
                {
                    screenScanStopwatch.Start();
                }
            }
            else
            {
                screenScanStopwatch.Restart();
            }
            oldIndexPos = index_pos;
            yield return new WaitForSeconds(0.01f);
        }
    }


    void OnInitializationComplete()
    {
        fullyInitialized = true;
        // set correct collider
        Bounds bounds = indicator1.GetComponent<BoxCollider>().bounds;

        var indicator_list = new GameObject[4] { indicator1, indicator2, indicator3, indicator4 };
        foreach (var ind in indicator_list)
        {
            var box_col = ind.GetComponent<BoxCollider>();
            if (box_col != null)
            {
                bounds.Encapsulate(box_col.bounds);
            }
        }

        collider = GetComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size;

        // listen to distant grabs
        HandTracking.Singleton.OnMiddleFingerGrab += OnDistantGrab;
        HandTracking.Singleton.OnMiddleFingerGrabRelease += OnDistantGrabRelease;

        // turn off quad after 2 sec
        StartCoroutine(turnOffQuad());
    }


    private IEnumerator turnOffQuad()
    {
        yield return new WaitForSeconds(2f);
        screenQuad.SetActive(false);
    }


    GameObject projectionIndicator;
    public Vector3? projectIndexOnScreen()
    {
        if (!fullyInitialized)
        {
            Debug.LogError("[screenAlignment] Trying to access alignment of index finger, but screen is not initialized.");
            return null;
        }
        if (!HandTracking.Singleton.gameObject.activeSelf)
        {
            HandTracking.Singleton.gameObject.SetActive(true);
        }

        var index_in_world_space = HandTracking.Singleton.getIndexKnuckle(); // for tracking use knuckle

        return projectWSPointToScreen(index_in_world_space);
    }

    public Vector3 projectWSPointToScreen(Vector3 point)
    {
        // project on plane https://forum.unity.com/threads/projection-of-point-on-plane.855958/
        var pos_projected_on_screen = Vector3.ProjectOnPlane(point, screenNormal) + Vector3.Dot(screenCenter, screenNormal) * screenNormal;

        return pos_projected_on_screen;
    }

    public Vector2? getScreenSpaceCoords(Vector3 input)
    {
        if (!fullyInitialized)
        {
            Debug.LogError("[screenAlignment] Trying project to screen space, but screen is not initialized.");
            return null;
        }
        if (!HandTracking.Singleton.gameObject.activeSelf)
        {
            HandTracking.Singleton.gameObject.SetActive(true);
        }

        var dir_x = screenVertices[2] - screenVertices[0];
        var dir_y = screenVertices[1] - screenVertices[0];
        //var dir_max = screenVertices[3] - screenVertices[0];
        var input_dir = input - screenVertices[0];

        //var p_max_x = Vector3.Dot(dir_x, dir_max);
        //var p_max_y = Vector3.Dot(dir_y, dir_max);

        //var projected_x = Vector3.Dot(dir_x, input_dir) / p_max_x;
        //var projected_y = Vector3.Dot(dir_y, input_dir) / p_max_y;

        var projected_x = Vector3.Dot(dir_x.normalized, input_dir) / dir_x.magnitude;
        var projected_y = Vector3.Dot(dir_y.normalized, input_dir) / dir_y.magnitude;

        //var on_screen_x = Mathf.Clamp(projected_x * SettingsData.serverViewport.x, 0f, SettingsData.serverViewport.x);
        //var on_screen_y = Mathf.Clamp(projected_y * SettingsData.serverViewport.y, 0f, SettingsData.serverViewport.y);

        var on_screen_x = projected_x * SettingsData.serverViewport.x;
        var on_screen_y = projected_y * SettingsData.serverViewport.y;

        //Debug.Log($"[screenAlignment] p_x {projected_x} p_y {projected_y}");
        //Debug.Log($"[screenAlignment] p_x {on_screen_x} p_y {on_screen_y}");

        return new Vector2(on_screen_x, on_screen_y);
    }

    public Vector3 getWorldSpaceCoords(Vector2 input)
    {
        var dir_x = screenVertices[2] - screenVertices[0];
        var dir_y = screenVertices[1] - screenVertices[0];

        var ws_coords = screenVertices[0] + dir_x * input.x / SettingsData.serverViewport.x + dir_y * input.y / SettingsData.serverViewport.y;

        return ws_coords;
    }

    public Vector2 getScreenAlignedDistanceWS(Vector2 input_min, Vector2 input_max)
    {
        var dir_x = screenVertices[2] - screenVertices[0];
        var dir_y = screenVertices[1] - screenVertices[0];

        var ws_coords1 = screenVertices[0] + dir_x * input_min.x / SettingsData.serverViewport.x + dir_y * input_min.y / SettingsData.serverViewport.y;
        var ws_coords2 = screenVertices[0] + dir_x * input_max.x / SettingsData.serverViewport.x + dir_y * input_max.y / SettingsData.serverViewport.y;

        var diff = ws_coords2 - ws_coords1;

        var amount_x = Vector3.Dot(diff, dir_x);
        var amount_y = Vector3.Dot(diff, dir_y);

        return new Vector2(amount_x, amount_y);
    }

    public Vector2 sizeProjectedToScreenWS(Bounds bounds)
    {
        var corners = bounds.GetCorners();

        var dir_x = screenVertices[2] - screenVertices[0];
        var dir_y = screenVertices[1] - screenVertices[0];

        Vector2 min = Vector2.one * float.MaxValue;
        Vector2 max = Vector2.zero;

        foreach (var corner in corners)
        {
            var proj_point = projectWSPointToScreen(corner) - screenVertices[0];
            
            var amount_x = Vector3.Dot(proj_point, dir_x);
            var amount_y = Vector3.Dot(proj_point, dir_y);

            Vector2 projVec = new Vector2(amount_x, amount_y);

            min = Vector2.Min(min, projVec);
            max = Vector2.Max(max, projVec);
        }

        return max - min;
    }

    public Vector3 getScreenNormal()
    {
        return screenNormal;
    }

    List<Transform> old_intersecting_objects = new List<Transform>();
    List<Transform> to_remove_intersecting_objects = new List<Transform>();
    Dictionary<Transform, float> initial_scale_of_intersecting_objects = new Dictionary<Transform, float>();
    private void FixedUpdate()
    {
        if (fullyInitialized)
        {
            var proj = projectIndexOnScreen();
            if (projectionIndicator == null)
            {
                projectionIndicator = Instantiate(indicator1, transform);
                DestroyImmediate(projectionIndicator.GetComponent<BoxCollider>());
                projectionIndicator.GetComponent<Renderer>().material.color = new Color(0f, 1f, 0f, 1f);
            }
            if (GetComponent<BoxCollider>().bounds.Contains(proj.Value))
            {
                projectionIndicator.SetActive(true);
                projectionIndicator.transform.position = proj.Value;
                var ss_coords = getScreenSpaceCoords(proj.Value);
                if (EventManager.Singleton != null)
                {
                    EventManager.Singleton.HoverOverScreen(ss_coords.Value);
                }
            }
            else
            {
                projectionIndicator.SetActive(false);
            }

            // check if object is getting pushed into the screen
            if (GlobalCtrl.Singleton != null)
            {
                var intersecting_objects = new List<Transform>();
                if (GenericObject.objects != null && GenericObject.objects.Count > 0)
                {
                    foreach (var obj in GenericObject.objects.Values)
                    {
                        if (collider.bounds.Intersects(obj.GetComponent<myBoundingBox>().localBounds) && obj.isGrabbed)
                        {
                            intersecting_objects.Add(obj.transform);
                            if (!initial_scale_of_intersecting_objects.Keys.Contains(obj.transform))
                            {
                                initial_scale_of_intersecting_objects[obj.transform] = obj.transform.localScale.x;
                            }
                        }
                    }
                }
                if (GlobalCtrl.Singleton.List_curMolecules != null && GlobalCtrl.Singleton.List_curMolecules.Count > 0)
                {
                    foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
                    {
                        if (collider.bounds.Intersects(mol.GetComponent<myBoundingBox>().localBounds) && mol.isGrabbed)
                        {
                            intersecting_objects.Add(mol.transform);
                            if (!initial_scale_of_intersecting_objects.Keys.Contains(mol.transform))
                            {
                                initial_scale_of_intersecting_objects[mol.transform] = mol.transform.localScale.x;
                            }
                        }
                    }
                }

                // transition object if center pushed beyond screen
                var is_transitioned = false;
                foreach (var obj in intersecting_objects)
                {
                    is_transitioned = transition(obj);
                }

                if (SettingsData.transitionAnimation == TransitionManager.TransitionAnimation.BOTH || SettingsData.transitionAnimation == TransitionManager.TransitionAnimation.SCALE)
                {
                    if (!is_transitioned)
                    {
                        // shrink object while getting pushed in the screen
                        foreach (var obj in intersecting_objects)
                        {
                            shrink(obj);
                        }

                        if (old_intersecting_objects.Count > 0)
                        {
                            foreach (var oio in old_intersecting_objects)
                            {
                                if (!intersecting_objects.Contains(oio))
                                {
                                    // only grow when grabbed
                                    bool isGrabbed = false;
                                    var mol = oio.GetComponent<Molecule>();
                                    if (mol != null)
                                    {
                                        isGrabbed = mol.isGrabbed;
                                    }
                                    var go = oio.GetComponent<GenericObject>();
                                    if (go != null)
                                    {
                                        isGrabbed = go.isGrabbed;
                                    }
                                    if (isGrabbed)
                                    {
                                        grow(oio);
                                    }
                                    else
                                    {
                                        // remove from list when not grabbed anymore
                                        if (!to_remove_intersecting_objects.Contains(oio))
                                        {
                                            to_remove_intersecting_objects.Add(oio);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // Perform removing from list
                    foreach (var obj in to_remove_intersecting_objects)
                    {
                        old_intersecting_objects.Remove(obj);
                        initial_scale_of_intersecting_objects.Remove(obj);
                    }
                    to_remove_intersecting_objects.Clear();
                }
            }
        }
    }

    private bool transition(Transform trans)
    {
        var box = trans.GetComponent<myBoundingBox>();
        var obj_bounds = box.localBounds;
        if (Vector3.Dot(screenNormal, obj_bounds.center - screenCenter) < 0f)
        {
            if (old_intersecting_objects.Contains(trans))
            {
                to_remove_intersecting_objects.Add(trans);
            }
            TransitionManager.Singleton.initializeTransitionClient(trans, TransitionManager.InteractionType.CLOSE_GRAB);
            return true;
        }
        return false;
    }

    private void shrink(Transform trans)
    {
        // if object is larger than screen it should only take 0.5*screen_hight
        var box = trans.GetComponent<myBoundingBox>();
        var ss_bounds = box.getScreenSpaceBounds();
        var ss_size_y = ss_bounds.w - ss_bounds.y;
        if (ss_size_y > 0.5f * SettingsData.serverViewport.y)
        {
            var target_scale_factor = SettingsData.serverViewport.y / (2f * ss_size_y);
            var target_scale = trans.localScale * target_scale_factor;

            var obj_bounds = box.localBounds;
            var obj_center = obj_bounds.center;
            var obj_center_proj_to_screen = projectWSPointToScreen(obj_center);
            var obj_distance_to_screen = (obj_center_proj_to_screen - obj_center).magnitude;
            var obj_longes_half_edge = Mathf.Max(Mathf.Max(obj_bounds.extents.x, obj_bounds.extents.y), obj_bounds.extents.z);

            var dist_scale_factor = obj_longes_half_edge / obj_distance_to_screen;

            //trans.localScale *= target_scale_factor * dist_scale_factor;
            trans.localScale -= 0.01f * dist_scale_factor * Vector3.one;
            // Regulate overshooting
            if (trans.localScale.x < target_scale.x) trans.localScale = target_scale;
        }
        if (!old_intersecting_objects.Contains(trans)) old_intersecting_objects.Add(trans);
    }

    private void grow(Transform trans)
    {
        var target_scale = 1f;
        if (initial_scale_of_intersecting_objects.Keys.Contains(trans))
        {
            target_scale = initial_scale_of_intersecting_objects[trans];
        }
        if (trans.localScale.x < target_scale)
        {
            var box = trans.GetComponent<myBoundingBox>();
            var obj_bounds = box.localBounds;
            var obj_center = obj_bounds.center;
            var obj_center_proj_to_screen = projectWSPointToScreen(obj_center);
            var obj_distance_to_screen = (obj_center_proj_to_screen - obj_center).magnitude;
            var obj_longes_half_edge = Mathf.Max(Mathf.Max(obj_bounds.extents.x, obj_bounds.extents.y), obj_bounds.extents.z);

            var dist_scale_factor = obj_distance_to_screen / obj_longes_half_edge;

            //trans.localScale *= dist_scale_factor;
            trans.localScale += 0.01f * dist_scale_factor * Vector3.one;
            // regulate overshooting
            if (trans.localScale.x > target_scale) trans.localScale = target_scale * Vector3.one;
        }
        else
        {
            if (!to_remove_intersecting_objects.Contains(trans))
            {
                to_remove_intersecting_objects.Add(trans);
            }
        }
    }


    public Vector3 getCurrentProjectedIndexPos()
    {
        var proj = projectIndexOnScreen();
        return proj.Value;
    }

    public Vector2 getCurrentIndexSSPos()
    {
        var proj = projectIndexOnScreen();
        var ss_coords = getScreenSpaceCoords(proj.Value);
        return ss_coords.Value;
    }

    public Vector3 getScreenCenter()
    {
        return screenCenter;
    }

    public bool contains(Vector3 pos)
    {
        return GetComponent<BoxCollider>().bounds.Contains(pos);
    }


    public void OnDistantGrab(Vector3 pos)
    {
        var proj = projectIndexOnScreen();
        var viewport_coords = getScreenSpaceCoords(proj.Value);
        if (EventManager.Singleton)
        {
            EventManager.Singleton.GrabOnScreen(viewport_coords.Value, true);
        }
    }

    public void OnDistantGrabRelease()
    {
        if (EventManager.Singleton)
        {
            EventManager.Singleton.ReleaseGrabOnScreen();
        }
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        // Intentionally empty
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        // Intentionally empty
        var proj = projectIndexOnScreen();
        var viewport_coords = getScreenSpaceCoords(proj.Value);
        if (EventManager.Singleton)
        {
            EventManager.Singleton.GrabOnScreen(viewport_coords.Value, false);
        }
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        // Intentionally empty
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (EventManager.Singleton)
        {
            EventManager.Singleton.ReleaseGrabOnScreen();
        }
    }

    private void OnSceneChange(Scene current, Scene next)
    {
        if (fullyInitialized)
        {
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.OnMiddleFingerGrab += OnDistantGrab;
                HandTracking.Singleton.OnMiddleFingerGrabRelease += OnDistantGrabRelease;
            }
            else
            {
                Debug.LogError("[screenAlignment] Could not subscribe to HandTracking events.");
            }
            
        }
    }

}
