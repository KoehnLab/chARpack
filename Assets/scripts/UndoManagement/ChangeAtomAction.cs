using chARpack.Structs;

namespace chARpack
{
    public class ChangeAtomAction : IUndoableAction
    {
        private cmlData before;
        private cmlData after;

        public ChangeAtomAction(cmlData before, cmlData after)
        {
            this.before = before;
            this.after = after;
        }

        public void Execute()
        {
            // Only needed if we decide to implement redo
            throw new System.NotImplementedException();
        }

        public void Undo()
        {
            GlobalCtrl.Singleton.deleteMolecule(after.moleID, false);
            GlobalCtrl.Singleton.BuildMoleculeFromCML(before, before.moleID);
        }
    }
}