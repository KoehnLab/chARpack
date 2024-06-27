using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServerScalingSlider : ServerTooltip
{
    [HideInInspector] public Molecule linkedMolecule;


    public override void Start()

    {
        base.Start();
        closeButton.onClick.AddListener(delegate { Destroy(gameObject); });
    }

    public void resetScale()
    {
        GetComponentInChildren<Slider>().value = 1.0f;
    }
}
