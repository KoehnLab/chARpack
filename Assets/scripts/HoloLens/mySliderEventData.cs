//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
using Microsoft.MixedReality.Toolkit.Input;

namespace Microsoft.MixedReality.Toolkit.UI
{
    public class mySliderEventData
    {
        public mySliderEventData(float o, float n, IMixedRealityPointer pointer, mySlider slider)
        {
            OldValue = o*(slider.maxVal - slider.minVal);
            NewValue = n*(slider.maxVal - slider.minVal);
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
