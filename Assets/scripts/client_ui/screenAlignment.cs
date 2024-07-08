using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
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
                        AudioSource.PlayClipAtPoint(confirmClip, index_pos);
                        indicator3.SetActive(true);
                        indicator3.transform.position = index_pos;
                        indicator4.SetActive(true);
                        indicator4.transform.position = screenVertices[3];

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

        BoxCollider collider = GetComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size;

        // listen to distant grabs
        HandTracking.Singleton.OnMiddleFingerGrab += OnDistantGrab;
        HandTracking.Singleton.OnMiddleFingerGrabRelease += OnDistantGrabRelease;
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
        var dir_x = screenVertices[2] - screenVertices[0];
        var dir_y = screenVertices[1] - screenVertices[0];
        var normal = Vector3.Cross(dir_x, dir_y).normalized;
        // project on plane https://forum.unity.com/threads/projection-of-point-on-plane.855958/
        var pos_projected_on_screen = Vector3.ProjectOnPlane(index_in_world_space, normal) + Vector3.Dot(screenCenter, normal) * normal;

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

        var on_screen_x = Mathf.Clamp(projected_x * SettingsData.serverViewport.x, 0f, SettingsData.serverViewport.x);
        var on_screen_y = Mathf.Clamp(projected_y * SettingsData.serverViewport.y, 0f, SettingsData.serverViewport.y);

        //var on_screen_x = projected_x * SettingsData.serverViewport.x;
        //var on_screen_y = projected_y * SettingsData.serverViewport.y;

        //Debug.Log($"[screenAlignment] p_x {projected_x} p_y {projected_y}");
        //Debug.Log($"[screenAlignment] p_x {on_screen_x} p_y {on_screen_y}");

        return new Vector2(on_screen_x, on_screen_y);
    }

    public Vector3 getWordsSpaceCoords(Vector2 input)
    {
        var dir_x = screenVertices[2] - screenVertices[0];
        var dir_y = screenVertices[1] - screenVertices[0];

        var ss_coords = screenVertices[0] + dir_x * input.x / SettingsData.serverViewport.x + dir_y * input.y / SettingsData.serverViewport.y;

        return ss_coords;
    }


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
        }
    }

    public Vector3 getCurrentProjectedPos()
    {
        var proj = projectIndexOnScreen();
        return proj.Value;
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
            EventManager.Singleton.GrabOnScreen(viewport_coords.Value);
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
            EventManager.Singleton.GrabOnScreen(viewport_coords.Value);
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
