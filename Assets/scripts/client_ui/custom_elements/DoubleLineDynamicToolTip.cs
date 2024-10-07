﻿using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace chARpack
{
    /// <summary>
    /// Modified MRTK script
    /// Class for Tooltip object
    /// Creates a floating tooltip that is attached to an object and moves to stay in view as object rotates with respect to the view.
    /// </summary>
    public class DoubleLineDynamicToolTip : MonoBehaviour
    {

        // store childs of content
        List<GameObject> contentList = new List<GameObject>();

        [SerializeField]
        [Tooltip("Show the opaque background of tooltip.")]
        private bool showBackground = true;

        /// <summary>
        /// Show the opaque background of tooltip.
        /// </summary>
        public bool ShowBackground
        {
            get { return showBackground; }
            set { showBackground = value; }
        }

        [SerializeField]
        private bool showHighlight = false;

        /// <summary>
        /// Shows white trim around edge of tooltip.
        /// </summary>
        public bool ShowHighlight
        {
            get
            {
                return showHighlight;
            }
            set
            {
                showHighlight = value;
            }
        }

        [SerializeField]
        [Tooltip("Show the connecting stem between the tooltip and its parent GameObject.")]
        private bool showConnector = true;

        /// <summary>
        /// Show the connecting stem between the tooltip and its parent GameObject.
        /// </summary>
        public bool ShowConnector
        {
            get { return showConnector; }
            set { showConnector = value; }
        }

        [SerializeField]
        [Tooltip("Display the state of the tooltip.")]
        private DisplayMode tipState = DisplayMode.On;

        /// <summary>
        /// The display the state of the tooltip.
        /// </summary>
        public DisplayMode TipState
        {
            get { return tipState; }
            set { tipState = value; }
        }

        [SerializeField]
        [Tooltip("Display the state of a group of tooltips.")]
        private DisplayMode groupTipState;

        /// <summary>
        /// Display the state of a group of tooltips.
        /// </summary>
        public DisplayMode GroupTipState
        {
            set { groupTipState = value; }
            get { return groupTipState; }
        }

        [SerializeField]
        [Tooltip("Display the state of the master tooltip.")]
        private DisplayMode masterTipState;

        /// <summary>
        /// Display the state of the master tooltip.
        /// </summary>
        public DisplayMode MasterTipState
        {
            set { masterTipState = value; }
            get { return masterTipState; }
        }

        [SerializeField]
        [Tooltip("GameObject that the line and text are attached to")]
        private GameObject anchor1;
        /// <summary>
        /// getter/setter for ameObject that the line and text are attached to
        /// </summary>
        public GameObject Anchor1
        {
            get { return anchor1; }
            set { anchor1 = value; }
        }

        [SerializeField]
        [Tooltip("GameObject that the line and text are attached to")]
        private GameObject anchor2;
        /// <summary>
        /// getter/setter for ameObject that the line and text are attached to
        /// </summary>
        public GameObject Anchor2
        {
            get { return anchor2; }
            set { anchor2 = value; }
        }


        [SerializeField]
        [Tooltip("GameObject for the Background")]
        private GameObject bg;
        /// <summary>
        /// getter/setter for GameObject of Background
        /// </summary>
        public GameObject BG
        {
            get { return bg; }
            set { bg = value; }
        }

        [Tooltip("Pivot point that text will rotate around as well as the point where the Line will be rendered to.")]
        [SerializeField]
        private GameObject pivot;

        /// <summary>
        /// Pivot point that text will rotate around as well as the point where the Line will be rendered to. 
        /// </summary>
        public GameObject Pivot => pivot;

        [SerializeField]
        [Tooltip("GameObject text that is displayed on the tooltip.")]
        private GameObject label;

        [SerializeField]
        [Tooltip("GameObject text that aligns the content.")]
        private GameObject content;

        [SerializeField]
        [Tooltip("Parent of the Content and Background")]
        private GameObject contentParent;

        [TextArea]
        [SerializeField]
        [Tooltip("Text for the ToolTip to display")]
        private string toolTipText;

        /// <summary>
        /// Text for the ToolTip to display
        /// </summary>
        public string ToolTipText
        {
            set
            {
                toolTipText = value;
                if (!Application.isPlaying)
                {   // Only force refresh in edit mode
                    RefreshLocalContent();
                }
            }
            get { return toolTipText; }
        }

        [SerializeField]
        [Tooltip("The padding around the content (height / width)")]
        private Vector2 backgroundPadding = Vector2.zero;

        [SerializeField]
        [Tooltip("The offset of the background (x / y / z)")]
        private Vector3 backgroundOffset = Vector3.zero;

        /// <summary>
        /// The offset of the background (x / y / z)
        /// </summary>
        public Vector3 LocalContentOffset => backgroundOffset;

        [SerializeField]
        [Range(0.01f, 3f)]
        [Tooltip("The scale of all the content (label, backgrounds, etc.)")]
        private float contentScale = 1f;

        /// <summary>
        /// The scale of all the content (label, backgrounds, etc.)
        /// </summary>
        public float ContentScale
        {
            get { return contentScale; }
            set
            {
                contentScale = value;
                if (!Application.isPlaying)
                {   // Only force refresh in edit mode
                    RefreshLocalContent();
                }
            }
        }

        [SerializeField]
        [Range(10, 60)]
        [Tooltip("The font size of the tooltip.")]
        private int fontSize = 30;

        /// <summary>
        /// The font size of the tooltip.
        /// </summary>
        public int FontSize
        {
            get { return fontSize; }
            set
            {
                fontSize = value;
                if (!Application.isPlaying)
                {   // Only force refresh in edit mode
                    RefreshLocalContent();
                }
            }
        }

        [SerializeField]
        [Tooltip("Determines where the line will attach to the tooltip content.")]
        private ToolTipAttachPoint attachPointType = ToolTipAttachPoint.Closest;

        public ToolTipAttachPoint PivotType
        {
            get
            {
                return attachPointType;
            }
            set
            {
                attachPointType = value;
            }
        }

        /// <summary>
        /// point where ToolTip is attached
        /// </summary>
        public Vector3 AttachPoint1Position
        {
            get { return attachPoint1Position; }
            set
            {
                // apply the difference to the offset
                attachPoint1Offset = value - contentParent.transform.TransformPoint(localAttachPoint1);
            }
        }

        /// <summary>
        /// point where ToolTip is attached
        /// </summary>
        public Vector3 AttachPoint2Position
        {
            get { return attachPoint2Position; }
            set
            {
                // apply the difference to the offset
                attachPoint2Offset = value - contentParent.transform.TransformPoint(localAttachPoint2);
            }
        }

        [SerializeField]
        [Tooltip("Added as an offset to the pivot position. Modifying AttachPointPosition directly changes this value.")]
        private Vector3 attachPoint1Offset;

        [SerializeField]
        [Tooltip("Added as an offset to the pivot position. Modifying AttachPointPosition directly changes this value.")]
        private Vector3 attachPoint2Offset;

        [SerializeField]
        [Tooltip("The line connecting the anchor to the pivot. If present, this component will be updated automatically.\n\nRecommended: SimpleLine, Spline, and ParabolaConstrainted")]
        private BaseMixedRealityLineDataProvider toolTipLine1;
        public GameObject Line1GO;

        [SerializeField]
        [Tooltip("The line connecting the anchor to the pivot. If present, this component will be updated automatically.\n\nRecommended: SimpleLine, Spline, and ParabolaConstrainted")]
        private BaseMixedRealityLineDataProvider toolTipLine2;
        public GameObject Line2GO;

        private Vector3 localContentSize;

        /// <summary>
        /// getter/setter for size of tooltip.
        /// </summary>
        public Vector3 LocalContentSize => localContentSize;

        private Vector3 pivotPosition;
        private Vector3 attachPoint1Position;
        private Vector3 attachPoint2Position;
        private Vector3 anchor1Position;
        private Vector3 anchor2Position;
        private Vector3 localAttachPoint1;
        private Vector3 localAttachPoint2;
        private Vector3[] localAttachPointPositions;
        private List<IToolTipBackground> backgrounds = new List<IToolTipBackground>();
        private List<IToolTipHighlight> highlights = new List<IToolTipHighlight>();
        private TextMeshPro cachedLabelText;

        /// <summary>
        /// point about which ToolTip pivots to face camera
        /// </summary>
        public Vector3 PivotPosition
        {
            get { return pivotPosition; }
            set
            {
                pivotPosition = value;
                pivot.transform.position = value;
            }
        }

        /// <summary>
        /// point where ToolTip connector is attached
        /// </summary>
        public Vector3 Anchor1Position
        {
            get { return anchor1Position; }
            set { anchor1.transform.position = value; }
        }

        /// <summary>
        /// point where ToolTip connector is attached
        /// </summary>
        public Vector3 Anchor2Position
        {
            get { return anchor2Position; }
            set { anchor2.transform.position = value; }
        }

        /// <summary>
        /// Transform of object to which ToolTip is attached
        /// </summary>
        public Transform ContentParentTransform => contentParent.transform;

        /// <summary>
        /// is ToolTip active and displaying
        /// </summary>
        public bool IsOn
        {
            get
            {
                return ResolveTipState(masterTipState, groupTipState, tipState, HasFocus);
            }
        }

        public static bool ResolveTipState(DisplayMode masterTipState, DisplayMode groupTipState, DisplayMode tipState, bool hasFocus)
        {
            switch (masterTipState)
            {
                case DisplayMode.None:
                default:
                    // Use our group state
                    switch (groupTipState)
                    {
                        case DisplayMode.None:
                        default:
                            // Use our local State
                            switch (tipState)
                            {
                                case DisplayMode.None:
                                case DisplayMode.Off:
                                default:
                                    return false;

                                case DisplayMode.On:
                                    return true;

                                case DisplayMode.OnFocus:
                                    return hasFocus;
                            }

                        case DisplayMode.On:
                            return true;

                        case DisplayMode.Off:
                            return false;

                        case DisplayMode.OnFocus:
                            return hasFocus;
                    }

                case DisplayMode.On:
                    return true;

                case DisplayMode.Off:
                    return false;

                case DisplayMode.OnFocus:
                    return hasFocus;
            }
        }

        /// <summary>
        /// does the ToolTip have focus.
        /// </summary>
        public virtual bool HasFocus
        {
            get
            {
                return false;
            }
        }

        public void addContent(GameObject stuff)
        {
            stuff.transform.parent = content.transform;
            stuff.transform.localScale *= 3;
            content.GetComponent<GridObjectCollection>().UpdateCollection();
            //RefreshLocalContent();
        }

        /// <summary>
        /// virtual functions
        /// </summary>
        protected virtual void OnEnable()
        {
            //ValidateHeirarchy();

            label.EnsureComponent<TextMeshPro>();
            gameObject.EnsureComponent<myToolTipConnector>();

            // Get our line if it exists
            if (toolTipLine1 == null && toolTipLine2 == null)
            {
                Debug.LogError("[DoubleLineDynamicToolTip] Lines not found.");
                return;
            }

            float line_width = 0.1f;
            AnimationCurve line_with_curve = new AnimationCurve(new Keyframe(-1.0f, line_width), new Keyframe(1.0f, line_width));
            Line1GO.GetComponent<MixedRealityLineRenderer>().LineWidth = line_with_curve;
            Line2GO.GetComponent<MixedRealityLineRenderer>().LineWidth = line_with_curve;

            // Make sure the tool tip text isn't empty
            if (string.IsNullOrEmpty(toolTipText))
                toolTipText = " ";

            backgrounds.Clear();
            foreach (IToolTipBackground background in GetComponents<IToolTipBackground>())
            {
                backgrounds.Add(background);
            }

            highlights.Clear();
            foreach (IToolTipHighlight highlight in GetComponents<IToolTipHighlight>())
            {
                highlights.Add(highlight);
            }

            contentParent.SetActive(false);
            ShowBackground = showBackground;
            ShowHighlight = showHighlight;
            ShowConnector = showConnector;
        }

        protected virtual void Update()
        {
            // Cache our pivot / anchor / attach point positions
            pivotPosition = pivot.transform.position;
            anchor1Position = anchor1.transform.position;
            anchor2Position = anchor2.transform.position;
            attachPoint1Position = contentParent.transform.TransformPoint(localAttachPoint1) + attachPoint1Offset;
            attachPoint2Position = contentParent.transform.TransformPoint(localAttachPoint2) + attachPoint2Offset;

            // Enable / disable our line if it exists
            if (toolTipLine1 != null)
            {
                toolTipLine1.enabled = showConnector;

                if (!(toolTipLine1 is ParabolaConstrainedLineDataProvider))
                {
                    toolTipLine1.FirstPoint = Anchor1Position;
                }

                toolTipLine1.LastPoint = AttachPoint1Position;
            }
            if (toolTipLine2 != null)
            {
                toolTipLine2.enabled = showConnector;

                if (!(toolTipLine2 is ParabolaConstrainedLineDataProvider))
                {
                    toolTipLine2.FirstPoint = Anchor2Position;
                }

                toolTipLine2.LastPoint = AttachPoint2Position;
            }

            if (IsOn)
            {
                contentParent.SetActive(true);
                localAttachPoint1 = ToolTipUtility.FindClosestAttachPointToAnchor(anchor1.transform, contentParent.transform, localAttachPointPositions, PivotType);
                localAttachPoint2 = ToolTipUtility.FindClosestAttachPointToAnchor(anchor2.transform, contentParent.transform, localAttachPointPositions, PivotType);
            }
            else
            {
                contentParent.SetActive(false);
            }

            RefreshLocalContent();
        }

        protected virtual void RefreshLocalContent()
        {
            // Set the scale of the pivot
            contentParent.transform.localScale = Vector3.one * contentScale;
            //label.transform.localScale = Vector3.one * 0.005f;
            content.transform.localScale = Vector3.one * 0.005f;
            // Set the content using a text mesh by default
            // This function can be overridden for tooltips that use Unity UI

            List<GameObject> currentContentList = new List<GameObject>();
            foreach(Transform child in content.transform)
            {
                currentContentList.Add(child.gameObject);
            }

            // If it has, update the content
            if (currentContentList != contentList) {
                contentList = currentContentList;

           

                if (cachedLabelText == null)
                    cachedLabelText = label.GetComponent<TextMeshPro>();

                if (cachedLabelText != null && !string.IsNullOrEmpty(toolTipText))
                {
                    cachedLabelText.fontSize = fontSize;
                    cachedLabelText.text = toolTipText.Trim();
                    // Update text so we get an accurate scale
                    cachedLabelText.ForceMeshUpdate();
                    // Get the world scale of the text
                    // Convert that to local scale using the content parent
                }
                
                // get the text scale
                Vector3 localScale = Vector3.Scale(cachedLabelText.transform.lossyScale / contentScale, cachedLabelText.textBounds.size);
                localContentSize.x = localScale.x;
                localContentSize.y = localScale.y;
                // widen the text depending of the number of buttons attached
                localContentSize.x += 0.020f * (contentList.Count - 1);
                // add height for more than 3 items
                int div = (int)(contentList.Count / 4.0f);
                localContentSize.y += div * 0.062f;

                // add padding
                localContentSize.x += backgroundPadding.x;
                localContentSize.y += backgroundPadding.y;

                // Now that we have the size of our content, get our pivots
                ToolTipUtility.GetAttachPointPositions(ref localAttachPointPositions, localContentSize);
                localAttachPoint1 = ToolTipUtility.FindClosestAttachPointToAnchor(anchor1.transform, contentParent.transform, localAttachPointPositions, PivotType);
                localAttachPoint2 = ToolTipUtility.FindClosestAttachPointToAnchor(anchor2.transform, contentParent.transform, localAttachPointPositions, PivotType);

                foreach (IToolTipBackground background in backgrounds)
                {
                    background.OnContentChange(localContentSize, LocalContentOffset, contentParent.transform);
                }
            }

            foreach (IToolTipBackground background in backgrounds)
            {
                background.IsVisible = showBackground;
            }

            foreach (IToolTipHighlight highlight in highlights)
            {
                highlight.ShowHighlight = ShowHighlight;
            }
        }

        public static Vector3 GetTextMeshLocalScale(TextMesh textMesh)
        {
            Vector3 localScale = Vector3.zero;

            if (string.IsNullOrEmpty(textMesh.text))
                return localScale;

            string[] splitStrings = textMesh.text.Split(new string[] { System.Environment.NewLine, "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            // Calculate the width of the text using character info
            float widestLine = 0f;
            foreach (string splitString in splitStrings)
            {
                float lineWidth = 0f;
                foreach (char symbol in splitString)
                {
                    CharacterInfo info;
                    if (textMesh.font.GetCharacterInfo(symbol, out info, textMesh.fontSize, textMesh.fontStyle))
                    {
                        lineWidth += info.advance;
                    }
                }
                if (lineWidth > widestLine)
                    widestLine = lineWidth;
            }
            localScale.x = widestLine;

            // Use this to multiply the character size
            Vector3 transformScale = textMesh.transform.localScale;
            localScale.x = (localScale.x * textMesh.characterSize * 0.1f) * transformScale.x;
            localScale.z = transformScale.z;

            // We could calculate the height based on line height and character size
            // But I've found that method can be flaky and has a lot of magic numbers
            // that may break in future Unity versions
            Vector3 eulerAngles = textMesh.transform.eulerAngles;
            Vector3 rendererScale = Vector3.zero;
            textMesh.transform.rotation = Quaternion.identity;
            rendererScale = textMesh.GetComponent<MeshRenderer>().bounds.size;
            textMesh.transform.eulerAngles = eulerAngles;
            localScale.y = textMesh.transform.worldToLocalMatrix.MultiplyVector(rendererScale).y * transformScale.y;

            return localScale;
        }
    }
}
