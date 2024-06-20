using System;
using System.IO;
using UnityEngine;
using Python.Runtime;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Net.Http;
using System.Security.Policy;
using System.Collections;


public class PythonEnvironmentManager : MonoBehaviour
{
    private static PythonEnvironmentManager _singleton;

    public static PythonEnvironmentManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(PythonEnvironmentManager)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    private void Awake()
    {
        Singleton = this;
    }

    bool isInitialized = false;

#if UNITY_STANDALONE || UNITY_EDITOR

    string base_path;
    string python_env_path;
    private static readonly HttpClient client = new HttpClient();
    Thread thread;
    void Start()
    {
        base_path = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        python_env_path = Path.Combine(base_path, "PythonEnv");
        thread = new Thread(() =>
        {
            if (!Directory.Exists(python_env_path))
            {
                Debug.Log("[PythonEnvironmentManager] No PythonEnv found. Starting download...");
                downloadEnvironment();
            }
        });
        thread.Start();
        StartCoroutine(waitForEnvironmentPrep());
    }

    IEnumerator waitForEnvironmentPrep()
    {   
        if (thread != null)
        {
            while (thread.IsAlive)
            {
                yield return null;
            }
            initEnvironment();
        }
    }

    private void initEnvironment()
    {
        string[] possibleDllNames = new string[]
        {
        "python313.dll",
        "python312.dll",
        "python311.dll",
        "python310.dll",
        "python39.dll",
        "python38.dll",
        "python37.dll",
        "python36.dll",
        "python35.dll",
        };

        var path = python_env_path;
        string pythonHome = null;
        string pythonExe = null;
        foreach (var p in path.Split(';'))
        {
            var fullPath = Path.Combine(p, "python.exe");
            if (File.Exists(fullPath))
            {
                pythonHome = Path.GetDirectoryName(fullPath);
                pythonExe = fullPath;
                break;
            }
        }

        string dll_path = null;
        string zip_path = null;
        if (pythonHome != null)
        {
            // check for the dll
            foreach (var dllName in possibleDllNames)
            {
                var fullPath = Path.Combine(pythonHome, dllName);
                if (File.Exists(fullPath))
                {
                    dll_path = fullPath;
                    zip_path = fullPath.Split('.')[0] + ".zip";
                    break;
                }
            }
            if (dll_path == null)
            {
                throw new Exception("[PythonEnvironmentManager] Couldn't find python DLL");
            }
        }
        else
        {
            throw new Exception("[PythonEnvironmentManager] Couldn't find python.exe");
        }

        //// Set the path to the embedded Python environment
        var pythonPath = pythonHome + ";" + Path.Combine(pythonHome, "Lib\\site-packages") + ";" + zip_path + ";" + Path.Combine(pythonHome, "DLLs") + ";" + Path.Combine(Application.streamingAssetsPath, "PythonScripts") + ";" + Path.Combine(Application.streamingAssetsPath, "md");
        Environment.SetEnvironmentVariable("PYTHONHOME", null);
        Environment.SetEnvironmentVariable("PYTHONPATH", null);

        // Display Python runtime details
        Debug.Log($"Python DLL: {dll_path}");
        Debug.Log($"Python zip: {zip_path}");
        Debug.Log($"Python executable: {pythonExe}");
        Debug.Log($"Python home: {pythonHome}");
        Debug.Log($"Python path: {pythonPath}");


        // Initialize the Python runtime
        Runtime.PythonDLL = dll_path;

        // Initialize the Python engine with the embedded Python environment
        PythonEngine.PythonHome = pythonHome;
        PythonEngine.PythonPath = pythonPath;
        
        PythonEngine.Initialize();
        isInitialized = true;

        Debug.Log("[PythonEnvironmentManager] Python environment initialized.");
    }

    // UnityWebRequest only works in main thread
    //private void downloadEnvironment()
    //{
    //    string url = "https://cloud.visus.uni-stuttgart.de/index.php/s/avET4hM9eoUCx6i/download";
    //    using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
    //    {
    //        Debug.Log("[PythonEnvironmentManager] Placing web request.");
    //        // Request and wait for the desired page.
    //        webRequest.SendWebRequest();
    //        while (webRequest.result == UnityWebRequest.Result.InProgress);

    //        switch (webRequest.result)
    //        {
    //            case UnityWebRequest.Result.ConnectionError:
    //            case UnityWebRequest.Result.DataProcessingError:
    //                Debug.LogError($"[PythonEnvironmentManager] Error: {webRequest.error}");
    //                break;
    //            case UnityWebRequest.Result.ProtocolError:
    //                Debug.LogError($"[PythonEnvironmentManager] HTTP Error: {webRequest.error}");
    //                break;
    //            case UnityWebRequest.Result.Success:
    //                Debug.Log($"[PythonEnvironmentManager] Download successful.\nReceived: {webRequest.downloadHandler.text}");
    //                break;
    //        }

    //        if (webRequest.result == UnityWebRequest.Result.Success)
    //        {
    //            string download_path = base_path + "/PythonEnv.zip";
    //            Debug.Log("[PythonEnvironmentManager] Writing downloaded content to disc.");
    //            WriteAsync(download_path, webRequest.downloadHandler.data);

    //            string extract_path = base_path + "/PythonEnv/";
    //            Debug.Log("[PythonEnvironmentManager] Extracting zip.");
    //            Task.Run(() => ZipFile.ExtractToDirectory(download_path, extract_path));

    //            Debug.Log("[PythonEnvironmentManager] Python environment ready.");
    //        }
    //    }
    //}

    async void downloadEnvironment()
    {
        string url = "https://cloud.visus.uni-stuttgart.de/index.php/s/avET4hM9eoUCx6i/download";
        // Send a GET request to the specified URL
        HttpResponseMessage response = await client.GetAsync(url);
        string download_path = base_path + "/PythonEnv.zip";
        // Check if the response is successful (status code 200-299)
        if (response.IsSuccessStatusCode)
        {
            // Get the file content as a stream
            using (Stream fileStream = await response.Content.ReadAsStreamAsync())
            {
                // Save the stream to the specified file

                using (FileStream outputFileStream = new FileStream(download_path, FileMode.Create))
                {
                    await fileStream.CopyToAsync(outputFileStream);
                }

                Debug.Log("[PythonEnvironmentManager] PythonEnvironment downloaded successfully.");
            }
        }
        else
        {
            Debug.LogError($"[PythonEnvironmentManager] Failed to download PythonEnvironment.\nStatus code: {response.StatusCode}");
        }

        string extract_path = base_path + "/PythonEnv/";
        Debug.Log("[PythonEnvironmentManager] Extracting zip.");

        ZipFile.ExtractToDirectory(download_path, extract_path);

        Debug.Log("[PythonEnvironmentManager] Python environment ready.");
    }

    //async Task WriteAsync(string filePath, byte[] data)
    //{
    //    using (FileStream sourceStream = new FileStream(filePath,
    //        FileMode.Append, FileAccess.Write, FileShare.None,
    //        bufferSize: 4096, useAsync: true))
    //    {
    //        await sourceStream.WriteAsync(data, 0, data.Length);
    //    };
    //}

    private void OnDestroy()
    {
        // Shutdown the Python engine
        PythonEngine.Shutdown();
    }

#endif
}
