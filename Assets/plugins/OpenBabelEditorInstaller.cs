#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;

[InitializeOnLoad]
public class OpenBabelEditorInstaller
{
    static OpenBabelEditorInstaller()
    {
        // TODO: add download of zip file
        Debug.Log("[OpenBabelEditorInstaller] Checking for openbabel.");
        var openbabel_path = Path.Combine(Path.Combine(Application.dataPath, ".."), "openbabel");
        var path_to_zip = Path.GetFullPath(Path.Combine(Path.Combine(Application.dataPath, ".."), "openbabel.zip"));
        var path_to_source_dll = Path.Combine(openbabel_path, "OBDotNet.dll");
        var path_to_target_dll = Path.Combine(Path.Combine(Application.dataPath, "plugins"), "OBDotNet.dll");
        if (!Directory.Exists(openbabel_path))
        {
            if (!File.Exists(path_to_zip))
            {
                Debug.LogError("[OpenBabelEditorInstaller] openbabel.zip not found. Please install openbabel manually.");
                return;
            }
            Debug.Log("[OpenBabelEditorInstaller] Extracting openbabel.zip");
            ZipFile.ExtractToDirectory(path_to_zip, openbabel_path, true);
            

        }
        else
        {
            // check if zip has been updated
            if (!File.Exists(path_to_zip))
            {
                var dir_last_modified = Directory.GetLastWriteTime(openbabel_path);
                var zip_last_modified = File.GetLastWriteTime(path_to_zip);
                if (zip_last_modified > dir_last_modified)
                {
                    Debug.Log("[OpenBabelEditorInstaller] Updating openbabel with content from zip.");
                    Directory.Delete(openbabel_path);
                    ZipFile.ExtractToDirectory(path_to_zip, openbabel_path, true);
                }
            }
        }
        if (!File.Exists(path_to_target_dll))
        {
            Debug.Log("[OpenBabelEditorInstaller] Placing DLL in plugins");
            FileUtil.CopyFileOrDirectory(path_to_source_dll, path_to_target_dll);
        }
        {
            // check if OBDotNet has update
            var plugins_dotnet_last_modified = File.GetLastWriteTime(path_to_target_dll);
            var zip_dotnet_last_modified = File.GetLastWriteTime(path_to_source_dll);
            if (zip_dotnet_last_modified > plugins_dotnet_last_modified)
            {
                Debug.Log("[OpenBabelEditorInstaller] Updating DLL in plugins");
                File.Delete(path_to_target_dll);
                FileUtil.CopyFileOrDirectory(path_to_source_dll, path_to_target_dll);
            }
        }
    }

}
#endif
