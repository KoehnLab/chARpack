
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;

[InitializeOnLoad]
public class DLLEraser
{
    static DLLEraser()
    {
        checkDLLs();
    }

    public static string package_path = Path.GetFullPath(Path.Combine(Application.dataPath, "Packages"));
    public static string vcrt_base_name = "Microsoft.VCRTForwarders";
    public static string qr_base_name = "Microsoft.MixedReality.QR";
    public static void checkDLLs()
    {
        if (Directory.Exists(package_path))
        {
            string[] subfolders = Directory.GetDirectories(package_path);
            foreach (var package_name in new string[] { vcrt_base_name, qr_base_name })
            {
                foreach (string folder in subfolders)
                {
                    string folderName = Path.GetFileName(folder);
                    if (folderName.StartsWith(package_name))
                    {
                        Debug.Log("[DLLEraser] Found matching folder: " + folder);

                        var runtimes_path = Path.Combine(folder, "runtimes");
                        var runtimes_meta = Path.Combine(folder, "runtimes.meta");
                        if (Directory.Exists(runtimes_path))
                        {
                            Directory.Delete(runtimes_path, true);
                        }
                        else
                        {
                            Debug.Log("[DLLEraser] No runtimes folder found.");
                        }
                        if (File.Exists(runtimes_meta))
                        {
                            File.Delete(runtimes_meta);
                        }

                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[DLLEraser] Base path does not exist: " + package_path);
        }
    }

 

}
#endif

