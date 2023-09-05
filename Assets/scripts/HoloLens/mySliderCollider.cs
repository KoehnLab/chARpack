using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    public class mySliderCollider : MonoBehaviour
    {
        private Vector3 startingPosition = Vector3.zero;
        public mySlider slider;

        public void OnMouseDown()
        {
            startingPosition = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
                new Vector3(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y, transform.position.z));
        }

        public void OnMouseDrag()
        {

            Vector3 newPosition = new Vector3(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y, transform.position.z);
            var delta = newPosition - startingPosition;
            var mouseDelta = Vector3.Dot(slider.SliderTrackDirection.normalized, delta);

            //if (useSliderStepDivisions)
            //{
            //    var stepVal = (mouseDelta / SliderTrackDirection.magnitude > 0) ? sliderStepVal : (sliderStepVal * -1);
            //    var stepMag = Mathf.Floor(Mathf.Abs(mouseDelta / SliderTrackDirection.magnitude) / sliderStepVal);
            //    SliderValue = Mathf.Clamp(StartSliderValue + (stepVal * stepMag), 0, 1);
            //}
            slider.SliderValue = Mathf.Clamp(slider.StartSliderValue + mouseDelta / slider.SliderTrackDirection.magnitude, 0, 1);
        }
    }
}
