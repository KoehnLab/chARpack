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


                OpenBabelForceField.MinimiseStructure(obmol, 500); // or do FF interations (alternative code below)
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

                // check for NaNs and infinities
                List<bool> finites = new List<bool>();

                foreach (var atom in obmol.Atoms())
                {
                    finites.Add(double.IsFinite(atom.GetX()) && double.IsFinite(atom.GetY()) && double.IsFinite(atom.GetZ()));
                }

                Vector3 com = Vector3.zero;
                int num_finites = 0;
                Vector2 minmax_x = new Vector2(float.MaxValue, float.MinValue);
                Vector2 minmax_y = new Vector2(float.MaxValue, float.MinValue);
                Vector2 minmax_z = new Vector2(float.MaxValue, float.MinValue);
                for (int i = 0; i < finites.Count; i++)
                {
                    if (finites[i])
                    {
                        var pos = obmol.GetAtom(i+1).GetVector().AsVector3();
                        com += pos;
                        minmax_x[0] = Mathf.Min(minmax_x[0], pos.x);
                        minmax_x[1] = Mathf.Max(minmax_x[1], pos.x);
                        minmax_y[0] = Mathf.Min(minmax_y[0], pos.y);
                        minmax_y[1] = Mathf.Max(minmax_y[1], pos.y);
                        minmax_z[0] = Mathf.Min(minmax_z[0], pos.z);
                        minmax_z[1] = Mathf.Max(minmax_z[1], pos.z);
                        num_finites++;
                    }
                }

                UnityEngine.Debug.Log($"[Num Finites] {num_finites}");
                UnityEngine.Debug.Log($"[Num Atoms] {obmol.NumAtoms()}");
                UnityEngine.Debug.Log($"[Num Non-Finites] {obmol.NumAtoms() - num_finites}");

                bool all_not_finite = finites.TrueForAll(x => !x);
                // Replace NaNs and Infinites with random positions
                for (int i = 0; i < finites.Count; i++)
                {
                    if (!finites[i])
                    {
                        float new_x;
                        float new_y;
                        float new_z;
                        if (all_not_finite)
                        {
                            new_x = Random.Range(-1.0f, 1.0f);
                            new_y = Random.Range(-1.0f, 1.0f);
                            new_z = Random.Range(-1.0f, 1.0f);
                        }
                        else
                        {
                            new_x = Random.Range(minmax_x[0], minmax_x[1]);
                            new_y = Random.Range(minmax_y[0], minmax_y[1]);
                            new_z = Random.Range(minmax_z[0], minmax_z[1]);
                        }

                        var new_pos = new OBVector3(new_x, new_y, new_z);
                        obmol.GetAtom(i+1).SetVector(new_pos);
                    }
                }
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
