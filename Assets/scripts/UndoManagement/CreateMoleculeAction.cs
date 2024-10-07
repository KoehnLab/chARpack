using chARpack.Structs;
using System;

namespace chARpack
{
    public class CreateMoleculeAction : IUndoableAction
    {
        private cmlData cmlData;
        private Guid m_id;

        public CreateMoleculeAction(Guid m_id, cmlData cmlData)
        {
            this.m_id = m_id;
            this.cmlData = cmlData;
        }
        public void Execute()
        {
            m_id = GlobalCtrl.Singleton.BuildMoleculeFromCML(cmlData);
        }

        public void Undo()
        {
            GlobalCtrl.Singleton.deleteMolecule(m_id, false);
        }
    }
}