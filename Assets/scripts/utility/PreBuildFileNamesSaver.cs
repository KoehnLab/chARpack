using System;
using UnityEngine;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor;



namespace chARpack
{
    [InitializeOnLoad]
    class PreBuildFileNamesSaver : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            createFileReference();
        }

        static PreBuildFileNamesSaver()
        {
            createFileReference();
        }

        static void createFileReference()
        {
            //The Resources folder path
            string resourcsPath = Application.dataPath + "/Resources";

            //Get file names except the ".meta" extension
            string[] fileNames = Directory.GetFiles(resourcsPath, "*.*", SearchOption.AllDirectories)
                .Where(x => Path.GetExtension(x) != ".meta").ToArray();

            for (int i = 0; i < fileNames.Length; i++)
            {
                fileNames[i] = fileNames[i].Replace("\\", "/");
            }

            //Convert the Names to Json to make it easier to access when reading it
            FileNameInfo fileInfo = new FileNameInfo(fileNames);
            string fileInfoJson = JsonUtility.ToJson(fileInfo, true);

            //Save the json to the Resources folder as "FileNames.txt"
            File.WriteAllText(Application.dataPath + "/Resources/FileNames.txt", fileInfoJson);

            AssetDatabase.Refresh();
        }
    }

#endif

    [Serializable]
    public class FileNameInfo
    {
        public string[] fileNames;

        public FileNameInfo(string[] fileNames)
        {
            fileNames = fileNames;
        }
    }
}