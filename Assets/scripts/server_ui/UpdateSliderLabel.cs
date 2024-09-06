using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdateSliderLabel : MonoBehaviour
{
    public GameObject label;

    public void updateLabel()
    {
        //label.GetComponent<Text>().text = $"{GetComponent<Slider>().value}";
        //label.GetComponent<Text>().text = $"{GetComponent<Slider>().value:0.00}";
        if (label.GetComponent<Text>() != null)
        {
            if (GetComponent<Slider>().wholeNumbers)
            {
                label.GetComponent<Text>().text = $"{GetComponent<Slider>().value}";
            }
            else
            {
                //int count = BitConverter.GetBytes(decimal.GetBits((decimal)StepSize)[3])[2] - 1;
                int prec = StepSize == 0 ? 0 : (int)Mathf.Floor(Mathf.Log10(Mathf.Abs(StepSize)));
                prec = Mathf.Abs(prec);
                label.GetComponent<Text>().text = string.Format($"{{0:F{prec}}}", GetComponent<Slider>().value);
            }
        }
        else if (label.GetComponent<TMP_Text>() != null)
        {
            if (GetComponent<Slider>().wholeNumbers)
            {
                label.GetComponent<TMP_Text>().text = $"{GetComponent<Slider>().value}";
            }
            else
            {
                //int count = BitConverter.GetBytes(decimal.GetBits((decimal)StepSize)[3])[2] - 1;
                int prec = StepSize == 0 ? 0 : (int)Mathf.Floor(Mathf.Log10(Mathf.Abs(StepSize)));
                prec = Mathf.Abs(prec);
                label.GetComponent<TMP_Text>().text = string.Format($"{{0:F{prec}}}", GetComponent<Slider>().value);
            }
        }
    }

    public float StepSize = 1f;

    void Start()
    {
        GetComponent<Slider>().onValueChanged.AddListener((e) =>
        {
            var ddd = Mathf.Round(GetComponent<Slider>().value / StepSize) * StepSize;
            GetComponent<Slider>().value = ddd;
            updateLabel();
        });
    }
}
