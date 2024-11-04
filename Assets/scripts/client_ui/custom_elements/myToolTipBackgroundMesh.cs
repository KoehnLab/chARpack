using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace chARpack
{
    /// <summary>
    /// Modified MRTK script
    /// Renders a background mesh for a tool tip using a mesh renderer
    /// If the mesh has an offset anchor point you will get odd results
    /// </summary>
    [AddComponentMenu("Scripts/MRTK/SDK/ToolTipBackgroundMesh")]
    public class myToolTipBackgroundMesh : MonoBehaviour, IToolTipBackground
    {
        [SerializeField]
        [Tooltip("Transform that scale and offset will be applied to.")]
        private Transform backgroundTransform;

        /// <summary>
        /// Mesh renderer button for mesh background.
        /// </summary>
        public MeshRenderer BackgroundRenderer;

        /// <summary>
        /// Determines whether background of Tooltip is visible.
        /// </summary>
        public bool IsVisible
        {
            set
            {
                if (BackgroundRenderer == null)
                    return;

                BackgroundRenderer.enabled = value;
            }
        }

        /// <summary>
        /// The Transform for the background of the Tooltip.
        /// </summary>
        public Transform BackgroundTransform
        {
            get
            {
                return backgroundTransform;
            }

            set
            {
                backgroundTransform = value;
            }
        }

        public void OnContentChange(Vector3 localContentSize, Vector3 localContentOffset, Transform contentParentTransform)
        {
            if (BackgroundRenderer == null)
                return;

            // Get the size of the mesh and use this to adjust the local content size on the x / y axis
            // This will accommodate meshes that aren't built to 1,1 scale
            Bounds meshBounds = BackgroundRenderer.GetComponent<MeshFilter>().sharedMesh.bounds;
            localContentSize.x /= meshBounds.size.x;
            localContentSize.y /= meshBounds.size.y;
            localContentSize.z = BackgroundTransform.localScale.z;

            BackgroundTransform.localScale = localContentSize;
        }
    }
}

