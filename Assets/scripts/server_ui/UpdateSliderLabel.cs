using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateSliderLabel : MonoBehaviour
{
    public GameObject label;

    public void updateLabel()
    {
        label.GetComponent<Text>().text = $"{GetComponent<Slider>().value}";
    }

    public void updateLabelTwoDigitPrecision()
    {
        label.GetComponent<Text>().text = $"{GetComponent<Slider>().value:0.00}";
    }
}
