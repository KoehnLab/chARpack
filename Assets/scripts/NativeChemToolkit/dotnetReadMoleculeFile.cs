using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using StructClass;
using OpenBabel;

public class dotnetReadMoleculeFile : MonoBehaviour
{
    private string[] supportedInputFormats = null;
    private string[] supportedOutputFormats = null;

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
        var inFormats = conv.GetSupportedInputFormat();
        var outFormats = conv.GetSupportedOutputFormat();

        UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported input Formats {inFormats.Count}.");
        UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported output Formats {outFormats.Count}.");

        supportedInputFormats = new string[inFormats.Count + 1];
        supportedOutputFormats = new string[outFormats.Count + 1];

        int i = 0;
        foreach (var format in inFormats)
        {
            //UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported Format: {format}.");
            supportedInputFormats[i] = format.Split(" ")[0];
            //UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported input Format: {supportedInputFormats[i]}.");
            i++;
        }
        supportedInputFormats[inFormats.Count] = "xml";

        int j = 0;
        foreach (var format in outFormats)
        {
            //UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported Format: {format}.");
            supportedOutputFormats[j] = format.Split(" ")[0];
            //UnityEngine.Debug.Log($"[ReadMoleculeFile] Supported output Format: {supportedInputFormats[i]}.");
            j++;
        }
        supportedOutputFormats[outFormats.Count] = "xml";
    }

    public void openLoadFileDialog()
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
            loadMolecule(fi);
        }
#endif
    }

    public void openSaveFileDialog()
    {
#if !WINDOWS_UWP
        var path = EditorUtility.SaveFilePanel("Save Molecule to File", Application.streamingAssetsPath + "/SavedMolecules/", "mol01", supportedOutputFormats.AsCommaSeparatedString());
        if (path.Length != 0)
        {
            // do checks on file
            FileInfo fi = new FileInfo(path);
            if (!checkOutputSupported(path))
            {
                UnityEngine.Debug.LogError($"[SaveMolecule] Chosen output format {fi.Extension} is not supported.");
                return;
            }
            // TODO How to select molecule to save?
            if (GlobalCtrl.Singleton.List_curMolecules.Count < 1)
            {
                UnityEngine.Debug.LogError("[SaveMolecule] No Molecules in currently in scene.");
                return;
            }
            Molecule mol;
            var selectedObject = Selection.activeGameObject;
            var objMol = selectedObject?.GetComponent<Molecule>();
            mol = objMol ? objMol : GlobalCtrl.Singleton.List_curMolecules[0];

            saveMolecule(mol, fi);
        }
#endif
    }

    private bool checkInputSupported(string path)
    {
        bool supported = false;
        FileInfo fi = new FileInfo(path);
        foreach (var format in supportedInputFormats)
        {
            if (fi.Extension.ToLower().Contains(format))
            {
                supported = true;
                break;
            }
        }
        return supported;
    }

    private bool checkOutputSupported(string path)
    {
        bool supported = false;
        FileInfo fi = new FileInfo(path);
        foreach (var format in supportedOutputFormats)
        {
            if (fi.Extension.ToLower().Contains(format))
            {
                supported = true;
                break;
            }
        }
        return supported;
    }

    private void loadMolecule(FileInfo fi)
    {

        if (!checkInputSupported(fi.Name))
        {

            UnityEngine.Debug.LogError($"[ReadMoleculeFile] File {fi.Name} with extension {fi.Extension} is not in list of supported formats.");
            return;
        }

        UnityEngine.Debug.Log($"[ReadMoleculeFile] Loading Molecule {fi.FullName}");
        List<cmlData> saveData = new List<cmlData>();
        if (fi.Extension.ToLower() == ".xml")
        {
            saveData = (List<cmlData>)CFileHelper.LoadData(fi.FullName, typeof(List<cmlData>));
        }
        else
        {

            // do the read
            var conv = new OBConversion(fi.Name);
            var obmol = new OBMol();
            conv.ReadFile(obmol, fi.FullName);

            // check if loaded molecule has 3D structure data
            if (obmol.GetDimension() != 3)
            {
                // this code is from avogadro to build/guess the structure
                var builder = new OBBuilder();
                builder.Build(obmol);
                obmol.AddHydrogens(); // Add some hydrogens before running force field


                OpenBabelForceField.MinimiseStructure(obmol, 250);
                //// do some FF iterations
                //var pFF = OpenBabelForceField.GetForceField("mmff94s", obmol);
                //if (pFF == null)
                //{
                //    pFF = OpenBabelForceField.GetForceField("UFF", obmol);
                //    if (pFF == null)
                //    {
                //        UnityEngine.Debug.LogError("[OBReaderInterface] Could not setup Force Field for generating molecule structure.");
                //        return; // can't do anything more
                //    }
                //}
                //pFF.ConjugateGradients(250, 1.0e-4);
                //pFF.UpdateCoordinates(obmol);
            }
            saveData.Add(obmol.AsCML());
        }

        if (saveData == null || saveData.Count == 0)
        {
            UnityEngine.Debug.LogError($"[ReadMoleculeFile] File {fi.FullName} could not be read. Abort.");
            return;
        }

        GlobalCtrl.Singleton.rebuildAtomWorld(saveData, true);
        NetworkManagerServer.Singleton.pushLoadMolecule(saveData);
    }

    public void saveMolecule(Molecule mol, FileInfo fi)
    {
        UnityEngine.Debug.Log($"[ReadMoleculeFile] Saving Molecule {fi.FullName}");
        if (fi.Extension.ToLower() == ".xml")
        {
            CFileHelper.SaveData(fi.FullName, mol.AsCML());
        }
        else
        {
            var conv = new OBConversion();
            conv.SetOutFormat(fi.Extension);
            conv.WriteFile(mol.AsCML().AsOBMol(), fi.FullName);
        }
    }

}
