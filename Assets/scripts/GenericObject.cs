using chARpackStructs;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GenericObject : MonoBehaviour, IMixedRealityPointerHandler
{
    public static Dictionary<Guid, GenericObject> objects = null;
    public string objectName = "";
    public GameObject attachedModel;
    public bool isMarked = false;
    public bool isGrabbed = false;
    private Stopwatch stopwatch;
    public Guid id;

    public static GenericObject create(string name, Guid? _existingID = null)
    {

        if (Path.GetExtension(name) != "fbx")
        {
            name = Path.GetFileNameWithoutExtension(name) + ".fbx";
        }
        //Load as TextAsset
        TextAsset fileNamesAsset = Resources.Load<TextAsset>("FileNames");
        //De-serialize it
        FileNameInfo fileInfoLoaded = JsonUtility.FromJson<FileNameInfo>(fileNamesAsset.text);

        var resources_file = "";
        foreach (string fName in fileInfoLoaded.fileNames)
        {
            //UnityEngine.Debug.Log($"[GenericObject] ResourceFile: {fName}");
            if (fName.EndsWith(name))
            {
                var reduced = fName.Split("Resources/")[1];
                resources_file = Path.Join(Path.GetDirectoryName(reduced), Path.GetFileNameWithoutExtension(reduced));
                break;
            }
        }

        if (resources_file == "")
        {
            UnityEngine.Debug.LogError($"[GenericObject] Did not find {name} in resources. Abort.");
            return null;
        }

        var genericObject = Instantiate(GlobalCtrl.Singleton.myBoundingBoxPrefab).AddComponent<GenericObject>();
        genericObject.transform.SetParent(GlobalCtrl.Singleton.atomWorld.transform);
        genericObject.gameObject.name = name;

        //UnityEngine.Debug.Log($"[GenericObject] Final ResourceFile: {resources_file}");
        var model_prefab = Resources.Load<GameObject>(resources_file);
        //var model_prefab = Resources.Load<GameObject>("other/round_wooden_table/round_wooden_table");
        var model = Instantiate(model_prefab);
        foreach (Transform child in model.transform)
        {
            child.AddComponent<MeshCollider>().convex = true;
            child.AddComponent<NearInteractionGrabbable>();
            child.AddComponent<AttachedModel>().genericObject = genericObject;
        }
        var outline = model.AddComponent<Outline>();
        outline.enabled = false;
        outline.OutlineWidth = 6f;

        model.transform.SetParent(genericObject.transform);
        genericObject.attachedModel = model;

        genericObject.objectName = name;

        // TODO: we need the collider but we dont want it
        var col = genericObject.AddComponent<BoxCollider>();
        col.size = new Vector3(0.001f, 0.001f, 0.001f);
        genericObject.AddComponent<ObjectManipulator>();
        genericObject.AddComponent<NearInteractionGrabbable>();

        var box = genericObject.GetComponent<myBoundingBox>();
        box.setNormalMaterial(false);
        box.scaleCorners(0.1f);

        // initial positioning
        genericObject.transform.position = GlobalCtrl.Singleton.getIdealSpawnPos(genericObject.transform);

        if (objects == null)
        {
            objects = new Dictionary<Guid, GenericObject>();
        }

        if (_existingID != null)
        {
            genericObject.id = _existingID.Value;
            objects[_existingID.Value] = genericObject;
        }
        else
        {
            var new_id = Guid.NewGuid();
            genericObject.id = new_id;
            objects[new_id] = genericObject;
        }
        return genericObject;
    }

    public static void delete(GenericObject go)
    {
        if (objects.ContainsKey(go.id))
        {
            objects.Remove(go.id);
        }
        else
        {
            var id = objects.First(kvp => kvp.Value == go);
            objects.Remove(id.Key);
        }
        Destroy(go.gameObject);
    }

    public static void deleteAll()
    {
        if (objects != null)
        {
            foreach (var go in objects.Values)
            {
                Destroy(go.gameObject);
            }
            objects.Clear();
        }
    }

    public static void createFromSerialized(sGenericObject sgo)
    {
        var new_go = create(sgo.obj_name, sgo.ID);
        new_go.transform.localScale = sgo.scale;
        if (sgo.relQuat != Quaternion.identity)
        {
            new_go.relQuatBeforeTransition = sgo.relQuat;
            if (NetworkManagerClient.Singleton != null)
            {
                var normal = screenAlignment.Singleton.getScreenNormal();
                // TODO test if -normal or just normal (-normal does not work properly) [maybe need different approach]
                var screen_quat = Quaternion.LookRotation(-normal);
                //var screen_quat = Quaternion.LookRotation(normal);
                new_go.transform.rotation = screen_quat * sgo.relQuat;
            }
            if (NetworkManagerServer.Singleton != null)
            {
                new_go.transform.rotation = GlobalCtrl.Singleton.currentCamera.transform.rotation * sgo.relQuat;
            }
        }
        if (SettingsData.transitionMode != TransitionManager.TransitionMode.INSTANT && sgo.ssPos != Vector2.zero)
        {
            UnityEngine.Debug.Log($"[Create:transition] Got SS coords: {sgo.ssPos};");
            if (screenAlignment.Singleton)
            {
                new_go.transform.position = screenAlignment.Singleton.getWorldSpaceCoords(sgo.ssPos);
            }
            else
            {
                new_go.transform.position = GlobalCtrl.Singleton.getIdealSpawnPos(new_go.transform, sgo.ssPos);
            }
        }
        if (sgo.ssBounds != Vector4.zero)
        {
            if (NetworkManagerClient.Singleton != null)
            {
                var ss_min = new Vector2(sgo.ssBounds.x, sgo.ssBounds.y);
                var ss_max = new Vector2(sgo.ssBounds.z, sgo.ssBounds.w);
                var aligned_dist = screenAlignment.Singleton.getScreenAlignedDistanceWS(ss_min, ss_max);
                var max_size = Mathf.Max(aligned_dist.x, aligned_dist.y);
                UnityEngine.Debug.Log($"[Create:transition] max_size: {max_size}");

                var go_bounds = new_go.GetComponent<myBoundingBox>().localBounds;
                var proj_go_size = screenAlignment.Singleton.sizeProjectedToScreenWS(go_bounds);
                UnityEngine.Debug.Log($"[Create:transition] proj_go_size: {proj_go_size}");
                var max_go_size = Mathf.Max(proj_go_size.x, proj_go_size.y);
                UnityEngine.Debug.Log($"[Create:transition] max_go_size: {max_go_size}");

                var scale_factor = max_size / max_go_size;
                UnityEngine.Debug.Log($"[Create:transition] scale_factor: {scale_factor}");
                new_go.transform.localScale *= scale_factor;
            }
            if (NetworkManagerServer.Singleton != null)
            {
                new_go.transform.position = GlobalCtrl.Singleton.getIdealSpawnPos(new_go.transform, sgo.ssPos);

                var ss_min = new Vector2(sgo.ssBounds.x, sgo.ssBounds.y);
                var ss_max = new Vector2(sgo.ssBounds.z, sgo.ssBounds.w);
                var ss_diff = ss_max - ss_min;
                var sgo_max_size = Mathf.Max(ss_diff.x, ss_diff.y);

                var current_ss_bounds = new_go.GetComponent<myBoundingBox>().getScreenSpaceBounds();
                UnityEngine.Debug.Log($"[Create:transition] current bounds {current_ss_bounds}");
                var current_min = new Vector2(current_ss_bounds.x, current_ss_bounds.y);
                var current_max = new Vector2(current_ss_bounds.z, current_ss_bounds.w);
                var current_diff = current_max - current_min;
                var current_max_size = Mathf.Max(current_diff.x, current_diff.y);
                UnityEngine.Debug.Log($"[Create:transition] sgo_max_size {sgo_max_size}; current_max_size {current_max_size}");

                if (!current_max_size.approx(0f))
                {
                    var scale_factor = sgo_max_size / current_max_size;
                    UnityEngine.Debug.Log($"[Create:transition] scale_factor {scale_factor}");
                    new_go.transform.localScale *= scale_factor;
                }
            }
        }
        // EventManager.Singleton.MoleculeLoaded(tempMolecule); TODO: implement for sync mode
        UnityEngine.Debug.Log($"[GO:Create:transition] transitioned {sgo.transitioned}; triggered by {sgo.transitionTriggeredBy}");
        if (sgo.transitioned)
        {
            EventManager.Singleton.ReceiveGenericObjectTransition(new_go, (TransitionManager.InteractionType)sgo.transitionTriggeredBy);
        }
    }


    public void Hover(bool value)
    {
        GetComponent<myBoundingBox>().setHovering(value);
    }

    private void Start()
    {
        if (HandTracking.Singleton)
        {
            HandTracking.Singleton.OnMiddleFingerGrab += OnTransitionGrab;
            HandTracking.Singleton.OnMiddleFingerGrabRelease += OnTransitionGrabRelease;
            //HandTracking.Singleton.OnIndexFingerGrab += OnNormalGrab;
            //HandTracking.Singleton.OnIndexFingerGrabRelease += OnNormalGrabRelease;
        }
    }

    private void OnDestroy()
    {
        if (HandTracking.Singleton)
        {
            HandTracking.Singleton.OnMiddleFingerGrab -= OnTransitionGrab;
            HandTracking.Singleton.OnMiddleFingerGrabRelease -= OnTransitionGrabRelease;
        }
    }

    private Stopwatch transitionGrabCoolDown = Stopwatch.StartNew();
    private void OnTransitionGrab(Vector3 pos)
    {
        if (GetComponent<myBoundingBox>().contains(pos))
        {
            if (SettingsData.syncMode == TransitionManager.SyncMode.Async)
            {
                transitionGrabCoolDown?.Stop();
                if (transitionGrabCoolDown?.ElapsedMilliseconds < 800)
                {
                    transitionGrabCoolDown.Start();
                    return;
                }
                TransitionManager.Singleton.initializeTransitionClient(transform, TransitionManager.InteractionType.DISTANT_GRAB);
                transitionGrabCoolDown.Restart();
            }
        }
    }
    public Quaternion relQuatBeforeTransition = Quaternion.identity;

    private void OnTransitionGrabRelease()
    {

    }

//#region mouse_interaction

//#if UNITY_STANDALONE || UNITY_EDITOR
//    public static bool anyArcball;
//    private bool arcball;
//    private Vector3 oldMousePosition;
//    private Vector3 newMousePosition;
//    public void Update()
//    {
//        if (SceneManager.GetActiveScene().name == "ServerScene")
//        {
//            if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftShift) && mouseOverObject())
//            {
//                arcball = true; anyArcball = true;
//                oldMousePosition = Input.mousePosition;
//                newMousePosition = Input.mousePosition;
//            }
//            if (Input.GetMouseButtonUp(1) || !Input.GetKey(KeyCode.LeftShift))
//            {
//                arcball = false; anyArcball = false;
//            }

//            if (arcball)
//            {
//                oldMousePosition = newMousePosition;
//                newMousePosition = Input.mousePosition;
//                if (newMousePosition != oldMousePosition)
//                {
//                    var vector2 = getArcballVector(newMousePosition);
//                    var vector1 = getArcballVector(oldMousePosition);
//                    float angle = (float)Math.Acos(Vector3.Dot(vector1, vector2));
//                    var axis_cam = Vector3.Cross(vector1, vector2);

//                    Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
//                    Matrix4x4 modelMatrix = transform.localToWorldMatrix;
//                    Matrix4x4 cameraToObjectMatrix = Matrix4x4.Inverse(viewMatrix * modelMatrix);
//                    var axis_world = cameraToObjectMatrix * axis_cam;

//                    transform.RotateAround(transform.position, axis_world, 2 * Mathf.Rad2Deg * angle);
//                }
//            }
//        }
//    }

//    private bool isBlockedByUI()
//    {
//        PointerEventData eventData = new PointerEventData(EventSystem.current);
//        eventData.position = Input.mousePosition;

//        List<RaycastResult> raysastResults = new List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, raysastResults);

//        if (raysastResults.Count > 0)
//        {
//            if (raysastResults[0].gameObject.layer == LayerMask.NameToLayer("UI"))
//            {
//                return true;
//            }
//        }
//        return false;
//    }

//    private bool mouseOverObject()
//    {
//        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//        if (Physics.Raycast(ray, out RaycastHit hit))
//        {
//            if (hit.collider == attachedModel.GetComponent<MeshCollider>())
//            {
//                return true;
//            }
//        }
//        return false;
//    }

//    private Vector3 getArcballVector(Vector3 inputPos)
//    {
//        Vector3 vector = CameraSwitcher.Singleton.currentCam.ScreenToViewportPoint(inputPos);
//        vector = -vector;
//        if (vector.x * vector.x + vector.y * vector.y <= 1)
//        {
//            vector.z = (float)Math.Sqrt(1 - vector.x * vector.x - vector.y * vector.y);
//        }
//        else
//        {
//            vector = vector.normalized;
//        }
//        return vector;
//    }


//    // offset for mouse interaction
//    public Vector3 mouse_offset = Vector3.zero;


//    void OnMouseDown()
//    {
//        UnityEngine.Debug.Log("blub");
//        // Handle server GUI interaction
//        if (EventSystem.current.IsPointerOverGameObject()) { return; }


//        mouse_offset = gameObject.transform.position - GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
//         new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f));

//        stopwatch = Stopwatch.StartNew();
//        isGrabbed = true;
//        processHighlights();
//    }

//    void OnMouseDrag()
//    {
//        if (EventSystem.current.IsPointerOverGameObject()) { return; }
//        Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f);
//        transform.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(newPosition) + mouse_offset;
//    }

//    private void OnMouseUp()
//    {
//        if (EventSystem.current.IsPointerOverGameObject()) { return; }

//        // reset outline
//        isGrabbed = false;


//        stopwatch?.Stop();
//        if (stopwatch?.ElapsedMilliseconds < 200)
//        {
//            toggleMarkObject();
//        }

//        processHighlights();
//    }

//#endif
//#endregion

    public void toggleMarkObject()
    {
        isMarked = !isMarked;
    }

    public void processHighlights()
    {
        var outline = attachedModel.GetComponent<Outline>();
        if (!isMarked)
        {
            outline.enabled = isGrabbed;
            outline.OutlineColor = chARpackColorPalette.ColorPalette.atomGrabColor;
        }
        else
        {
            outline.enabled = true;
            outline.OutlineColor = chARpackColorPalette.ColorPalette.atomSelectionColor;
        }
    }



    private Vector3 pickupPos = Vector3.zero;
    private Quaternion pickupRot = Quaternion.identity;
    /// <summary>
    /// This method is triggered when a grab/select gesture is started.
    /// Sets the generic object to grabbed.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        pickupPos = transform.localPosition;
        pickupRot = transform.localRotation;
        isGrabbed = true;
        stopwatch = Stopwatch.StartNew();
        // change material of grabbed object
        GetComponent<myBoundingBox>().setGrabbed(true);
        processHighlights();
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        // Intentionally empty
    }

    /// <summary>
    /// This method is triggered when the grabbed molecule is dragged.
    /// It invokes a network event to keep molecule positions synchronized.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        //if (!frozen)
        //{
        //    // keep everything relative to atom world
        //    EventManager.Singleton.MoveMolecule(m_id, transform.localPosition, transform.localRotation);
        //}
    }

    /// <summary>
    /// This function is triggered when a grabbed molecule is dropped.
    /// It ends the grabbed status of the molecule, marks it if less than
    /// the maximum timespan for the select gesture has elapsed and checks for/performs
    /// potential merges.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        stopwatch?.Stop();
        if (isGrabbed)
        {
            isGrabbed = false;
            if (stopwatch?.ElapsedMilliseconds < 200)
            {
                transform.localPosition = pickupPos;
                transform.localRotation = pickupRot;
                //EventManager.Singleton.MoveMolecule(m_id, transform.localPosition, transform.localRotation);
                toggleMarkObject();
                // TODO open tool tip
            }
            // change material back to normal
            GetComponent<myBoundingBox>().setGrabbed(false);
            processHighlights();
        }
    }
}