using Microsoft.MixedReality.Toolkit.Input;
using System.Linq;
using UnityEngine;

namespace chARpack
{
    /// <summary>
    /// This class provides the functionality for a bounding box enclosing a molecule.
    /// </summary>
    public class myBoundingBox : MonoBehaviour
    {

        public Material myLineMaterialWithFade;
        public Material myLineMaterialWithoutFade;
        public Material myLineGrabbedMaterial;
        public Material myHandleMaterialWithFade;
        public Material myHandleMaterialWithoutFade;
        public Material myHandleGrabbedMaterial;
        public GameObject cornerHandle;

        [HideInInspector] public LineRenderer myLR;
        [HideInInspector] public GameObject[] cornerHandles = new GameObject[8];
        private Material handleHoverMaterial;
        private Material lineHoverMaterial;

        private Quaternion[] cornerOrientation = new Quaternion[8];
        private Bounds _localBounds;
        public Bounds localBounds { get { getBounds(); return _localBounds; } private set { _localBounds = value; } }
        private GameObject boxGO;

        public bool fade = true;

        private void getBounds()
        {
            //localBounds = myLR.bounds;
            //localBounds = new Bounds(this.transform.position, Vector3.zero);
            var renderers = GetComponentsInChildren<Renderer>();
            bool first = true;
            foreach (var render in renderers)
            {
                if (render != myLR && !cornerHandles.Contains(render.transform.gameObject))
                {
                    if (first)
                    {
                        _localBounds = render.bounds;
                        first = false;
                    }
                    else
                    {
                        _localBounds.Encapsulate(render.bounds);
                    }
                }
            }
        }

        public Vector4 getScreenSpaceBounds()
        {
            getBounds();
            if (NetworkManagerServer.Singleton != null)
            {
                var cam = GlobalCtrl.Singleton.currentCamera;
                if (cam == null)
                {
                    cam = Camera.main;
                }
                var boundPoints = _localBounds.GetCorners();

                Vector2 ss_min = Vector2.one * float.MaxValue;
                Vector2 ss_max = Vector2.zero;
                foreach (var point in boundPoints)
                {
                    var ss_coord = cam.WorldToScreenPoint(point);
                    ss_min = Vector2.Min(ss_min, ss_coord);
                    ss_max = Vector2.Max(ss_max, ss_coord);
                }
                return new Vector4(ss_min.x, ss_min.y, ss_max.x, ss_max.y);
            }
            if (NetworkManagerClient.Singleton != null)
            {
                var corners = _localBounds.GetCorners();
                Vector2 ss_min = Vector2.one * float.MaxValue;
                Vector2 ss_max = Vector2.zero;
                if (screenAlignment.Singleton == null) return Vector4.zero;
                foreach (var corner in corners)
                {
                    var proj_corner = screenAlignment.Singleton.projectWSPointToScreen(corner);
                    var ss_corner = screenAlignment.Singleton.getScreenSpaceCoords(proj_corner);

                    ss_min = Vector2.Min(ss_min, ss_corner.Value);
                    ss_max = Vector2.Max(ss_max, ss_corner.Value);
                }

                return new Vector4(ss_min.x, ss_min.y, ss_max.x, ss_max.y);
            }
            return Vector4.zero;
        }

        public Vector3 getSize()
        {
            return localBounds.size;
        }

        public bool contains(Vector3 pos)
        {
            return localBounds.Contains(pos);
        }

        public Bounds getCopyOfBounds()
        {
            return new Bounds(_localBounds.center, _localBounds.size);
        }

        public bool containedInHandles(Vector3 pos)
        {
            foreach (var handle in cornerHandles)
            {
                if (handle.GetComponent<BoxCollider>().bounds.Contains(pos)) return true;
            }
            return false;
        }

        private void renderCube()
        {
            // note: line is drawn continously
            // render 12 edges of a cube

            myLR.SetPosition(0, _localBounds.min); // LBF
            myLR.SetPosition(1, new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.min.z));
            myLR.SetPosition(2, new Vector3(_localBounds.max.x, _localBounds.max.y, _localBounds.min.z));
            myLR.SetPosition(3, new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.min.z));
            myLR.SetPosition(4, _localBounds.min);
            myLR.SetPosition(5, new Vector3(_localBounds.min.x, _localBounds.min.y, _localBounds.max.z));
            myLR.SetPosition(6, new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.max.z));
            myLR.SetPosition(7, _localBounds.max);
            myLR.SetPosition(8, new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.max.z));
            myLR.SetPosition(9, new Vector3(_localBounds.min.x, _localBounds.min.y, _localBounds.max.z));
            myLR.SetPosition(10, new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.max.z));
            myLR.SetPosition(11, new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.min.z));
            myLR.SetPosition(12, new Vector3(_localBounds.max.x, _localBounds.max.y, _localBounds.min.z));
            myLR.SetPosition(13, _localBounds.max);
            myLR.SetPosition(14, new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.max.z));
            myLR.SetPosition(15, new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.min.z));

        }

        //private void bakeLineToMesh()
        //{
        //    var meshFilter = gameObject.AddComponent<MeshFilter>();
        //    Mesh mesh = new Mesh();
        //    myLR.BakeMesh(mesh);
        //    meshFilter.sharedMesh = mesh;

        //    var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        //    meshRenderer.sharedMaterial = myLineMaterial;

        //    Destroy(myLR);
        //}

        private void Awake()
        {
            boxGO = new GameObject("box");
            boxGO.transform.parent = transform;

            myLR = boxGO.AddComponent<LineRenderer>();
            myLR.sharedMaterial = myLineMaterialWithFade;
            //myLR.useWorldSpace = false;
            myLR.startWidth = 0.001f;
            myLR.endWidth = 0.001f;
            myLR.positionCount = 16;

            getBounds();
            generateCorners();

            handleHoverMaterial = Resources.Load<Material>("materials/BoundingBoxHandleHover");
            lineHoverMaterial = Resources.Load<Material>("materials/BoundingBoxLineHover");
        }

        public void show(bool value)
        {
            boxGO.SetActive(value);
        }

        private void generateCorners()
        {
            Vector3[] corners = new Vector3[8];
            corners[0] = _localBounds.max;
            corners[1] = new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.max.z);
            corners[2] = new Vector3(_localBounds.min.x, _localBounds.min.y, _localBounds.max.z);
            corners[3] = new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.max.z);

            corners[4] = new Vector3(_localBounds.max.x, _localBounds.max.y, _localBounds.min.z);
            corners[5] = new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.min.z);
            corners[6] = _localBounds.min;
            corners[7] = new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.min.z);

            cornerOrientation[0] = Quaternion.Euler(0, 90, 0);
            cornerOrientation[1] = Quaternion.Euler(0, 0, 0);
            cornerOrientation[2] = Quaternion.Euler(0, 0, 90);
            cornerOrientation[3] = Quaternion.Euler(0, 0, -180);
            cornerOrientation[4] = Quaternion.Euler(0, 180, 0);
            cornerOrientation[5] = Quaternion.Euler(0, -90, 0);
            cornerOrientation[6] = Quaternion.Euler(180, 0, 0);
            cornerOrientation[7] = Quaternion.Euler(0, 90, 180);

            //var cornerPosRot = corners.Zip(cornerOrientation, (c, q) => new { Corner = c, Quat = q });

            // add colliders on all 8 corners
            for (int i = 0; i < corners.Length; i++)
            {

                cornerHandles[i] = Instantiate(cornerHandle, boxGO.transform);
                cornerHandles[i].transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                //cornerHandles[i].transform.SetPositionAndRotation(corners[i], cornerOrientation[i]);
                cornerHandles[i].transform.position = corners[i];
                cornerHandles[i].transform.rotation = cornerOrientation[i];
                cornerHandles[i].AddComponent<BoxCollider>();
                cornerHandles[i].AddComponent<NearInteractionGrabbable>();
                cornerHandles[i].AddComponent<cornerClickScript>();

                if (myHandleMaterialWithFade != null)
                {
                    Renderer[] renderers = cornerHandles[i].GetComponentsInChildren<Renderer>();

                    for (int j = 0; j < renderers.Length; ++j)
                    {
                        renderers[j].material = myHandleMaterialWithFade;
                    }
                }

            }
        }

        private void updateCorners()
        {
            Vector3[] corners = new Vector3[8];
            corners[0] = _localBounds.max;
            corners[1] = new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.max.z);
            corners[2] = new Vector3(_localBounds.min.x, _localBounds.min.y, _localBounds.max.z);
            corners[3] = new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.max.z);

            corners[4] = new Vector3(_localBounds.max.x, _localBounds.max.y, _localBounds.min.z);
            corners[5] = new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.min.z);
            corners[6] = _localBounds.min;
            corners[7] = new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.min.z);

            var scale_normalization = Vector3.one.divide(transform.localScale);
            for (int i = 0; i < corners.Length; i++)
            {
                cornerHandles[i].transform.position = corners[i];
                cornerHandles[i].transform.rotation = cornerOrientation[i];
                cornerHandles[i].transform.localScale = (0.02f + 0.05f * _localBounds.size.maxDimValue()) * scale_normalization;

            }
        }

        /// <summary>
        /// Scales the corners of the bounding box.
        /// This is used to keep the corner size consistent with molecule size.
        /// </summary>
        /// <param name="scale"></param>
        //public void scaleCorners(float scale)
        //{
        //    var vec_scale = Vector3.one * scale;
        //    foreach (var corner in cornerHandles)
        //    {
        //        corner.transform.localScale = vec_scale;
        //    }
        //}

        /// <summary>
        /// Sets the bounding box materials fade property.
        /// </summary>
        /// <param name="fade_">whether to use the material with fade effect</param>
        public void setNormalMaterial(bool fade_)
        {
            fade = fade_;
            Material line;
            Material handles;
            if (fade)
            {
                line = myLineMaterialWithFade;
                handles = myHandleMaterialWithFade;
            }
            else
            {
                line = myLineMaterialWithoutFade;
                handles = myHandleMaterialWithoutFade;
            }

            boxGO.GetComponent<LineRenderer>().material = line;
            foreach (var corner in cornerHandles)
            {
                corner.GetComponentInChildren<MeshRenderer>().material = handles;
            }
        }

        /// <summary>
        /// Changes the visuals of lines and corners when the bounding box is grabbed.
        /// </summary>
        /// <param name="grabbed">whether the box is grabbed</param>
        bool isGrabbed = false;
        public void setGrabbed(bool grabbed)
        {
            isGrabbed = grabbed;
            if (grabbed)
            {
                if (myHandleGrabbedMaterial != null)
                {
                    foreach (var handle in cornerHandles)
                    {
                        handle.GetComponentInChildren<MeshRenderer>().material = myHandleGrabbedMaterial;
                    }
                }
                if (myLineGrabbedMaterial != null)
                {
                    myLR.material = myLineGrabbedMaterial;
                }
            }
            else
            {
                setNormalMaterial(fade);
            }
        }

        public void setHovering(bool hovering)
        {
            if (isGrabbed) return;
            if (hovering)
            {
                if (handleHoverMaterial != null)
                {
                    foreach (var handle in cornerHandles)
                    {
                        handle.GetComponentInChildren<MeshRenderer>().material = handleHoverMaterial;
                    }
                }
                if (lineHoverMaterial != null)
                {
                    myLR.material = lineHoverMaterial;
                }
            }
            else
            {
                setNormalMaterial(fade);
            }
        }

        public void Update()
        {
            getBounds();
            renderCube();
            updateCorners();
        }

    }
}
