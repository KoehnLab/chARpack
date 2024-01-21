using Microsoft.MixedReality.Toolkit.UI;
using StructClass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleMoleculeAction : IUndoableAction
{
    public ushort molID { get; private set; }
    public float prevSliderValue;

    public ScaleMoleculeAction(ushort molID, float prevSliderValue)
    {
        this.molID = molID;
        this.prevSliderValue = (prevSliderValue - GlobalCtrl.Singleton.Dict_curMolecules[molID].scalingSliderInstance.GetComponent<mySlider>().minVal) /
             (GlobalCtrl.Singleton.Dict_curMolecules[molID].scalingSliderInstance.GetComponent<mySlider>().maxVal - GlobalCtrl.Singleton.Dict_curMolecules[molID].scalingSliderInstance.GetComponent<mySlider>().minVal);
    }

    public ScaleMoleculeAction(ScaleMoleculeAction action)
    {
        molID = action.molID;
        prevSliderValue = action.prevSliderValue;
    }
    public void Execute()
    {
        // Only needed if we decide to implement redo
        throw new System.NotImplementedException();
    }

    public void Undo()
    {
        GlobalCtrl.Singleton.Dict_curMolecules[molID].scalingSliderInstance.GetComponent<mySlider>().SliderValue = prevSliderValue;
        GlobalCtrl.Singleton.SignalUndoScaling();
    }
}