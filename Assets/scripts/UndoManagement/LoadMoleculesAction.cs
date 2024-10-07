using chARpack.Structs;
using System.Collections.Generic;

namespace chARpack
{
    public class LoadMoleculeAction : IUndoableAction
    {
        private List<cmlData> loadData;

        public LoadMoleculeAction(List<cmlData> cmlData)
        {
            this.loadData = cmlData;
        }
        public void Execute()
        {

        }

        public void Undo()
        {
            foreach (var mol in loadData)
            {
                GlobalCtrl.Singleton.deleteMolecule(mol.moleID, false);
            }
        }
    }
}
