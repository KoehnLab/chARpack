using SimpleFileBrowser;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace chARpack
{
    public class StudyLogger : MonoBehaviour
    {

        private static StudyLogger _singleton;

        public static StudyLogger Singleton
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
                    Debug.Log($"[{nameof(StudyLogger)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }

            }
        }

        private void Awake()
        {
            Singleton = this;
        }


        string currentLogFile = null;
        bool isResuming = false;
        int resumingTask = -1;
        public void setLogOutputFile(string logOutputFile)
        {
            currentLogFile = logOutputFile;
            if (File.Exists(currentLogFile))
            {
                StreamReader reader = new StreamReader(currentLogFile);
                var file_content = reader.ReadToEnd();
                reader.Close();

                if (file_content.Contains("Study finished"))
                {
                    Debug.LogWarning($"[setLogOutputFile] Trying to write over a finished study. Please use a new file.");
                    currentLogFile = null;
                    return;
                }

                isResuming = true;
                resumingTask = findLastCompletedTaskID() + 1;
                write("Study Resuming.");
            }
            else
            {
                isResuming = false;
                write("Study Started.", FileMode.Create);
            }
        }

        public bool getIsResuming()
        {
            return isResuming;
        }

        public int getResumingTaskID()
        {
            return resumingTask;
        }

        IEnumerator ShowSaveDialogCoroutine()
        {
            yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files);


            if (FileBrowser.Success)
            {
                if (FileBrowser.Result.Length != 1)
                {
                    Debug.LogError("[StudyTaskManager] Path from FileBrowser is empty. Abort.");
                    yield break;
                }
                FileInfo fi = new FileInfo(FileBrowser.Result[0]);
                setLogOutputFile(fi.FullName);
                StudyTaskManager.Singleton.startStudy();
            }
        }

        public string getLogFilePath()
        {
            if (currentLogFile == null)
            {
                Debug.LogError("[StudyLogger] No log file available. Study not initialized yet.");
                return "";
            }
            return currentLogFile;
        }

        public void startLogger()
        {
            StartCoroutine(ShowSaveDialogCoroutine());
        }

        public void write(string log, FileMode fm = FileMode.Append)
        {
            if (currentLogFile == null)
            {
                Debug.LogError("[StudyLogger] Cannot log anything. Study not initialized yet.");
                return;
            }

            using (FileStream stream = new FileStream(currentLogFile, fm))
            {
                var output = System.DateTime.Now.ToString("[yyyy-MM-dd_hh:mm:ss] ");
                output += log + "\n";

                byte[] bytes = Encoding.UTF8.GetBytes(output);
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public void write(string log, int task_id)
        {
            if (currentLogFile == null)
            {
                Debug.LogError("[StudyLogger] Cannot log anything. Study not initialized yet.");
                return;
            }

            using (FileStream stream = new FileStream(currentLogFile, FileMode.Append))
            {
                var output = System.DateTime.Now.ToString($"[yyyy-MM-dd_hh:mm:ss] (Task_{task_id}) ");
                output += log + "\n";

                byte[] bytes = Encoding.UTF8.GetBytes(output);
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private int findLastCompletedTaskID()
        {
            StreamReader reader = new StreamReader(currentLogFile);
            var file_content = reader.ReadToEnd();
            reader.Close();

            var split = file_content.Split("\n").ToList();
            var task_id = 0;
            if (file_content.Contains("k_"))
            {
                var last_task_line = split.Last(line => line.Contains("k_"));
                task_id = int.Parse(last_task_line.Split("k_")[1].Split(")")[0]);
                if (!last_task_line.Contains("finished"))
                {
                    task_id = task_id - 1;
                }
            }
            else
            {
                task_id = -1;
            }


            return task_id;
        }

    }
}
