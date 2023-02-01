using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using StructClass;
using OpenBabel;

public class dotnetReadMoleculeFile : MonoBehaviour
{
    private string[] supportedFormats = null;

    private void Awake()
    {
        // set babel data dir environment variable
        var path_to_plugins = Path.Combine(Application.dataPath, "plugins");
        System.Environment.SetEnvironmentVariable("BABEL_DATADIR", path_to_plugins);
        UnityEngine.Debug.Log($"[ReadMoleculeFile] Set env BABEL_DATADIR to {path_to_plugins}");

        // check for fragment files
        // TODO: implement alternative

        // setup OpenBabel
        if (!OpenBabelSetup.IsOpenBabelAvailable)
            OpenBabelSetup.ThrowOpenBabelNotFoundError();

        // get supported file formats
        var conv = new OBConversion();
        var formats = conv.GetSupportedInputFormat();

        supportedFormats = new string[formats.Count];

        int i = 0;
        foreach (var format in formats) 
        {
            //UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported Format: {format}.");
            supportedFormats[i] = format.Split(" ")[0];
            UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported Format: {supportedFormats[i]}.");
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
            UnityEngine.Debug.Log($"[ReadMoleculeFile] Current extension: {fi.Extension}");
            if (!fi.Exists)
            {
                UnityEngine.Debug.LogError("[ReadMoleculeFile] Something went wrong during path conversion. Abort.");
                return;
            }
            loadMolecule(path);
        }
#endif
    }

    private bool checkSupported(string path)
    {
        bool supported = false;
        FileInfo fi = new FileInfo(path);
        foreach (var format in supportedFormats)
        {
            if (fi.Extension.Contains(format))
            {
                supported = true;
                break;
            }
        }
        return supported;
    }

    private void loadMolecule(string path)
    {

        //if (!checkSupported(path))
        //{
        //    FileInfo fi = new FileInfo(path);
        //    UnityEngine.Debug.LogError($"[ReadMoleculeFile] File {path} with extension {fi.Extension} is not in list of supported formats.");
        //    return;
        //}

        // do the read
        var conv = new OBConversion(path);
        var mol = new OBMol();
        conv.ReadFile(mol, path);

        // check if loaded molecule has 3D structure data
        if (mol.GetDimension() != 3)
        {
            // this code is from avogadro to build/guess the structure
            var builder = new OBBuilder();
            builder.Build(mol);
            mol.AddHydrogens(); // Add some hydrogens before running force field


            OpenBabelForceField.MinimiseStructure(mol, 250);
            //// do some FF iterations
            //var pFF = OpenBabelForceField.GetForceField("mmff94s", mol);
            //if (pFF == null)
            //{
            //    pFF = OpenBabelForceField.GetForceField("UFF", mol);
            //    if (pFF == null)
            //    {
            //        UnityEngine.Debug.LogError("[OBReaderInterface] Could not setup Force Field for generating molecule structure.");
            //        return; // can't do anything more
            //    }
            //}

            //pFF.ConjugateGradients(250, 1.0e-4);
            //pFF.UpdateCoordinates(mol);
        }


        List<cmlData> saveData = new List<cmlData>();
        saveData.Add(mol.AsCML());

        GlobalCtrl.Singleton.rebuildAtomWorld(saveData, true);
        NetworkManagerServer.Singleton.pushLoadMolecule(saveData);
    }

}
