using Microsoft.MixedReality.Toolkit.UI;
using StructClass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleMoleculeAction : IUndoableAction
{
    public cmlData before;
    public cmlData after { get; private set; }

    public ScaleMoleculeAction(cmlData before, cmlData after)
    {
        this.before = before;
        this.after = after;
    }

    public ScaleMoleculeAction(ScaleMoleculeAction action)
    {
        before = action.before;
        after = action.after;
    }
    public void Execute()
    {
        // Only needed if we decide to implement redo
        throw new System.NotImplementedException();
    }

    public void Undo()
    {
        bool hasSlider = false;
        var ratio = 1.0f;
        if (GlobalCtrl.Singleton.Dict_curMolecules[after.moleID].scalingSliderInstance)
        {
            ratio = after.moleScale.x / GlobalCtrl.Singleton.Dict_curMolecules[after.moleID].scalingSliderInstance.GetComponent<mySlider>().SliderValue;
            hasSlider = true;
        }
        before.molePos = GlobalCtrl.Singleton.Dict_curMolecules[after.moleID].transform.localPosition;
        GlobalCtrl.Singleton.deleteMolecule(after.moleID, false);
        GlobalCtrl.Singleton.BuildMoleculeFromCML(before, before.moleID);
        if (hasSlider)
        {
            GlobalCtrl.Singleton.Dict_curMolecules[before.moleID].toggleScalingSlider();
            GlobalCtrl.Singleton.Dict_curMolecules[before.moleID].scalingSliderInstance.GetComponent<mySlider>().StartSliderValue = before.moleScale.x / ratio;
        }
        GlobalCtrl.Singleton.SignalUndoScaling();
    }
}