using UnityEngine;

namespace chARpack
{
    /// <summary>
    /// This script provides mouse interactability for a slider thumb.
    /// </summary>
    public class mySliderCollider : MonoBehaviour
    {
        private Vector3 startingPosition = Vector3.zero;
        private float distToCamera = 0;
        public mySlider slider;

        public void OnMouseDown()
        {
            Camera currentCam;
            if (GlobalCtrl.Singleton != null)
            {
                currentCam = GlobalCtrl.Singleton.currentCamera;
            }
            else
            {
                currentCam = Camera.main;
            }
            distToCamera = Vector3.Dot(currentCam.transform.forward, transform.position - currentCam.transform.position);
            startingPosition = currentCam.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, distToCamera));
            slider.StartSliderValue = slider.SliderValue;
        }

        public void OnMouseDrag()
        {
            Camera currentCam;
            if (GlobalCtrl.Singleton != null)
            {
                currentCam = GlobalCtrl.Singleton.currentCamera;
            }
            else
            {
                currentCam = Camera.main;
            }
            Vector3 newPosition = currentCam.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, distToCamera));
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
