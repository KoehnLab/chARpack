using chARpack.Structs;
using System.Collections.Generic;

namespace chARpack
{
    public class DeleteAllAction : IUndoableAction
    {
        private List<cmlData> atomWorld;

        public DeleteAllAction(List<cmlData> atomWorld)
        {
            this.atomWorld = atomWorld;
        }
        public void Execute()
        {
            GlobalCtrl.Singleton.DeleteAll();
        }

        public void Undo()
        {
            // Preserve previous IDs
            GlobalCtrl.Singleton.createFromCML(atomWorld, false);
        }
    }
}
