using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace chARpack
{
    /// <summary>
    /// This script is used to make MRTK buttons clickable with the left mouse button.
    /// </summary>
    public class buttonMouseClick : MonoBehaviour
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        private void OnMouseDown()
        {
            if (EventSystem.current.IsPointerOverGameObject()) { return; }

            Scene activeScene = SceneManager.GetActiveScene();

            // Avoid double pressing with mouse and finger click; special case for canvas buttons that don't have buttonConfigHelper and OnClick
            if (GameObject.FindObjectOfType(typeof(Microsoft.MixedReality.Toolkit.Input.RiggedHandVisualizer)) != null || gameObject.GetComponent<CanvasRenderer>() || activeScene.name == "ServerScene")
            {
                GetComponent<PressableButtonHoloLens2>().ButtonPressed.Invoke();
            }
        }
#endif
    }
}
