using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Text;
using System;
using UnityEditor;
using StructClass;


public class ReadMoleculeFile : MonoBehaviour
{
    private string[] supportedFormats = null;

    private void Awake()
    {
        IntPtr formatsUnmanagedStringArray = IntPtr.Zero;

        var num_formats = getSupportedFormats(out formatsUnmanagedStringArray);

        string[] formatsManagedStringArray = null;
        supportedFormats = new string[num_formats];

        ConvertHelpers.MarshalUnmananagedStrArray2ManagedStrArray(formatsUnmanagedStringArray, num_formats, out formatsManagedStringArray);
        int i = 0;
        foreach (var format in formatsManagedStringArray) 
        {
            UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported Format: {format}.");
            supportedFormats[i] = format.Split(" ")[0];
            UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported Format short: {supportedFormats[i]}.");
            i++;
        }
    }

    public void openFileDialog()
    {
#if !WINDOWS_UWP
        var path = EditorUtility.OpenFilePanel("Open Molecule File", "", "");
        if (path.Length != 0)
        {
            // do checks on file
            FileInfo fi = new FileInfo(path);
            if (!fi.Exists)
            {
                foreach (var sformat in supportedFormats)
                {
                    if (fi.Extension.Contains("." + sformat)) {
                        var split = path.Split("." + sformat);
                        if (split[1].Length > 1)
                        {
                            UnityEngine.Debug.LogError("[ReadMoleculeFile] Something went wrong during path conversion. Abort.");
                            return;
                        } else
                        {
                            path = split[0] + "." + sformat;
                            fi = new FileInfo(path);
                        }
                    }
                }
                if (!fi.Exists)
                {
                    UnityEngine.Debug.LogError("[ReadMoleculeFile] Something went wrong during path conversion. Abort.");
                    return;
                }

            }
            loadMolecule(path);
        }
#endif
    }

    private void loadMolecule(string path)
    {


        //DirectoryInfo dir = new DirectoryInfo(Application.streamingAssetsPath);
        //FileInfo[] info = dir.GetFiles("*.*");
        //foreach (FileInfo f in info)
        //{
        //    if (f.Extension == ".xyz")
        //    {
        //        UnityEngine.Debug.Log(f.FullName);
        //        fname = Encoding.ASCII.GetBytes(f.FullName);
        //    }
        //}

        byte[] fname = Encoding.ASCII.GetBytes(path);

        int exit_code = doTheRead(fname);
        if (exit_code == 1)
        {
            UnityEngine.Debug.LogError("[ReadMoleculeFile] Native plugin returned with an error.");
        }
        int num_atoms = getNumAtoms();
        UnityEngine.Debug.Log("[ReadMoleculeFile] Native plugin returned " + num_atoms + " atoms.");

        //for (int i = 0; i < num_atoms; i++)
        //{
        //    double[] current_pos = new double[3];
        //    getPos(i, current_pos);
        //    UnityEngine.Debug.LogError("[ReadMoleculeFile] Current atom position" + current_pos[0]);
        //}

        double[] all_positions = new double[3 * num_atoms];
        List<Vector3> pos_vec = new List<Vector3>();
        Vector3 mean_pos = Vector3.zero;
        exit_code = getAllPositions(all_positions);
        if (exit_code == 1)
        {
            UnityEngine.Debug.LogError("[ReadMoleculeFile] Native plugin returned with an error.");
        }
        for (int i = 0; i < num_atoms; i++)
        {
            UnityEngine.Debug.Log("[ReadMoleculeFile] Current atom position " + all_positions[3 * i + 0] + " " + all_positions[3 * i + 1] + " " + all_positions[3 * i + 2]);
            var current_pos = new Vector3((float)all_positions[3 * i + 0], (float)all_positions[3 * i + 1], (float)all_positions[3 * i + 2]);
            current_pos /= GlobalCtrl.Singleton.u2aa;
            pos_vec.Add(current_pos);
            mean_pos += current_pos;
        }
        mean_pos /= num_atoms;

        int num_single_bonds = getNumSingleBonds();
        UnityEngine.Debug.Log("[ReadMoleculeFile] Num single bonds " + num_single_bonds);
        int num_angle_bonds = getNumAngleBonds();
        UnityEngine.Debug.Log("[ReadMoleculeFile] Num angle bonds " + num_angle_bonds);
        int num_torsion_bonds = getNumTorsionBonds();
        UnityEngine.Debug.Log("[ReadMoleculeFile] Num torsion bonds " + num_torsion_bonds);


        int[] single_bonds = new int[2 * num_single_bonds];
        exit_code = getSingleBonds(single_bonds);
        if (exit_code == 1)
        {
            UnityEngine.Debug.LogError("[ReadMoleculeFile] Native plugin returned with an error.");
        }
        for (int i = 0; i < num_single_bonds; i++)
        {
            UnityEngine.Debug.Log("[ReadMoleculeFile] Atom " + single_bonds[2 * i + 0] + " binds to Atom " + single_bonds[2 * i + 1]);
        }
        int[] atomicNumbers = new int[num_atoms];
        getAtomicNumbers(atomicNumbers);

        List<string> symbols = new List<string>();
        foreach (var number in atomicNumbers)
        {
            byte[] symbol = Encoding.ASCII.GetBytes("###");
            getSymbol(number, symbol);
            var symbol_str = Encoding.ASCII.GetString(symbol);
            symbol_str = symbol_str.Trim('#');
            symbols.Add(symbol_str);
            UnityEngine.Debug.Log($"[ReadMoleculeFile] Atomic number {number} is ({symbol_str.Length}) symbol {symbol_str}");
        }

        UnityEngine.Debug.Log($"[ReadMoleculeFile] Dic_ElementData: {GlobalCtrl.Singleton.Dic_ElementData}");


        // construct cml data
        var mol_id = GlobalCtrl.Singleton.getFreshMoleculeID();
        var atom_id = GlobalCtrl.Singleton.getFreshAtomID();
        List<ushort> new_ids = new List<ushort>();

        List<cmlData> saveData = new List<cmlData>();
        List<cmlAtom> list_atom = new List<cmlAtom>();
        // TODO: needs better way for hybridization
        var hybridization = GlobalCtrl.Singleton.curHybrid;
        for (int i = 0; i < num_atoms; i++)
        {
            pos_vec[i] -= mean_pos;
            list_atom.Add(new cmlAtom(atom_id, symbols[i], hybridization, pos_vec[i]));
            new_ids.Add(atom_id);
            atom_id++;
        }
        List<cmlBond> list_bond = new List<cmlBond>();
        // TODO: needs better way for bond order
        var bond_order = 1.0f;
        for (int j = 0; j < num_single_bonds; j++)
        {
            list_bond.Add(new cmlBond(new_ids[single_bonds[2 * j + 0]-1], new_ids[single_bonds[2 * j + 1]-1], bond_order));
        }
        // init position is in front of current camera
        Vector3 create_position = CameraSwitcher.Singleton.currentCam.transform.position + 0.5f * CameraSwitcher.Singleton.currentCam.transform.forward;
        cmlData tempData = new cmlData(create_position, Quaternion.identity, mol_id, list_atom, list_bond);
        saveData.Add(tempData);

        GlobalCtrl.Singleton.rebuildAtomWorld(saveData, true);
        NetworkManagerServer.Singleton.pushLoadMolecule(saveData);

    }

    // import plugin interface functions
    [DllImport("NativeChemToolkit")]
    private static extern int doTheRead(byte[] fname);
    [DllImport("NativeChemToolkit")]
    private static extern int getNumAtoms();
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getAllPositions(double[] out_array);
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getNumBondsOf(int idx);
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getNumSingleBonds();
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getNumAngleBonds();
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getNumTorsionBonds();
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getSingleBonds(int[] out_array);
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getAtomicNumbers(int[] out_array);
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    private static extern int getSymbol(int atomic_number, byte[] out_array);
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.StdCall)]
    private static extern int getSupportedFormats(out IntPtr out_array);


}
