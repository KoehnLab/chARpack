using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

namespace chARpack
{
    /// <summary>
    /// This class updates the text of the slider label when the slider is moved.
    /// </summary>
    public class myShowSliderValue : MonoBehaviour
    {
        [SerializeField]
        private TextMeshPro textMesh = null;

        public void OnSliderUpdated(mySliderEventData eventData)
        {
            if (textMesh == null)
            {
                textMesh = GetComponent<TextMeshPro>();
            }

            if (textMesh != null)
            {
                textMesh.text = $"{eventData.NewValue:F2}";
            }
        }
    }
}