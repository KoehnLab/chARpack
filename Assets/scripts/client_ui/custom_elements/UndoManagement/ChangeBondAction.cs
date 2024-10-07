using chARpack.Structs;

namespace chARpack
{
    public class ChangeBondAction : IUndoableAction
    {
        public enum BondType
        {
            SINGLE,
            ANGLE,
            TORSION
        }

        private cmlData before;
        private cmlData after;

        public ChangeBondAction(cmlData before, cmlData after)
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
            //switch (type) {
            //    case BondType.SINGLE:
            //        for(var i=0; i<after.bondArray.Length; i++)
            //        {
            //            GlobalCtrl.Singleton.Dict_curMolecules[after.moleID].bondTerms[i] = bondTermFromCML(before.bondArray[i]);
            //        }
            //        break;
            //    case BondType.ANGLE:
            //        for (var i = 0; i < after.angleArray.Length; i++)
            //        {
            //            GlobalCtrl.Singleton.Dict_curMolecules[after.moleID].angleTerms[i] = angleTermFromCML(before.angleArray[i]);
            //        }
            //        break;
            //    case BondType.TORSION:
            //        for (var i = 0; i < after.torsionArray.Length; i++)
            //        {
            //            GlobalCtrl.Singleton.Dict_curMolecules[after.moleID].torsionTerms[i] = torsionTermFromCML(before.torsionArray[i]);
            //        }
            //        break;
            //}
            GlobalCtrl.Singleton.deleteMolecule(after.moleID, false);
            GlobalCtrl.Singleton.BuildMoleculeFromCML(before, before.moleID);
        }

        private ForceField.BondTerm bondTermFromCML(cmlBond cml)
        {
            var term = new ForceField.BondTerm();
            term.Atom1 = cml.id1; term.Atom2 = cml.id2; term.eqDist = cml.eqDist; term.kBond = cml.kb; term.order = cml.order;
            return term;
        }

        private ForceField.AngleTerm angleTermFromCML(cmlAngle cml)
        {
            var term = new ForceField.AngleTerm();
            term.Atom1 = cml.id1; term.Atom2 = cml.id2; term.Atom3 = cml.id3; term.eqAngle = cml.angle; term.kAngle = cml.ka;
            return term;
        }

        private ForceField.TorsionTerm torsionTermFromCML(cmlTorsion cml)
        {
            var term = new ForceField.TorsionTerm();
            term.Atom1 = cml.id1; term.Atom2 = cml.id2; term.Atom3 = cml.id3; term.Atom4 = cml.id4; term.eqAngle = cml.angle; term.vk = cml.k0; term.nn = cml.nn;
            return term;
        }
    }
}
