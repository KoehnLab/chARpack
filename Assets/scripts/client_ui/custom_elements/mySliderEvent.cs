using UnityEngine.Events;

namespace chARpack
{
    /// <summary>
    /// A UnityEvent callback containing a SliderEventData payload.
    /// Convenience class to differentiate between MRTK slider and custom slider.
    /// </summary>
    [System.Serializable]
    public class mySliderEvent : UnityEvent<mySliderEventData> { }

}
