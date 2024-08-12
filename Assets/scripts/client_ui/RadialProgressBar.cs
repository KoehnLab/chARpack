using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialProgressBar : MonoBehaviour
{

    public Image image;

    public void setProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        image.fillAmount = progress;
    }
}
