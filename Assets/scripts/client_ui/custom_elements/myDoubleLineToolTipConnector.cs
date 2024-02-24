// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// Connects a ToolTip to a target
    /// Maintains that connection even if the target moves
    /// </summary>
    public class myDoubleLineToolTipConnector : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The GameObject to which the tooltip is connected")]
        private GameObject target1;

        /// <summary>
        /// The GameObject to which the tooltip is connected
        /// </summary>
        public GameObject Target1
        {
            get { return target1; }
            set { target1 = value; }
        }

        [SerializeField]
        [Tooltip("The GameObject to which the tooltip is connected")]
        private GameObject target2;

        /// <summary>
        /// The GameObject to which the tooltip is connected
        /// </summary>
        public GameObject Target2
        {
            get { return target2; }
            set { target2 = value; }
        }

        [SerializeField]
        private DoubleLineDynamicToolTip toolTip;

        public enum Target { Target1, Target2};

        [SerializeField]
        [Tooltip("Pick target")]
        private Target whichTarget = Target.Target1;

        /// <summary>
        /// Which target to orient to
        /// </summary>
        public Target WhichTarget
        {
            get { return whichTarget; }
            set { whichTarget = value; }
        }


        [SerializeField]
        [Tooltip("The follow style of the tooltip connector")]
        private ConnectorFollowType connectorFollowType = ConnectorFollowType.AnchorOnly;

        /// <summary>
        /// The follow style of the tooltip connector
        /// </summary>
        public ConnectorFollowType ConnectorFollowingType
        {
            get { return connectorFollowType; }
            set { connectorFollowType = value; }
        }

        [SerializeField]
        [Tooltip("Is the connector pivot set manually or automatically?")]
        private ConnectorPivotMode pivotMode = ConnectorPivotMode.Manual;

        /// <summary>
        /// Is the connector pivot set manually or automatically?
        /// </summary>
        public ConnectorPivotMode PivotMode
        {
            get { return pivotMode; }
            set { pivotMode = value; }
        }

        [SerializeField]
        [Tooltip("The direction of the connector")]
        private ConnectorPivotDirection pivotDirection = ConnectorPivotDirection.North;

        /// <summary>
        /// The direction of the connector
        /// </summary>
        public ConnectorPivotDirection PivotDirection
        {
            get { return pivotDirection; }
            set { pivotDirection = value; }
        }

        [SerializeField]
        [Tooltip("orientation style for connector")]
        private ConnectorOrientType pivotDirectionOrient = ConnectorOrientType.OrientToObject;

        /// <summary>
        /// Orientation style for connector
        /// </summary>
        public ConnectorOrientType PivotDirectionOrient
        {
            get { return pivotDirectionOrient; }
            set { pivotDirectionOrient = value; }
        }

        [SerializeField]
        [Tooltip("The direction of the manual pivot.")]
        private Vector3 manualPivotDirection = Vector3.up;

        /// <summary>
        /// The direction of the manual pivot.
        /// </summary>
        public Vector3 ManualPivotDirection
        {
            get { return manualPivotDirection; }
            set { manualPivotDirection = value; }
        }

        [SerializeField]
        private Vector3 manualPivotLocalPosition = Vector3.up;

        /// <summary>
        /// getter/setter for local pivot position
        /// </summary>
        public Vector3 ManualPivotLocalPosition
        {
            get { return manualPivotLocalPosition; }
            set { manualPivotLocalPosition = value; }
        }

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("Set Distance from object that Tooltip pivots around.")]
        private float pivotDistance = 0.25f;

        /// <summary>
        /// Set Distance from object that Tooltip pivots around.
        /// </summary>
        public float PivotDistance
        {
            get { return pivotDistance; }
            set { pivotDistance = Mathf.Min(2.0f, Mathf.Max(0, value)); }
        }

        private void OnEnable()
        {
            toolTip = gameObject.GetComponent<DoubleLineDynamicToolTip>();
            if (toolTip == null)
            {
                Debug.LogWarning("This component only works with a ToolTip.");
                enabled = false;
                return;
            }

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (target2 == null || target2 == null)
            {
                return;
            }

            var target = target1;
            if (whichTarget == Target.Target2)
            {
                target = target2;
            }

            switch (connectorFollowType)
            {
                case ConnectorFollowType.AnchorOnly:
                default:
                    // Set the position of the anchor to the target's position
                    // And do nothing else
                    toolTip.Anchor1.transform.position = target1.transform.position;
                    toolTip.Anchor2.transform.position = target2.transform.position;
                    break;

                case ConnectorFollowType.Position:
                    // Move the entire tooltip transform while maintaining the anchor position offset
                    toolTip.transform.position = target.transform.position;
                    switch (PivotMode)
                    {
                        case ConnectorPivotMode.Automatic:
                            Transform relativeTo = null;
                            switch (PivotDirectionOrient)
                            {
                                case ConnectorOrientType.OrientToCamera:
                                    relativeTo = CameraCache.Main.transform;
                                    break;

                                case ConnectorOrientType.OrientToObject:
                                    relativeTo = target.transform;
                                    break;
                            }

                            toolTip.PivotPosition = target.transform.position + GetDirectionFromPivotDirection(
                                PivotDirection,
                                ManualPivotDirection,
                                relativeTo) * PivotDistance;
                            break;

                        case ConnectorPivotMode.LocalPosition:
                            toolTip.PivotPosition = target.transform.position + target.transform.TransformPoint(manualPivotLocalPosition);
                            break;

                        case ConnectorPivotMode.Manual:
                            // Do nothing
                            break;
                    }
                    break;

                case ConnectorFollowType.PositionAndYRotation:
                    // Set the transform of the entire tool tip
                    // Set the pivot relative to target/camera
                    toolTip.transform.position = target.transform.position;
                    Vector3 eulerAngles = target.transform.eulerAngles;
                    eulerAngles.x = 0f;
                    eulerAngles.z = 0f;
                    toolTip.transform.eulerAngles = eulerAngles;

                    switch (PivotMode)
                    {
                        case ConnectorPivotMode.Automatic:
                            Transform relativeTo = null;
                            switch (PivotDirectionOrient)
                            {
                                case ConnectorOrientType.OrientToCamera:
                                    relativeTo = CameraCache.Main.transform;
                                    break;

                                case ConnectorOrientType.OrientToObject:
                                    relativeTo = target.transform;
                                    break;
                            }
                            Vector3 localPosition = GetDirectionFromPivotDirection(PivotDirection, ManualPivotDirection, relativeTo) * PivotDistance;
                            toolTip.PivotPosition = target.transform.position + localPosition;
                            break;

                        case ConnectorPivotMode.LocalPosition:
                            toolTip.PivotPosition = target.transform.position + target.transform.TransformPoint(manualPivotLocalPosition);
                            break;

                        case ConnectorPivotMode.Manual:
                            // Do nothing
                            break;
                    }
                    break;

                case ConnectorFollowType.PositionAndXYRotation:
                    // Set the transform of the entire tool tip
                    // Set the pivot relative to target/camera
                    toolTip.transform.position = target.transform.position;
                    toolTip.transform.rotation = target.transform.rotation;
                    switch (PivotMode)
                    {
                        case ConnectorPivotMode.Automatic:
                            Transform relativeTo = null;
                            switch (PivotDirectionOrient)
                            {
                                case ConnectorOrientType.OrientToCamera:
                                    relativeTo = CameraCache.Main.transform;
                                    break;

                                case ConnectorOrientType.OrientToObject:
                                    relativeTo = target.transform;
                                    break;
                            }
                            toolTip.PivotPosition = target.transform.position + GetDirectionFromPivotDirection(
                                PivotDirection,
                                ManualPivotDirection,
                                relativeTo) * PivotDistance;
                            break;

                        case ConnectorPivotMode.LocalPosition:
                            toolTip.PivotPosition = target.transform.position + target.transform.TransformPoint(manualPivotLocalPosition);
                            break;

                        case ConnectorPivotMode.Manual:
                            // Do nothing
                            break;
                    }
                    break;
            }
        }

        private void Update()
        {
            UpdatePosition();
        }

        /// <summary>
        /// Computes the director of the connector 
        /// </summary>
        /// <param name="pivotDirection">enum describing director of connector pivot</param>
        /// <param name="manualPivotDirection">is the pivot set manually</param>
        /// <param name="relativeTo">Transform that describes the frame of reference of the pivot</param>
        /// <returns>a vector describing the pivot direction in world space</returns>
        public static Vector3 GetDirectionFromPivotDirection(ConnectorPivotDirection pivotDirection, Vector3 manualPivotDirection, Transform relativeTo)
        {
            Vector3 dir = Vector3.zero;

            switch (pivotDirection)
            {
                case ConnectorPivotDirection.North:
                    dir = Vector3.up;
                    break;

                case ConnectorPivotDirection.Northeast:
                    dir = Vector3.Lerp(Vector3.up, Vector3.right, 0.5f).normalized;
                    break;

                case ConnectorPivotDirection.East:
                    dir = Vector3.right;
                    break;

                case ConnectorPivotDirection.Southeast:
                    dir = Vector3.Lerp(Vector3.down, Vector3.right, 0.5f).normalized;
                    break;

                case ConnectorPivotDirection.South:
                    dir = Vector3.down;
                    break;

                case ConnectorPivotDirection.Southwest:
                    dir = Vector3.Lerp(Vector3.down, Vector3.left, 0.5f).normalized;
                    break;

                case ConnectorPivotDirection.West:
                    dir = Vector3.left;
                    break;

                case ConnectorPivotDirection.Northwest:
                    dir = Vector3.Lerp(Vector3.up, Vector3.left, 0.5f).normalized;
                    break;

                case ConnectorPivotDirection.InFront:
                    dir = Vector3.forward;
                    break;

                case ConnectorPivotDirection.Manual:
                    dir = manualPivotDirection.normalized;
                    break;
            }

            return relativeTo.TransformDirection(dir);
        }
    }
}