using chARpack.Structs;
using System.Collections.Generic;

namespace chARpack
{
    public class DeleteAtomAction : IUndoableAction
    {
        private cmlData before;
        private List<cmlData> after;

        public DeleteAtomAction(cmlData before, List<cmlData> after)
        {
            this.before = before;
            this.after = after;
        }
        public void Execute()
        {
            // Only needed if we decide to implement a redo system
            throw new System.NotImplementedException();
        }

        public void Undo()
        {
            // Set position to that of one of the resulting molecules since
            // they might have been moved anywhere separately
            foreach (cmlData molecule in after)
            {
                GlobalCtrl.Singleton.deleteMolecule(molecule.moleID, false);
            }
            GlobalCtrl.Singleton.BuildMoleculeFromCML(before, before.moleID);
        }
    }
}
