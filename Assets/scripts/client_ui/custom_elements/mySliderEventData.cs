using Microsoft.MixedReality.Toolkit.Input;

namespace chARpack
{
    /// <summary>
    /// A custom slider event data class that provides the ability to 
    /// use values outside of the [0,1] range used by the default MRTK slider.
    /// </summary>
    public class mySliderEventData
    {
        public mySliderEventData(float o, float n, IMixedRealityPointer pointer, mySlider slider)
        {
            OldValue = o*(slider.maxVal - slider.minVal)+slider.minVal;
            NewValue = n*(slider.maxVal - slider.minVal)+slider.minVal;
            Pointer = pointer;
            Slider = slider;
        }

        /// <summary>
        /// The previous value of the slider
        /// </summary>
        public float OldValue { get; private set; }

        /// <summary>
        /// The current value of the slider
        /// </summary>
        public float NewValue { get; private set; }

        /// <summary>
        /// The slider that triggered this event
        /// </summary>
        public mySlider Slider { get; private set; }

        /// <summary>
        /// The currently active pointer manipulating / hovering the slider,
        /// or null if no pointer is manipulating the slider.
        /// Note: OnSliderUpdated is called with .Pointer == null
        /// OnStart, so always check if this field is null before using!
        /// </summary>
        public IMixedRealityPointer Pointer { get; set; }
    }
}
