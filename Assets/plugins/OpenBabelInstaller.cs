using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Ionic.Zip;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class OpenBabelInstaller
{
#if UNITY_EDITOR
    static OpenBabelInstaller()
    {
        checkOpenBabelInstallation();
    }
#endif

    public static string openbabel_path = Path.GetFullPath(Path.Combine(Path.Combine(Application.dataPath, ".."), "openbabel"));
    public static string path_to_zip = Path.GetFullPath(Path.Combine(Path.Combine(Application.dataPath, ".."), "openbabel.zip"));
    public static string path_to_csharpdll = Path.Combine(openbabel_path, "openbabel_csharp.dll");
    public static string path_to_source_dll = Path.Combine(openbabel_path, "OBDotNet.dll");
    public static string path_to_target_dll = Path.Combine(Path.Combine(Application.dataPath, "plugins"), "OBDotNet.dll");
    public static string babel_datadir = Path.Combine(openbabel_path, "data");
    public static bool installationSuccessfull = false;
    public async static void checkOpenBabelInstallation()
    {
        // TODO: add download of zip file
        Debug.Log("[OpenBabelInstaller] Checking for openbabel.");
        if (!Directory.Exists(openbabel_path))
        {
            if (!File.Exists(path_to_zip))
            {
                await downloadOpenBabel();
            }
            Debug.Log("[OpenBabelInstaller] Extracting openbabel.zip");

            System.IO.Compression.ZipFile.ExtractToDirectory(path_to_zip, openbabel_path, true);
        }
        else
        {
            // check if zip has been updated
            if (!File.Exists(path_to_zip))
            {
                await downloadOpenBabel();
            }
            var dir_last_modified = Directory.GetLastWriteTime(openbabel_path);
            var zip_last_modified = File.GetLastWriteTime(path_to_zip);
            if (zip_last_modified > dir_last_modified || !File.Exists(path_to_csharpdll))
            {
                Debug.Log("[OpenBabelInstaller] Updating openbabel with content from zip.");
                Directory.Delete(openbabel_path);
                System.IO.Compression.ZipFile.ExtractToDirectory(path_to_zip, openbabel_path, true);
            }
        }
#if UNITY_EDITOR
        if (!File.Exists(path_to_target_dll))
        {
            Debug.Log("[OpenBabelInstaller] Placing DLL in plugins");
            FileUtil.CopyFileOrDirectory(path_to_source_dll, path_to_target_dll);
        }
        else
        {
            // check if OBDotNet has update
            var plugins_dotnet_last_modified = File.GetLastWriteTime(path_to_target_dll);
            var zip_dotnet_last_modified = File.GetLastWriteTime(path_to_source_dll);
            if (zip_dotnet_last_modified > plugins_dotnet_last_modified)
            {
                Debug.Log("[OpenBabelInstaller] Updating DLL in plugins");
                File.Delete(path_to_target_dll);
                FileUtil.CopyFileOrDirectory(path_to_source_dll, path_to_target_dll);
            }
        }
#endif
        installationSuccessfull = true;
    }

    public async static void checkOpenBabelInstallation_withProgress(Func<float?, bool> downloadProgressChanged, Action<object, ExtractProgressEventArgs> extractProgressChanged, Action<int> setTotalFilesInZip)
    {
        // TODO: add download of zip file
        Debug.Log("[OpenBabelInstaller] Checking for openbabel.");
        if (!Directory.Exists(openbabel_path))
        {
            if (!File.Exists(path_to_zip))
            {
                await downloadOpenBabel_withProgress(downloadProgressChanged);
            }
            Debug.Log("[OpenBabelInstaller] Extracting openbabel.zip");

            //ZipFile.ExtractToDirectory(path_to_zip, openbabel_path, true);
            using (ZipFile zip = ZipFile.Read(path_to_zip))
            {
                setTotalFilesInZip(zip.Count);
                zip.ExtractProgress += new EventHandler<ExtractProgressEventArgs>(extractProgressChanged);
                zip.ExtractAll(openbabel_path, ExtractExistingFileAction.OverwriteSilently);
            }
        }
        else
        {
            // check if zip has been updated
            if (!File.Exists(path_to_zip))
            {
                await downloadOpenBabel_withProgress(downloadProgressChanged);
            }
            var dir_last_modified = Directory.GetLastWriteTime(openbabel_path);
            var zip_last_modified = File.GetLastWriteTime(path_to_zip);
            if (zip_last_modified > dir_last_modified || !File.Exists(path_to_csharpdll))
            {
                Debug.Log("[OpenBabelInstaller] Updating openbabel with content from zip.");
                Directory.Delete(openbabel_path);
                //ZipFile.ExtractToDirectory(path_to_zip, openbabel_path, true);
                using (ZipFile zip = ZipFile.Read(path_to_zip))
                {
                    setTotalFilesInZip(zip.Count);
                    zip.ExtractProgress += new EventHandler<ExtractProgressEventArgs>(extractProgressChanged);
                    zip.ExtractAll(openbabel_path, ExtractExistingFileAction.OverwriteSilently);
                }
            }
        }
#if UNITY_EDITOR
        if (!File.Exists(path_to_target_dll))
        {
            Debug.Log("[OpenBabelInstaller] Placing DLL in plugins");
            FileUtil.CopyFileOrDirectory(path_to_source_dll, path_to_target_dll);
        }
        else
        {
            // check if OBDotNet has update
            var plugins_dotnet_last_modified = File.GetLastWriteTime(path_to_target_dll);
            var zip_dotnet_last_modified = File.GetLastWriteTime(path_to_source_dll);
            if (zip_dotnet_last_modified > plugins_dotnet_last_modified)
            {
                Debug.Log("[OpenBabelInstaller] Updating DLL in plugins");
                File.Delete(path_to_target_dll);
                FileUtil.CopyFileOrDirectory(path_to_source_dll, path_to_target_dll);
            }
        }
#endif
        installationSuccessfull = true;
    }

    async static Task downloadOpenBabel()
    {
        string url = "https://cloud.visus.uni-stuttgart.de/index.php/s/cPn0N9e27zM8Prb/download";
        HttpClient client = new HttpClient();
        // Send a GET request to the specified URL
        HttpResponseMessage response = await client.GetAsync(url);
        // Check if the response is successful (status code 200-299)
        if (response.IsSuccessStatusCode)
        {
            // Get the file content as a stream
            using (Stream fileStream = await response.Content.ReadAsStreamAsync())
            {
                // Save the stream to the specified file

                using (FileStream outputFileStream = new FileStream(path_to_zip, FileMode.Create))
                {
                    await fileStream.CopyToAsync(outputFileStream);
                }

                Debug.Log("[OpenBabelInstaller] OpenBabel downloaded successfully.");
            }
        }
        else
        {
            Debug.LogError($"[OpenBabelInstaller] Failed to download OpenBabel.\nStatus code: {response.StatusCode}");
        }
    }

    async static Task downloadOpenBabel_withProgress(Func<float?, bool> progressChanged)
    {
        string url = "https://cloud.visus.uni-stuttgart.de/index.php/s/cPn0N9e27zM8Prb/download";
        HttpClient client = new HttpClient();
        // Send a GET request to the specified URL
        HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength;

        using var contentStream = await response.Content.ReadAsStreamAsync();
        var totalBytesRead = 0L;
        var readCount = 0L;
        var buffer = new byte[8192];
        var isMoreToRead = true;

        static float? calculatePercentage(long? totalDownloadSize, long totalBytesRead) =>
            totalDownloadSize.HasValue ? (float)Math.Round((float)totalBytesRead / totalDownloadSize.Value * 100, 2) : null;
        using var fileStream = new FileStream(path_to_zip, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        do
        {
            var bytesRead = await contentStream.ReadAsync(buffer);
            if (bytesRead == 0)
            {
                isMoreToRead = false;

                if (progressChanged(calculatePercentage(totalBytes, totalBytesRead)))
                {
                    throw new OperationCanceledException();
                }

                continue;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

            totalBytesRead += bytesRead;
            readCount++;

            if (readCount % 100 == 0)
            {
                if (progressChanged(calculatePercentage(totalBytes, totalBytesRead)))
                {
                    throw new OperationCanceledException();
                }
            }
        }
        while (isMoreToRead);
    }

}

