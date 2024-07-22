using chARpackStructs;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using OpenBabel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public class GenericObject : MonoBehaviour
{
    static Dictionary<Guid, GenericObject> objects = null;
    public string objectName = "";
    public GameObject attachedModel;
    public bool isMarked = false;
    public bool isGrabbed = false;

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
            UnityEngine.Debug.Log($"[GenericObject] ResourceFile: {fName}");
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

        UnityEngine.Debug.Log($"[GenericObject] Final ResourceFile: {resources_file}");
        var model_prefab = Resources.Load<GameObject>(resources_file);
        //var model_prefab = Resources.Load<GameObject>("other/round_wooden_table/round_wooden_table");
        var model = Instantiate(model_prefab);
        var collider = model.AddComponent<MeshCollider>();
        collider.convex = true;
        model.AddComponent<ObjectManipulator>();
        model.AddComponent<NearInteractionGrabbable>();
        var outline = model.AddComponent<Outline>();
        outline.enabled = false;
        outline.OutlineWidth = 6f;
        model.AddComponent<AttachedModel>().genericObject = genericObject;

        model.transform.SetParent(genericObject.transform);
        genericObject.attachedModel = model;

        genericObject.transform.position = GlobalCtrl.Singleton.getCurrentSpawnPos();
        genericObject.objectName = name;

        // TODO: we need the collider but we dont want it
        var col = genericObject.AddComponent<BoxCollider>();
        col.size = new Vector3(0.001f, 0.001f, 0.001f);
        genericObject.AddComponent<ObjectManipulator>();
        genericObject.AddComponent<NearInteractionGrabbable>();

        var box = genericObject.GetComponent<myBoundingBox>();
        box.setNormalMaterial(false);
        box.scaleCorners(0.1f);


        if (objects == null)
        {
            objects = new Dictionary<Guid, GenericObject>();
        }

        if (_existingID != null)
        {
            objects[_existingID.Value] = genericObject;
        }
        else
        {
            objects[Guid.NewGuid()] = genericObject;
        }
        return genericObject;
    }

    public static void delete(GenericObject go)
    {
        objects.Remove(go.getID());
        Destroy(go);
    }

    public static void createFromSerialized(sGenericObject sgo)
    {
        var new_go = create(sgo.obj_name, sgo.ID);

        if (sgo.relQuat != Quaternion.identity)
        {
            new_go.relQuatBeforeTransition = sgo.relQuat;
            if (NetworkManagerClient.Singleton != null)
            {
                var normal = screenAlignment.Singleton.getScreenNormal();
                // TODO test if -normal or just normal (-normal does not work properly) [maybe need different approach]
                //var screen_quat = Quaternion.LookRotation(-normal);
                var screen_quat = Quaternion.LookRotation(normal);
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
                new_go.transform.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(new Vector3(sgo.ssPos.x, sgo.ssPos.y, 0.4f));
            }
        }
        if (sgo.ssBounds != Vector4.zero)
        {
            new_go.transform.localScale = Vector3.one;
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
                var ss_min = new Vector2(sgo.ssBounds.x, sgo.ssBounds.y);
                var ss_max = new Vector2(sgo.ssBounds.z, sgo.ssBounds.w);
                var ss_diff = ss_max - ss_min;
                var sgo_max_size = Mathf.Max(ss_diff.x, ss_diff.y);

                var current_ss_bounds = new_go.GetComponent<myBoundingBox>().getScreenSpaceBounds();
                var current_min = new Vector2(current_ss_bounds.x, current_ss_bounds.y);
                var current_max = new Vector2(current_ss_bounds.z, current_ss_bounds.w);
                var current_diff = current_max - current_min;
                var current_max_size = Mathf.Max(current_diff.x, current_diff.y);

                UnityEngine.Debug.Log($"[Create:transition] sgo_max_size {sgo_max_size}; current_max_size {current_max_size}");

                var scale_factor = sgo_max_size / current_max_size;
                UnityEngine.Debug.Log($"[Create:transition] scale_factor {scale_factor}");
                new_go.transform.localScale *= scale_factor;
            }
        }
        // EventManager.Singleton.MoleculeLoaded(tempMolecule);

        if (sgo.transitioned)
        {
            EventManager.Singleton.ReceiveGenericObjectTransition(new_go);
        }
    }


    public Guid getID()
    {
        return objects.Single(item => item.Value == this).Key;
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
                TransitionManager.Singleton.initializeTransitionClient(transform);
                transitionGrabCoolDown.Restart();
            }
        }
    }
    public Quaternion relQuatBeforeTransition = Quaternion.identity;

    private void OnTransitionGrabRelease()
    {

    }

#region mouse_interaction

#if UNITY_STANDALONE || UNITY_EDITOR
    public static bool anyArcball;
    private bool arcball;
    private Vector3 oldMousePosition;
    private Vector3 newMousePosition;
    public void Update()
    {
        if (SceneManager.GetActiveScene().name == "ServerScene")
        {
            if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftShift) && mouseOverObject())
            {
                arcball = true; anyArcball = true;
                oldMousePosition = Input.mousePosition;
                newMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1) || !Input.GetKey(KeyCode.LeftShift))
            {
                arcball = false; anyArcball = false;
            }

            if (arcball)
            {
                oldMousePosition = newMousePosition;
                newMousePosition = Input.mousePosition;
                if (newMousePosition != oldMousePosition)
                {
                    var vector2 = getArcballVector(newMousePosition);
                    var vector1 = getArcballVector(oldMousePosition);
                    float angle = (float)Math.Acos(Vector3.Dot(vector1, vector2));
                    var axis_cam = Vector3.Cross(vector1, vector2);

                    Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
                    Matrix4x4 modelMatrix = transform.localToWorldMatrix;
                    Matrix4x4 cameraToObjectMatrix = Matrix4x4.Inverse(viewMatrix * modelMatrix);
                    var axis_world = cameraToObjectMatrix * axis_cam;

                    transform.RotateAround(transform.position, axis_world, 2 * Mathf.Rad2Deg * angle);
                }
            }
        }
    }

    private bool isBlockedByUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);

        if (raysastResults.Count > 0)
        {
            if (raysastResults[0].gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                return true;
            }
        }
        return false;
    }

    private bool mouseOverObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider == attachedModel.GetComponent<MeshCollider>())
            {
                return true;
            }
        }
        return false;
    }

    private Vector3 getArcballVector(Vector3 inputPos)
    {
        Vector3 vector = CameraSwitcher.Singleton.currentCam.ScreenToViewportPoint(inputPos);
        vector = -vector;
        if (vector.x * vector.x + vector.y * vector.y <= 1)
        {
            vector.z = (float)Math.Sqrt(1 - vector.x * vector.x - vector.y * vector.y);
        }
        else
        {
            vector = vector.normalized;
        }
        return vector;
    }


    // offset for mouse interaction
    public Vector3 mouse_offset = Vector3.zero;
    private Stopwatch stopwatch;

    void OnMouseDown()
    {
        UnityEngine.Debug.Log("blub");
        // Handle server GUI interaction
        if (EventSystem.current.IsPointerOverGameObject()) { return; }


        mouse_offset = gameObject.transform.position - GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
         new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f));

        stopwatch = Stopwatch.StartNew();
        isGrabbed = true;
        grabHighlight(true);
    }

    void OnMouseDrag()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }
        Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f);
        transform.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(newPosition) + mouse_offset;
    }

    private void OnMouseUp()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        // reset outline
        isGrabbed = false;
        grabHighlight(false);

        stopwatch?.Stop();
        if (stopwatch?.ElapsedMilliseconds < 200)
        {
            toggleMarkObject();
        }

    }

#endif
#endregion

    public void toggleMarkObject()
    {
        isMarked = !isMarked;
    }

    public void grabHighlight(bool v)
    {
        var outline = attachedModel.GetComponent<Outline>();
        if (!isMarked)
        {
            outline.enabled = v;
            outline.OutlineColor = chARpackColorPalette.ColorPalette.atomGrabColor;
        }
        else
        {
            outline.enabled = true;
            outline.OutlineColor = chARpackColorPalette.ColorPalette.atomSelectionColor;
        }
    }
}
