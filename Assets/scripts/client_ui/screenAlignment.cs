using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace chARpack
{
    public class screenAlignment : MonoBehaviour
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
        AudioClip closeToScreenClip;
        [HideInInspector] public GameObject screenPrefab;
        private GameObject arcPrefab;
        private GameObject arcInstance;
        private GameObject progressBarPrefab;
        private GameObject instructionPrefab;
        private GameObject instructionInstance;

        // Start is called before the first frame update
        void Start()
        {
            confirmClip = (AudioClip)Resources.Load("audio/confirmation");
            closeToScreenClip = Resources.Load<AudioClip>("audio/wine_loop");
            screenPrefab = (GameObject)Resources.Load("prefabs/ScreenPrefab");
            arcPrefab = (GameObject)Resources.Load("prefabs/vfx/vfx_arc_fixed_endpoints");
            gameObject.AddComponent<AudioSource>();

            arcInstance = Instantiate(arcPrefab);
            arcInstance.transform.SetParent(transform);
            arcInstance.gameObject.SetActive(false);

            //progressBarPrefab = Resources.Load<GameObject>("prefabs/VerticalProgressBar");
            progressBarPrefab = Resources.Load<GameObject>("prefabs/RadialProgressBar");

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
                if (HandTracking.Singleton.getCurrentHand() == HandTracking.Handedness.None)
                {
                    screenScanStopwatch.Restart();
                    yield return null;
                    continue;
                }
                var index_pos = HandTracking.Singleton.getIndexTip(); // for scanning screen use tip
#if WINDOWS_UWP
            if (Vector3.Distance(index_pos, oldIndexPos) <= 0.002f)
#else
                if (Vector3.Distance(index_pos, oldIndexPos) <= 0.001f)
#endif
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

        Bounds bounds;
        void OnInitializationComplete()
        {
            fullyInitialized = true;

            //// set correct collider
            bounds = indicator1.GetComponent<BoxCollider>().bounds;

            var indicator_list = new GameObject[4] { indicator1, indicator2, indicator3, indicator4 };
            foreach (var ind in indicator_list)
            {
                var box_col = ind.GetComponent<BoxCollider>();
                if (box_col != null)
                {
                    bounds.Encapsulate(box_col.bounds);
                }
            }

            //collider = GetComponent<BoxCollider>();
            //collider.center = bounds.center;
            //collider.size = bounds.size;

            // listen to distant grabs
            HandTracking.Singleton.OnMiddleFingerGrab.SetDefaultListener(OnDistantTransitionGrab);
            HandTracking.Singleton.OnMiddleFingerGrabRelease += OnDistantTransitionGrabRelease;

            HandTracking.Singleton.OnEmptyIndexFingerGrab.SetDefaultListener(OnDistantGrab);
            HandTracking.Singleton.OnIndexFingerGrabRelease += OnDistantGrabRelease;

            HandTracking.Singleton.OnCatch.SetDefaultListener(OnDistantTransitionCatch);


            // turn off quad after 2 sec
            StartCoroutine(turnOffQuad());
        }


        private IEnumerator turnOffQuad()
        {
            yield return new WaitForSeconds(2f);
            screenQuad.SetActive(false);
        }


        GameObject projectionIndicator;
        public Vector3? projectIndexKnuckleOnScreen()
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

            var index_in_world_space = HandTracking.Singleton.getIndexKnuckle(); // for far tracking use knuckle

            return projectWSPointToScreen(index_in_world_space);
        }

        public Vector3? projectIndexTipOnScreen()
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

            var index_in_world_space = HandTracking.Singleton.getIndexTip();// for close tracking use index

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
        List<Transform> to_remove_objects = new List<Transform>();
        Dictionary<Transform, float> initial_scale_of_intersecting_objects = new Dictionary<Transform, float>();
        Dictionary<Transform, float> initial_distance_of_intersecting_objects = new Dictionary<Transform, float>();
        Dictionary<Transform, GameObject> progressBarInstances = new Dictionary<Transform, GameObject>();
        // Dictionary<Transform, GameObject> transitionPointIndicator = new Dictionary<Transform, GameObject>();
        Dictionary<Transform, Vector3> localTransitionPoint = new Dictionary<Transform, Vector3>();
        bool handCloseToScreen = false;
        private void FixedUpdate()
        {
            if (fullyInitialized)
            {
                Vector3? proj;
                if (getDistanceFromScreen(HandTracking.Singleton.getIndexTip()) < (0.25f * getScreenSizeWS().y))
                {
                    if (!handCloseToScreen)
                    {
                        handCloseToScreen = true;
                        if (HandTracking.Singleton != null)
                        {
                            HandTracking.Singleton.OnEmptyCloseIndexFingerGrab.SetDefaultListener(OnCloseGrab);
                            HandTracking.Singleton.OnIndexFingerGrabRelease += OnCloseGrabRelease;
                        }
                    }
                    proj = projectIndexTipOnScreen();

                    if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.CLOSE_GRAB))
                    {
                        arcInstance.transform.position = proj.Value;
                        var diff_vec = proj.Value - HandTracking.Singleton.getIndexTip();
                        if (Vector3.Dot(screenNormal, diff_vec) < 0)
                        {
                            arcInstance.gameObject.SetActive(true);
                            arcInstance.GetComponentInChildren<VisualEffect>().SetVector3("Pos1", HandTracking.Singleton.getIndexTip());
                            arcInstance.GetComponentInChildren<VisualEffect>().SetVector3("Pos2", HandTracking.Singleton.getIndexTip() + 0.25f * diff_vec);
                            arcInstance.GetComponentInChildren<VisualEffect>().SetVector3("Pos3", HandTracking.Singleton.getIndexTip() + 0.75f * diff_vec);
                            arcInstance.GetComponentInChildren<VisualEffect>().SetVector3("Pos4", proj.Value);
                        }
                        else
                        {
                            arcInstance.gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    if (handCloseToScreen)
                    {
                        handCloseToScreen = false;
                        if (HandTracking.Singleton != null)
                        {
                            HandTracking.Singleton.OnEmptyCloseIndexFingerGrab.RemoveDefaultListener();
                            HandTracking.Singleton.OnIndexFingerGrabRelease -= OnCloseGrabRelease;
                        }
                    }
                    //GetComponent<AudioSource>().Stop();
                    proj = projectIndexKnuckleOnScreen();
                    arcInstance.gameObject.SetActive(false);
                }
                //if (projectionIndicator == null)
                //{
                //    projectionIndicator = Instantiate(indicator1, transform);
                //    DestroyImmediate(projectionIndicator.GetComponent<BoxCollider>());
                //    projectionIndicator.GetComponent<Renderer>().material.color = new Color(0f, 1f, 0f, 1f);
                //}
                if (bounds.Contains(proj.Value))
                {
                    //projectionIndicator.SetActive(true);
                    //projectionIndicator.transform.position = proj.Value;
                    var ss_coords = getScreenSpaceCoords(proj.Value);
                    if (EventManager.Singleton != null)
                    {
                        EventManager.Singleton.HoverOverScreen(ss_coords.Value);
                    }
                }
                //else
                //{
                //    projectionIndicator.SetActive(false);
                //}

                //if (GenericObject.objects != null && GenericObject.objects.Count > 0)
                //{
                //    var go_proj = projectWSPointToScreen(GenericObject.objects.Values.Last().transform.position);
                //    projectionIndicator.transform.position = go_proj;
                //    //Debug.Log($"setting indicator {go_proj}");
                //}

                // check if object is getting pushed into the screen
                if (GlobalCtrl.Singleton != null)
                {
                    var intersecting_objects = new List<Tuple<Transform, bool>>();
                    if (GenericObject.objects != null && GenericObject.objects.Count > 0)
                    {
                        foreach (var obj in GenericObject.objects.Values)
                        {
                            if (bounds.Intersects(obj.GetComponent<myBoundingBox>().localBounds) && obj.isGrabbed)
                            {
                                var is_thrown = false;
                                if (!initial_scale_of_intersecting_objects.Keys.Contains(obj.transform))
                                {
                                    initial_scale_of_intersecting_objects[obj.transform] = obj.transform.localScale.x;
                                }
                                if (!initial_distance_of_intersecting_objects.Keys.Contains(obj.transform))
                                {
                                    var box = obj.transform.GetComponent<myBoundingBox>();
                                    var local_trans_point = box.transform.InverseTransformPoint(box.localBounds.center);
                                    var tip = HandTracking.Singleton.getIndexTip();
                                    if (box.contains(tip))
                                    {
                                        local_trans_point = getTransitionPoint(box, tip);
                                    }
                                    else
                                    {
                                        is_thrown = true;
                                    }

                                    localTransitionPoint[obj.transform] = local_trans_point;
                                    var global_trans_point = obj.transform.TransformPoint(local_trans_point);
                                    var distance = Vector3.Distance(projectWSPointToScreen(global_trans_point), global_trans_point);
                                    initial_distance_of_intersecting_objects[obj.transform] = distance;

                                    //transitionPointIndicator[obj.transform] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                    //transitionPointIndicator[obj.transform].GetComponent<Renderer>().material = Resources.Load<Material>("materials/UserMaterial");
                                    //transitionPointIndicator[obj.transform].transform.SetParent(transform);
                                    //transitionPointIndicator[obj.transform].transform.localScale = 0.01f * Vector3.one;
                                    //transitionPointIndicator[obj.transform].transform.position = global_trans_point;

                                    progressBarInstances[obj.transform] = Instantiate(progressBarPrefab);
                                    progressBarInstances[obj.transform].transform.SetParent(transform);
                                    progressBarInstances[obj.transform].transform.localScale = 0.1f * Vector3.one;
                                    progressBarInstances[obj.transform].gameObject.SetActive(false);

                                    var audio_source = obj.GetComponent<AudioSource>();
                                    if (audio_source == null)
                                    {
                                        audio_source = obj.AddComponent<AudioSource>();
                                    }
                                    audio_source.clip = closeToScreenClip;
                                    audio_source.loop = true;
                                    audio_source.volume = 0f;
                                    audio_source.Play();
                                }
                                intersecting_objects.Add(new Tuple<Transform, bool>(obj.transform, is_thrown));
                            }
                        }
                    }
                    if (GlobalCtrl.Singleton.List_curMolecules != null && GlobalCtrl.Singleton.List_curMolecules.Count > 0)
                    {
                        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
                        {
                            if (bounds.Intersects(mol.GetComponent<myBoundingBox>().localBounds) && mol.isGrabbed)
                            {
                                var is_thrown = false;
                                if (!initial_scale_of_intersecting_objects.Keys.Contains(mol.transform))
                                {
                                    initial_scale_of_intersecting_objects[mol.transform] = mol.transform.localScale.x;
                                }
                                if (!localTransitionPoint.Keys.Contains(mol.transform))
                                {
                                    var box = mol.transform.GetComponent<myBoundingBox>();
                                    var local_trans_point = box.transform.InverseTransformPoint(box.localBounds.center);
                                    var tip = HandTracking.Singleton.getIndexTip();
                                    if (box.contains(tip))
                                    {
                                        local_trans_point = getTransitionPoint(box, tip);
                                    }
                                    else
                                    {
                                        is_thrown = true;
                                    }
                                    localTransitionPoint[mol.transform] = local_trans_point;
                                    var global_trans_point = mol.transform.TransformPoint(local_trans_point);
                                    var distance = Vector3.Distance(projectWSPointToScreen(global_trans_point), global_trans_point);
                                    initial_distance_of_intersecting_objects[mol.transform] = distance;

                                    //transitionPointIndicator[mol.transform] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                    //transitionPointIndicator[mol.transform].GetComponent<Renderer>().material = Resources.Load<Material>("materials/UserMaterial");
                                    //transitionPointIndicator[mol.transform].transform.SetParent(transform);
                                    //transitionPointIndicator[mol.transform].transform.localScale = 0.01f * Vector3.one;
                                    //transitionPointIndicator[mol.transform].transform.position = global_trans_point;

                                    progressBarInstances[mol.transform] = Instantiate(progressBarPrefab);
                                    progressBarInstances[mol.transform].transform.SetParent(transform);
                                    progressBarInstances[mol.transform].transform.localScale = 0.1f * Vector3.one;
                                    progressBarInstances[mol.transform].gameObject.SetActive(false);

                                    var audio_source = mol.GetComponent<AudioSource>();
                                    if (audio_source == null)
                                    {
                                        audio_source = mol.AddComponent<AudioSource>();
                                    }
                                    audio_source.clip = closeToScreenClip;
                                    audio_source.loop = true;
                                    audio_source.volume = 0f;
                                    audio_source.Play();
                                }
                                intersecting_objects.Add(new Tuple<Transform, bool>(mol.transform, is_thrown));
                            }
                        }
                    }
                    var intersecting_objects_list = intersecting_objects.Select(t => t.Item1).ToList();
                    // transition object if center pushed beyond screen
                    var is_transitioned = false;
                    foreach (var obj in intersecting_objects)
                    {
                        is_transitioned = transition(obj.Item1, obj.Item2);
                    }

                    if (SettingsData.transitionAnimation.HasFlag(TransitionManager.TransitionAnimation.SCALE))
                    {
                        if (!is_transitioned)
                        {
                            // shrink object while getting pushed in the screen
                            foreach (var obj in intersecting_objects_list)
                            {
                                shrink(obj);
                            }

                            if (old_intersecting_objects.Count > 0)
                            {
                                foreach (var oio in old_intersecting_objects)
                                {
                                    if (!intersecting_objects_list.Contains(oio))
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
                                            if (!to_remove_objects.Contains(oio))
                                            {
                                                to_remove_objects.Add(oio);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Perform removing from list
                        foreach (var obj in to_remove_objects)
                        {
                            old_intersecting_objects.Remove(obj);
                            initial_scale_of_intersecting_objects.Remove(obj);
                            initial_distance_of_intersecting_objects.Remove(obj);
                            Destroy(progressBarInstances[obj]);
                            progressBarInstances.Remove(obj);
                            //Destroy(transitionPointIndicator[obj]);
                            //transitionPointIndicator.Remove(obj);
                            localTransitionPoint.Remove(obj);
                            obj.GetComponent<AudioSource>().Stop();
                        }
                        to_remove_objects.Clear();

                        foreach (var pbi in progressBarInstances.Keys)
                        {
                            if (!old_intersecting_objects.Contains(pbi) && !intersecting_objects_list.Contains(pbi))
                            {
                                Destroy(progressBarInstances[pbi]);
                                progressBarInstances.Remove(pbi);
                            }
                        }
                    }
                }
            }
        }

        public void addObjectToGrow(Transform trans, float inital_scale)
        {
            if (!old_intersecting_objects.Contains(trans))
            {
                old_intersecting_objects.Add(trans);
            }
            initial_scale_of_intersecting_objects[trans] = inital_scale;
        }


        private Vector3 getTransitionPoint(myBoundingBox box, Vector3 gripPos)
        {
            var obj_bounds = box.localBounds;
            Vector3 middle_point = obj_bounds.center;
            List<float> dist_list = new List<float>();
            foreach (var corner in box.cornerHandles)
            {
                dist_list.Add(Vector3.Distance(corner.transform.position, projectWSPointToScreen(corner.transform.position)));

                //if (Vector3.Dot(screenNormal, gripPos - corner.transform.position) < 0f)
                //{
                //    var dist = Vector3.Distance(gripPos, corner.transform.position);
                //    if (dist > current_max_dist)
                //    {
                //        current_max_dist = dist;
                //        middle_point = gripPos + 0.5f * (corner.transform.position - gripPos);
                //    }
                //}
            }
            var min_val = dist_list.Min();
            var index = dist_list.IndexOf(min_val);
            var global_transition_point = gripPos + 0.5f * (box.cornerHandles[index].transform.position - gripPos);
            var local_transition_point = box.transform.InverseTransformPoint(global_transition_point);

            return local_transition_point;
        }

        private bool transition(Transform trans, bool is_thrown = false)
        {
            if (is_thrown)
            {
                if (!SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.THROW)) return false;
            }
            else
            {
                if (!SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.CLOSE_GRAB)) return false;
            }

            var box = trans.GetComponent<myBoundingBox>();
            var transition_point = trans.transform.TransformPoint(localTransitionPoint[trans]);

            // render progress bar
            if (GlobalCtrl.Singleton != null)
            {
                var current_distance = Vector3.Distance(transition_point, projectWSPointToScreen(transition_point));
                if (current_distance > initial_distance_of_intersecting_objects[trans])
                {
                    progressBarInstances[trans].SetActive(false);
                }
                else
                {
                    progressBarInstances[trans].SetActive(true);

                    var cam = GlobalCtrl.Singleton.currentCamera;

                    //List<float> view_dist_list = new List<float>();
                    //foreach (var corner in box.cornerHandles)
                    //{
                    //    view_dist_list.Add(Vector3.Distance(corner.transform.position, cam.transform.position));
                    //}
                    //float minVal = view_dist_list.Min();
                    //int index = view_dist_list.IndexOf(minVal);

                    //progressBarInstances[trans].transform.position = box.cornerHandles[index].transform.position - 0.01f * cam.transform.right;
                    if (SettingsData.handedness == HandTracking.Handedness.Left)
                    {
                        progressBarInstances[trans].transform.position = HandTracking.Singleton.getWristPose().position + 0.05f * cam.transform.right + 0.05f * cam.transform.forward;
                    }
                    else
                    {
                        progressBarInstances[trans].transform.position = HandTracking.Singleton.getWristPose().position - 0.05f * cam.transform.right + 0.05f * cam.transform.forward;
                    }

                    progressBarInstances[trans].transform.forward = cam.transform.forward;
                    var current_progress = 1f - current_distance / initial_distance_of_intersecting_objects[trans];
                    //progressBarInstances[trans].GetComponent<VerticalProgressBar>().setProgress(current_progress);
                    progressBarInstances[trans].GetComponent<RadialProgressBar>().setProgress(current_progress);

                    trans.GetComponent<AudioSource>().volume = Mathf.Clamp01(current_progress) * 0.75f;
                    //transitionPointIndicator[trans].transform.position = transition_point;

                }
            }


            if (Vector3.Dot(screenNormal, transition_point - projectWSPointToScreen(transition_point)) < 0f)
            {
                // directly remove stuff
                trans.GetComponent<AudioSource>().Stop();
                old_intersecting_objects.Remove(trans);
                initial_scale_of_intersecting_objects.Remove(trans);
                initial_distance_of_intersecting_objects.Remove(trans);
                Destroy(progressBarInstances[trans]);
                progressBarInstances.Remove(trans);
                //Destroy(transitionPointIndicator[obj]);
                //transitionPointIndicator.Remove(obj);
                localTransitionPoint.Remove(trans);

                // do the transition
                if (is_thrown)
                {
                    TransitionManager.Singleton.initializeTransitionClient(trans, TransitionManager.InteractionType.THROW);
                }
                else
                {
                    TransitionManager.Singleton.initializeTransitionClient(trans, TransitionManager.InteractionType.CLOSE_GRAB);
                }
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
                if (!to_remove_objects.Contains(trans))
                {
                    to_remove_objects.Add(trans);
                }
            }
        }


        public Vector3 getCurrentProjectedIndexPos()
        {
            var proj = projectIndexKnuckleOnScreen();
            return proj.Value;
        }

        public Vector2 getCurrentIndexSSPos()
        {
            var proj = projectIndexKnuckleOnScreen();
            var ss_coords = getScreenSpaceCoords(proj.Value);
            return ss_coords.Value;
        }

        public Vector3 getScreenCenter()
        {
            return screenCenter;
        }

        public Vector2 getScreenSizeWS()
        {
            var y = (indicator2.transform.position - indicator1.transform.position).magnitude;
            var x = (indicator3.transform.position - indicator1.transform.position).magnitude;
            return new Vector2(x, y);
        }

        public float getDistanceFromScreen(Vector3 pos)
        {
            var proj = projectWSPointToScreen(pos);
            return (pos - proj).magnitude;
        }

        public bool contains(Vector3 pos)
        {
            if (fullyInitialized)
            {
                return bounds.Contains(pos);
            }
            return false;
        }


        public void OnDistantTransitionGrab()
        {
            if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.DISTANT_GRAB))
            {
                var proj = projectIndexKnuckleOnScreen();
                var viewport_coords = getScreenSpaceCoords(proj.Value);
                if (EventManager.Singleton)
                {
                    EventManager.Singleton.TransitionGrab(viewport_coords.Value, TransitionManager.InteractionType.DISTANT_GRAB);
                }
            }
        }

        public void OnDistantTransitionCatch()
        {
            if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.CATCH))
            {
                var proj = projectIndexKnuckleOnScreen();
                var viewport_coords = getScreenSpaceCoords(proj.Value);
                if (EventManager.Singleton)
                {
                    EventManager.Singleton.TransitionGrab(viewport_coords.Value, TransitionManager.InteractionType.CATCH);
                }
            }
        }

        public void OnDistantTransitionGrabRelease()
        {
            if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.DISTANT_GRAB))
            {
                if (EventManager.Singleton)
                {
                    EventManager.Singleton.ReleaseTransitionGrab();
                }
            }
        }

        public void OnDistantGrab()
        {
            if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.ONSCREEN_PULL))
            {
                if (EventManager.Singleton)
                {
                    if (EventManager.Singleton)
                    {
                        EventManager.Singleton.GrabOnScreen();
                    }
                }
            }
        }

        public void OnDistantGrabRelease()
        {
            if (EventManager.Singleton)
            {
                EventManager.Singleton.ReleaseGrabOnScreen();
            }
        }

        public void OnCloseGrab()
        {
            if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.CLOSE_GRAB))
            {
                // Intentionally empty
                //var proj = projectIndexKnuckleOnScreen();
                var proj = projectIndexTipOnScreen();
                var viewport_coords = getScreenSpaceCoords(proj.Value);
                if (EventManager.Singleton)
                {
                    EventManager.Singleton.TransitionGrab(viewport_coords.Value, TransitionManager.InteractionType.CLOSE_GRAB);
                }
            }
        }

        public void OnCloseGrabRelease()
        {
            if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.CLOSE_GRAB))
            {
                if (EventManager.Singleton)
                {
                    EventManager.Singleton.ReleaseTransitionGrab();
                }
            }
        }

        private void OnSceneChange(Scene current, Scene next)
        {
            if (fullyInitialized)
            {
                if (HandTracking.Singleton)
                {
                    HandTracking.Singleton.OnMiddleFingerGrab.SetDefaultListener(OnDistantTransitionGrab);
                    HandTracking.Singleton.OnMiddleFingerGrabRelease += OnDistantTransitionGrabRelease;
                    HandTracking.Singleton.OnEmptyIndexFingerGrab.SetDefaultListener(OnDistantGrab);
                    HandTracking.Singleton.OnIndexFingerGrabRelease += OnDistantGrabRelease;
                    HandTracking.Singleton.OnCatch.SetDefaultListener(OnDistantTransitionCatch);
                }
                else
                {
                    Debug.LogError("[screenAlignment] Could not subscribe to HandTracking events.");
                }

            }
        }

        private void OnDestroy()
        {
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.OnMiddleFingerGrab.RemoveListener(OnDistantTransitionGrab);
                HandTracking.Singleton.OnMiddleFingerGrabRelease -= OnDistantTransitionGrabRelease;
                HandTracking.Singleton.OnEmptyIndexFingerGrab.RemoveListener(OnDistantGrab);
                HandTracking.Singleton.OnIndexFingerGrabRelease -= OnDistantGrabRelease;
                HandTracking.Singleton.OnCatch.RemoveDefaultListener();
            }
        }

    }
}