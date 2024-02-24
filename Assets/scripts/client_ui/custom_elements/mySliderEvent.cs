// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// A UnityEvent callback containing a SliderEventData payload.
    /// Convenience class to differentiate between MRTK slider and custom slider.
    /// </summary>
    [System.Serializable]
    public class mySliderEvent : UnityEvent<mySliderEventData> { }

}
