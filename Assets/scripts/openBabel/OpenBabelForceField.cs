using OpenBabel;

namespace chARpack
{
    public static class OpenBabelForceField
    {
        /// <summary>
        /// Minimise a structure using OpenBabel for a given number of steps
        /// </summary>
        public static OBMol MinimiseStructure(OBMol mol,
                                              int steps,
                                              string forcefieldName = "mmff94s")
        {
            var forcefield = GetForceField(forcefieldName, mol);

            forcefield.SteepestDescent(steps);

            if (!forcefield.GetCoordinates(mol))
                OpenBabelSetup.ThrowOpenBabelException("Failed to get conformer");

            return mol;
        }

        /// <summary>
        /// Attempt to find and setup the given forcefield for a given molecule.
        /// </summary>
        public static OBForceField GetForceField(string name, OBMol mol)
        {
            var forcefield = OBForceField.FindForceField(name);
            if (forcefield == null)
                OpenBabelSetup.ThrowOpenBabelException("Failed to load force field");
            if (!forcefield.Setup(mol))
                OpenBabelSetup.ThrowOpenBabelException("Failed to setup force field");
            return forcefield;
        }
    }
}
