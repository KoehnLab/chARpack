using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenBabel;
using chARpackStructs;
using System.Linq;

public static class OpenBabelExtensions
{
    public static cmlData AsCML(this OBMol molecule)
    {
        uint num_atoms = molecule.NumAtoms();
        Debug.Log($"[OpenBabelExtensions] Got {num_atoms} atoms.");

        List<Vector3> pos_vec = new List<Vector3>();
        Vector3 mean_pos = Vector3.zero;
        List<string> symbols = new List<string>();
        List<ushort> hybridizatons = new List<ushort>();
        foreach (var atom in molecule.Atoms())
        {
            var current_pos = atom.GetVector().AsVector3() * GlobalCtrl.scale / GlobalCtrl.u2aa;
            pos_vec.Add(current_pos);
            mean_pos += current_pos;
            symbols.Add(GlobalCtrl.Singleton.list_ElementData[(int)atom.GetAtomicNum()].m_abbre);
            hybridizatons.Add((ushort)atom.GetHyb());
            // Debug.Log($"[OpenBabelExtensions] Atom has type: {GlobalCtrl.Singleton.list_ElementData[(int)atom.GetAtomicNum()].m_abbre}, atomic number: {atom.GetAtomicNum()}, hyb: {atom.GetHyb()}");
        }
        mean_pos /= num_atoms;

        List<cmlBond> list_bond = new List<cmlBond>();
        foreach (var bond in molecule.Bonds())
        {
            var a = (ushort)(bond.GetBeginAtomIdx() - 1);
            var b = (ushort)(bond.GetEndAtomIdx() - 1);
            var order = (float)bond.GetBondOrder();

            list_bond.Add(new cmlBond(a, b, order));
        }

        var mol_id = GlobalCtrl.Singleton.getFreshMoleculeID();

        List<cmlAtom> list_atom = new List<cmlAtom>();
        for (ushort i = 0; i < num_atoms; i++)
        {
            pos_vec[i] -= mean_pos;
            list_atom.Add(new cmlAtom(i, symbols[i], hybridizatons[i], pos_vec[i]));
        }

        // init position is in front of current camera in atom world coordinates
        Vector3 create_position = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(CameraSwitcher.Singleton.currentCam.transform.position + 0.5f * CameraSwitcher.Singleton.currentCam.transform.forward);
        cmlData tempData = new cmlData(create_position, Vector3.one,Quaternion.identity, mol_id, list_atom, list_bond, null, null, true); // TODO maybe get angles and torsions out of OpenBabel

        return tempData;
    }

    public static OBMol AsOBMol(this cmlData molecule)
    {
        var obmol = new OBMol();
        var atomlist = new List<OBAtom>();
        foreach (var atom in molecule.atomArray)
        {
            var obatom = new OBAtom();
            var element = GlobalCtrl.Singleton.list_ElementData.Find(x => x.m_abbre == atom.abbre);
            obatom.SetAtomicNum(GlobalCtrl.Singleton.list_ElementData.IndexOf(element));
            obatom.SetId((uint)(atom.id + 1));
            var pos = atom.pos.ToVector3() * GlobalCtrl.u2aa / GlobalCtrl.scale;
            obatom.SetVector(pos.AsOBVector3());
            obatom.SetHyb(atom.hybrid);
            atomlist.Add(obatom);
            obmol.AddAtom(obatom);
        }

        int i = 1;
        foreach (var bond in molecule.bondArray)
        {
            var obbond = new OBBond();
            obbond.Set(i, atomlist[bond.id1], atomlist[bond.id2], (int)bond.order, 0);
            obmol.AddBond(obbond);
            i++;
        }

        return obmol;
    }
    public static cmlData AsCML(this Molecule mol)
    {
        // this method preserves the position of the molecules and atoms (and rotation)
        cmlData saveData;

        mol.shrinkAtomIDs();
        List<cmlAtom> list_atom = new List<cmlAtom>();
        foreach (Atom a in mol.atomList)
        {
            list_atom.Add(new cmlAtom(a.m_id, a.m_data.m_abbre, a.m_data.m_hybridization, a.transform.localPosition));
        }

        List<cmlBond> list_bond = new List<cmlBond>();
        foreach (var b in mol.bondTerms)
        {
            list_bond.Add(new cmlBond(b.Atom1, b.Atom2, b.order, b.eqDist, b.kBond));
        }
        List<cmlAngle> list_angle = new List<cmlAngle>();
        foreach (var b in mol.angleTerms)
        {
            list_angle.Add(new cmlAngle(b.Atom1, b.Atom2, b.Atom3, b.eqAngle, b.kAngle));
        }
        List<cmlTorsion> list_torsion = new List<cmlTorsion>();
        foreach (var b in mol.torsionTerms)
        {
            list_torsion.Add(new cmlTorsion(b.Atom1, b.Atom2, b.Atom3, b.Atom4, b.eqAngle, b.vk, b.nn));
        }

        saveData = new cmlData(mol.transform.localPosition, mol.transform.localScale, mol.transform.localRotation, mol.m_id, list_atom, list_bond, list_angle, list_torsion, true);


        return saveData;
    }

    /// <summary>
    /// Convert an OpenBabel <see cref="OBMol"/> to a SMILES string.
    /// </summary>
    public static string AsSMILES(this OBMol molecule)
    {
        var conv = new OBConversion();
        conv.SetOutFormat("SMI");
        return conv.WriteString(molecule).Trim();
    }

    /// <summary>
    /// Convert an OpenBabel <see cref="OBVector3"/> to a Unity <see cref="Vector3"/>.
    /// </summary>
    public static OBVector3 AsOBVector3(this Vector3 vector)
    {
        return new OBVector3(vector.x, vector.y, -vector.z);
    }

    /// <summary>
    /// Convert a Unity <see cref="Vector3"/> to an OpenBabel <see cref="OBVector3"/>.
    /// </summary>
    public static Vector3 AsVector3(this OBVector3 vector)
    {
        return new Vector3((float)vector.GetX(), (float)vector.GetY(), -(float)vector.GetZ());
    }

}
