using System;
using System.IO;
using UnityEngine;
using Python.Runtime;
using System.IO.Compression;
using System.Threading;
using System.Net.Http;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;


namespace chARpack
{
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

        public bool isInitialized { get; private set; }


#if UNITY_STANDALONE || UNITY_EDITOR

        string base_path;
        string python_env_path;
        bool isInstalled = false;
        private static readonly HttpClient client = new HttpClient();
        Thread thread;
        IntPtr state;

        void Start()
        {
            var li_inst = LoadingIndicator.GetPythonInstance();
            if (li_inst != null)
            {
                li_inst.startLoading("Python", "Preparing ...");
            }
            isInitialized = false;
            base_path = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            python_env_path = Path.Combine(base_path, "PythonEnv");
            thread = new Thread(() =>
            {
                isInstalled = false;
                checkPythonInstallation();
            });
            thread.Start();
            StartCoroutine(waitForEnvironmentPrep());
        }

        async void checkPythonInstallation()
        {
            if (!File.Exists(python_env_path + ".zip"))
            {
                Debug.Log("[PythonEnvironmentManager] No PythonEnv.zip found. Starting download...");
                var li_inst = LoadingIndicator.GetPythonInstance();
                await downloadEnvironment(li_inst.downloadProgressChanged);
                await extractEvironment();
            }
            else if (!Directory.Exists(python_env_path))
            {
                Debug.Log("[PythonEnvironmentManager] PythonEnv.zip found. Starting extraction...");
                await extractEvironment();
            }
            else if (!File.Exists(Path.Combine(python_env_path, "python.exe")))
            {
                Debug.Log("[PythonEnvironmentManager] Extraction incomplete. Restarting extraction...");
                await extractEvironment();
            }
            isInstalled = true;
        }

        IEnumerator waitForEnvironmentPrep()
        {
            while (!isInstalled)
            {
                yield return new WaitForSeconds(1f);
            }
            thread.Join();
            initEnvironment();
            var li_inst = LoadingIndicator.GetPythonInstance();
            if (li_inst != null)
            {
                li_inst.loadingFinished(true, "Initialized.");
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
                        zip_path = python_env_path + ".zip";
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
            var pythonPath = pythonHome + ";" + 
                Path.Combine(pythonHome, "Lib") + ";" + 
                Path.Combine(pythonHome, "Lib\\site-packages") + ";" + 
                zip_path + ";" + 
                Path.Combine(pythonHome, "DLLs") + ";" + 
                Path.Combine(Application.streamingAssetsPath, "PythonScripts") + ";" + 
                Path.Combine(Application.streamingAssetsPath, "md");
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
            //Environment.CurrentDirectory = pythonHome;

            Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
            Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath);

            PythonEngine.Initialize();
            state = PythonEngine.BeginAllowThreads();
            isInitialized = true;

            Debug.Log("[PythonEnvironmentManager] Python environment initialized.");

            PythonDispatcher.Initialize();
        }

        async Task downloadEnvironment(Func<float?, bool> progressChanged)
        {
            string url = "https://cloud.visus.uni-stuttgart.de/index.php/s/UWVy9CVfQIcMqrO/download";
            // Send a GET request to the specified URL
            HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            string download_path = base_path + "/PythonEnv.zip";

            // test
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            static float? calculatePercentage(long? totalDownloadSize, long totalBytesRead) => 
                totalDownloadSize.HasValue ? (float)Math.Round((float)totalBytesRead / totalDownloadSize.Value * 100, 2) : null;
            using var fileStream = new FileStream(download_path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

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

        // old
        // Check if the response is successful (status code 200-299)
        //if (response.IsSuccessStatusCode)
        //{
        //    // Get the file content as a stream
        //    using (Stream fileStream = await response.Content.ReadAsStreamAsync())
        //    {
        //        // Save the stream to the specified file

        //        using (FileStream outputFileStream = new FileStream(download_path, FileMode.Create))
        //        {
        //            await fileStream.CopyToAsync(outputFileStream);
        //        }

        //        Debug.Log("[PythonEnvironmentManager] PythonEnvironment downloaded successfully.");
        //    }
        //}
        //else
        //{
        //    Debug.LogError($"[PythonEnvironmentManager] Failed to download PythonEnvironment.\nStatus code: {response.StatusCode}");
        //}
        }

        private async Task extractEvironment()
        {
            string destinationPath = base_path + "/PythonEnv/";
            string zipPath = base_path + "/PythonEnv.zip";

            int numTotalFiles = 0;
            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                numTotalFiles = archive.Entries.Count(x => !string.IsNullOrWhiteSpace(x.Name));
            }

            var li_inst = LoadingIndicator.GetPythonInstance();
            int extracted_files = 0;
            var checking_task = Task.Run(async () =>
            {
                while (extracted_files < numTotalFiles)
                {
                    if (Directory.Exists(destinationPath))
                    {
                        var dir_info = new DirectoryInfo(destinationPath);
                        var file_info = dir_info.GetFiles("*.*", SearchOption.AllDirectories);
                        extracted_files = file_info.Length;
                        float progress = 100f * extracted_files / numTotalFiles;
                        if (li_inst)
                        {
                            li_inst.extractProgressChanged(progress);
                        }
                    }
                    await Task.Delay(1000);
                }
            });

            var extract_task = Task.Run(() => { ZipFile.ExtractToDirectory(zipPath, destinationPath, true); });

            await Task.WhenAll(checking_task, extract_task);
        }


        private void OnDestroy()
        {
            PythonEngine.EndAllowThreads(state);
            // Shutdown the Python engine
            PythonEngine.Shutdown();
        }

#endif
    }
}
