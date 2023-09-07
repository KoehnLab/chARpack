using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    public class mySliderCollider : MonoBehaviour
    {
        private Vector3 startingPosition = Vector3.zero;
        private float distToCamera = 0;
        public mySlider slider;

        public void OnMouseDown()
        {
            distToCamera = Vector3.Dot(GlobalCtrl.Singleton.currentCamera.transform.forward, transform.position - GlobalCtrl.Singleton.currentCamera.transform.position);
            startingPosition = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
                new Vector3(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y, distToCamera));
            slider.StartSliderValue = slider.SliderValue;
        }

        public void OnMouseDrag()
        {
            Vector3 newPosition = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
                new Vector3(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y, distToCamera));
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
