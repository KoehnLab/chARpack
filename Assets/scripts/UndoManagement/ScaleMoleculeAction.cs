using chARpack.Structs;

namespace chARpack
{
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
            if (GlobalCtrl.Singleton.List_curMolecules[after.moleID].scalingSliderInstance)
            {
                ratio = after.moleScale.x / GlobalCtrl.Singleton.List_curMolecules[after.moleID].scalingSliderInstance.GetComponent<mySlider>().SliderValue;
                hasSlider = true;
            }
            GlobalCtrl.Singleton.deleteMolecule(after.moleID, false);
            GlobalCtrl.Singleton.BuildMoleculeFromCML(before, before.moleID);
            if (hasSlider)
            {
                GlobalCtrl.Singleton.List_curMolecules[before.moleID].toggleScalingSlider();
                GlobalCtrl.Singleton.List_curMolecules[before.moleID].scalingSliderInstance.GetComponent<mySlider>().StartSliderValue = before.moleScale.x / ratio;
            }
            GlobalCtrl.Singleton.SignalUndoScaling();
        }
    }
}
