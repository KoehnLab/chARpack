using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using chARpackStructs;
using OpenBabel;
using System.Collections;
using System.Threading.Tasks;
using SimpleFileBrowser;
using System;

/// <summary>
/// This class provides methods to read molecule data from files using OpenBabel.
/// </summary>
public class OpenBabelReadWrite : MonoBehaviour
{
    private static OpenBabelReadWrite _singleton;
    public static OpenBabelReadWrite Singleton
    {
        get => _singleton;
        private set

        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != null)
            {
                UnityEngine.Debug.Log($"{nameof(OpenBabelReadWrite)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    private string[] supportedInputFormats = null;
    private string[] supportedOutputFormats = null;

    private void Awake()
    {
        Singleton = this;

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

    /// <summary>
    /// Opens the file browser and waits for the user to select a file.
    /// If the file path is valid, the molecule is loaded.
    /// </summary>
    public void openLoadFileDialog()
    {
        // implementation using EditorUtility (pauses main execution loop)
        //#if !WINDOWS_UWP

        //        var path = EditorUtility.OpenFilePanel("Open Molecule File", "", "");

        //        if (path.Length != 0)
        //        {
        //            // do checks on file
        //            FileInfo fi = new FileInfo(path);
        //            UnityEngine.Debug.Log($"[ReadMoleculeFile] Current extension: {fi.Extension}");
        //            if (!fi.Exists)
        //            {
        //                UnityEngine.Debug.LogError("[ReadMoleculeFile] Something went wrong during path conversion. Abort.");
        //                return;
        //            }
        //            loadMolecule(fi);
        //        }
        //#endif
        StartCoroutine( ShowLoadDialogCoroutine() );
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files);


        if (FileBrowser.Success)
        {
            if (FileBrowser.Result.Length != 1)
            {
                UnityEngine.Debug.LogError("[ReadMoleculeFile] Path from FileBrowser is empty. Abort.");
                yield break;
            }
            FileInfo fi = new FileInfo(FileBrowser.Result[0]);
            UnityEngine.Debug.Log($"[ReadMoleculeFile] Current extension: {fi.Extension}");
            if (!fi.Exists)
            {
                UnityEngine.Debug.LogError("[ReadMoleculeFile] Something went wrong during path conversion. Abort.");
                yield break;
            }
            yield return loadMoleculeUI(fi);
        }
    }

    /// <summary>
    /// Opens a file browser and waits for the user to select a path.
    /// If the path and the selected extension are valid, saves marked molecules
    /// to a file.
    /// </summary>
    public void openSaveFileDialog()
    {
        //#if !WINDOWS_UWP
        //        var path = EditorUtility.SaveFilePanel("Save Molecule to File", Application.streamingAssetsPath + "/SavedMolecules/", "mol01", supportedOutputFormats.AsCommaSeparatedString());
        //        if (path.Length != 0)
        //        {
        //            // do checks on file
        //            FileInfo fi = new FileInfo(path);
        //            if (!checkOutputSupported(path))
        //            {
        //                UnityEngine.Debug.LogError($"[SaveMolecule] Chosen output format {fi.Extension} is not supported.");
        //                return;
        //            }
        //            // TODO How to select molecule to save?
        //            if (GlobalCtrl.Singleton.List_curMolecules.Count < 1)
        //            {
        //                UnityEngine.Debug.LogError("[SaveMolecule] No Molecules in currently in scene.");
        //                return;
        //            }
        //            Molecule mol;
        //            var selectedObject = Selection.activeGameObject;
        //            var objMol = selectedObject?.GetComponent<Molecule>();
        //            mol = objMol ? objMol : GlobalCtrl.Singleton.List_curMolecules[0];

        //            saveMolecule(mol, fi);
        //        }
        //#endif
        StartCoroutine(ShowSaveDialogCoroutine());
    }

    IEnumerator ShowSaveDialogCoroutine()
    {
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files);


        if (FileBrowser.Success)
        {
            if (FileBrowser.Result.Length != 1)
            {
                UnityEngine.Debug.LogError("[SaveMoleculeFile] Path from FileBrowser is empty. Abort.");
                yield break;
            }
            FileInfo fi = new FileInfo(FileBrowser.Result[0]);
            if (!checkOutputSupported(FileBrowser.Result[0]))
            {
                UnityEngine.Debug.LogError($"[SaveMolecule] Chosen output format {fi.Extension} is not supported.");
                yield break;
            }

            if (GlobalCtrl.Singleton.List_curMolecules.Count < 1)
            {
                UnityEngine.Debug.LogError("[SaveMolecule] No Molecules in currently in scene.");
                yield break;
            }

            var mols = new List<Molecule>();
            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
            {
                if (mol.isMarked)
                {
                    mols.Add(mol);
                }
            }
            if (mols.Count < 1)
            {
                mols.Add(GlobalCtrl.Singleton.List_curMolecules.GetFirst());
            }

            foreach (var mol in mols)
            {
                if (mols.Count > 1)
                {
                    var f_dir = fi.Directory;
                    var f_name = Path.GetFileNameWithoutExtension(fi.FullName);
                    var f_ext = fi.Extension;
                    var new_path = Path.Combine(f_dir.FullName, f_name + "_" + mol.m_id + f_ext);
                    var new_fi = new FileInfo(new_path);
                    yield return saveMolecule(mol, new_fi);
                }
                else
                {
                    yield return saveMolecule(mol, fi);
                }
            }
        }
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

    public List<cmlData> loadMolecule(FileInfo fi)
    {

        if (!checkInputSupported(fi.Name))
        {

            UnityEngine.Debug.LogError($"[ReadMoleculeFile] File {fi.Name} with extension {fi.Extension} is not in list of supported formats.");
            return null;
        }

        UnityEngine.Debug.Log($"[ReadMoleculeFile] Loading Molecule {fi.FullName}");
        List<cmlData> saveData = new List<cmlData>();
        if (fi.Extension.ToLower() == ".xml")
        {
            saveData = (List<cmlData>)XMLFileHelper.LoadData(fi.FullName, typeof(List<cmlData>));
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
                            new_x = UnityEngine.Random.Range(-1.0f, 1.0f);
                            new_y = UnityEngine.Random.Range(-1.0f, 1.0f);
                            new_z = UnityEngine.Random.Range(-1.0f, 1.0f);
                        }
                        else
                        {
                            new_x = UnityEngine.Random.Range(minmax_x[0], minmax_x[1]);
                            new_y = UnityEngine.Random.Range(minmax_y[0], minmax_y[1]);
                            new_z = UnityEngine.Random.Range(minmax_z[0], minmax_z[1]);
                        }

                        var new_pos = new OBVector3(new_x, new_y, new_z);
                        obmol.GetAtom(i+1).SetVector(new_pos);
                    }
                }
            }
            var mol_id = Guid.NewGuid();
            saveData.Add(new Tuple<Guid, OBMol>(mol_id, obmol).AsCML());
        }

        if (saveData == null || saveData.Count == 0)
        {
            UnityEngine.Debug.LogError($"[ReadMoleculeFile] File {fi.FullName} could not be read. Abort.");
            return null;
        }

        return saveData;
    }

    public IEnumerator loadMoleculeUI(FileInfo fi)
    {
        var mol = loadMolecule(fi);
        if (mol == null) yield break;
        GlobalCtrl.Singleton.createFromCML(mol);
        NetworkManagerServer.Singleton.pushLoadMolecule(mol);
    }

    /// <summary>
    /// Saves a molecule to the specified file, either in XML format
    /// or a format supported by OpenBabel.
    /// </summary>
    /// <param name="mol"></param>
    /// <param name="fi"></param>
    /// <returns></returns>
    public IEnumerator saveMolecule(Molecule mol, FileInfo fi)
    {
        UnityEngine.Debug.Log($"[ReadMoleculeFile] Saving Molecule {fi.FullName}");
        if (fi.Extension.ToLower() == ".xml")
        {
            XMLFileHelper.SaveData(fi.FullName, mol.AsCML());
        }
        else
        {
            var conv = new OBConversion();
            conv.SetOutFormat(fi.Extension);
            conv.WriteFile(mol.AsCML().AsOBMol(), fi.FullName);
        }
        return null;
    }

    public void generateSVG()
    {
        var mols = new List<Molecule>();
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
        {
            if (mol.isMarked)
            {
                mols.Add(mol);
            }
        }
        if (mols.Count < 1)
        {
            mols.Add(GlobalCtrl.Singleton.List_curMolecules.GetFirst());
        }

        var obmol = mols[0].AsCML().AsOBMol();

        // convert to xyz
        var convXYZ = new OBConversion();
        convXYZ.SetOutFormat(OBFormat.FindType("xyz"));
        var xyz = convXYZ.WriteString(obmol);

        // read back for unbiased obmol
        var newobmol = new OBMol();
        var conv_ = new OBConversion();
        conv_.SetInFormat(OBFormat.FindType("xyz"));
        conv_.ReadString(newobmol, xyz);
        
        // create bonds etc.
        var builder = new OBBuilder();
        builder.Build(newobmol);

        // convert to smiles
        var conv = new OBConversion();
        conv.SetOutFormat(OBFormat.FindType("smiles"));
        UnityEngine.Debug.Log(conv.WriteString(newobmol));

        conv.SetOutFormat(OBFormat.FindType("svg"));
        conv.WriteFile(newobmol, $"{Application.streamingAssetsPath}/SavedMolecules/blub.svg");

        UnityEngine.Debug.Log($"Has 2D information: {newobmol.Has2D()}");


        float min_x = 0f;
        float max_x = 0f;
        float min_y = 0f;
        float max_y = 0f;
        float min_z = 0f;
        float max_z = 0f;
        var atom0 = newobmol.GetFirstAtom();
        if (atom0 != null)
        {
            min_x = max_x = (float)atom0.GetVector().GetX();
            min_y = max_y = (float)atom0.GetVector().GetY();
            min_z = max_z = (float)atom0.GetVector().GetZ();
            foreach (var atom in newobmol.Atoms())
            {
                min_x = Mathf.Min(min_x, (float)atom.GetVector().GetX());
                max_x = Mathf.Max(max_x, (float)atom.GetVector().GetX());
                min_y = Mathf.Min(min_y, (float)atom.GetVector().GetY());
                max_y = Mathf.Max(max_y, (float)atom.GetVector().GetY());
                min_z = Mathf.Min(min_z, (float)atom.GetVector().GetZ());
                max_z = Mathf.Max(max_z, (float)atom.GetVector().GetZ());
            }
        }

        UnityEngine.Debug.Log($"minX: {min_x}, minY: {min_y}, minZ: {min_z}");


        var margin = 40f;

        foreach (var atom in newobmol.Atoms())
        {
            UnityEngine.Debug.Log($"vector: {(atom.GetVector().GetX() - min_x) + margin} {(atom.GetVector().GetY() - min_y) + margin} {(atom.GetVector().GetZ() - min_z) + margin}");
        }


    }



    public bool createSmiles(string smiles)
    {
        try
        {
            var conv = new OBConversion();
            conv.SetInFormat(OBFormat.FindType("smiles"));
            var obmol = new OBMol();
            var valid = conv.ReadString(obmol, smiles);
            if (obmol == null || !valid) {
                UnityEngine.Debug.LogError("Invalid SMILES string.");
                return false;
            }

            obmol.AddHydrogens();
            var builder = new OBBuilder();
            builder.Build(obmol);
            OpenBabelForceField.MinimiseStructure(obmol, 500);

            var mol_id = Guid.NewGuid();
            List<cmlData> mol = new List<cmlData>();
            mol.Add(new Tuple<Guid, OBMol>(mol_id, obmol).AsCML());
            foreach(cmlData mole in mol)
            {
                for(int i=0; i<mole.atomArray.Length;i++)
                {
                    if(mole.atomArray[i].abbre.Equals("H") || mole.atomArray[i].abbre.Equals("Dummy"))
                    {
                        mole.atomArray[i].hybrid = 0;
                    }
                }
            }
            GlobalCtrl.Singleton.createFromCML(mol);
            NetworkManagerServer.Singleton.pushLoadMolecule(mol);
        }
        catch
        {
            UnityEngine.Debug.LogError("Invalid SMILES string.");
            return false;
        }
        return true;
    }

}
