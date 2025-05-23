using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace chARpack
{
    public class StudyTaskManager : MonoBehaviour
    {

        private static StudyTaskManager _singleton;

        public static StudyTaskManager Singleton
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
                    UnityEngine.Debug.Log($"[{nameof(StudyTaskManager)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }

            }
        }

        private void Awake()
        {
            Singleton = this;
        }


        List<Tuple<string, Transform>> objects = new List<Tuple<string, Transform>>();
        List<string> object_paths = new List<string>();
        List<string> settings_paths = new List<string>();

        public void generateObjects()
        {
            StartCoroutine(generateRoutine());
        }

        private IEnumerator generateRoutine()
        {
            objects.Clear();
            UnityEngine.Random.InitState(SettingsData.randomSeed + currentTaskID);

            //float longest_edge = 0;
            var longest_edge_list = new List<float>();
            var object_paths_copy = new List<string>(object_paths);

            // instantiate 16 objects 
            for (int i = 0; i < 16; i++)
            {
                yield return null;
                var rnd_id = UnityEngine.Random.Range(0, object_paths_copy.Count);
                var path = object_paths_copy[rnd_id];

                if (Path.GetExtension(path) == ".fbx")
                {
                    var instance = GenericObject.create(path);
                    instance.gameObject.SetActive(false);
                    //longest_edge = Mathf.Max(longest_edge, instance.GetComponent<myBoundingBox>().localBounds.size.maxDimValue());
                    longest_edge_list.Add(instance.GetComponent<myBoundingBox>().localBounds.size.maxDimValue());
                    objects.Add(new Tuple<string, Transform>(path, instance.transform));
                }
                else
                {
                    UnityEngine.Debug.Log($"[TaskManager] Molecule path {path}");
                    var instance_list = GlobalCtrl.Singleton.LoadMolecule(path, true);
                    instance_list[0].gameObject.SetActive(false);
                    //longest_edge = Mathf.Max(longest_edge, instance_list[0].GetComponent<myBoundingBox>().localBounds.size.maxDimValue());
                    longest_edge_list.Add(instance_list[0].GetComponent<myBoundingBox>().localBounds.size.maxDimValue());
                    objects.Add(new Tuple<string, Transform>(path, instance_list[0].transform));
                }
                object_paths_copy.RemoveAt(rnd_id);
            }

            //var spacing = 0.9f * longest_edge;
            var spacing = 0.9f * longest_edge_list.Max();
            var forward = (objects[0].Item2.transform.position - GlobalCtrl.Singleton.currentCamera.transform.position).normalized;
            var up = GlobalCtrl.Singleton.currentCamera.transform.up;
            var right = Vector3.Cross(forward, up).normalized;

            var start_pos = objects[0].Item2.transform.position + 1.5f * spacing * up - 1.5f * spacing * right;
            if (LoginData.isServer)
            {
                var start_dist = (start_pos - objects[0].Item2.transform.position).magnitude;
                start_pos += start_dist * forward;
            }
            else
            {
                var rotation = 90f;
                if (SettingsData.handedness == HandTracking.Handedness.Left)
                {
                    rotation = -rotation;
                }
#if CHARPACK_MRTK_2_8
                forward = -screenAlignment.Singleton.getScreenNormal();
                right = Quaternion.Euler(0f, rotation, 0f) * forward;
                var dist = Vector3.Distance(screenAlignment.Singleton.getScreenCenter(), GlobalCtrl.Singleton.currentCamera.transform.position);
                start_pos = screenAlignment.Singleton.getScreenCenter() + screenAlignment.Singleton.getScreenSizeWS().x * right - dist * forward;
                //var dist = (objects[0].Item2.transform.position - GlobalCtrl.Singleton.currentCamera.transform.position).magnitude;
                //start_pos = (GlobalCtrl.Singleton.currentCamera.transform.position - Mathf.Sign(rotation) * 1.5f * spacing * right) + dist * forward + 1.5f * spacing * up - 1.5f * spacing * right;
#endif

                // resetting parameters for grid generation
                forward = right;
                right = Quaternion.Euler(0f, rotation, 0f) * forward;
                spacing = 1.1f * longest_edge_list.Average();
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var obj = objects[j + 4 * i].Item2;
                    obj.position = start_pos + j * spacing * right;
                    obj.gameObject.SetActive(true);
                    UnityEngine.Debug.Log($"[TaskManager] obj pos {obj.position}");
                }
                start_pos -= spacing * up;
            }
            yield return null;
            highlightRandomObject();
            UnityEngine.Random.InitState(SettingsData.randomSeed);
        }


        public void highlightRandomObject()
        {
            var rnd_id = UnityEngine.Random.Range(0, objects.Count);
            var rnd_object = objects[rnd_id];

            var object_to_track = Guid.Empty;
            var mol = rnd_object.Item2.GetComponent<Molecule>();
            if (mol != null)
            {
                mol.setServerFocus(true, true);
                object_to_track = mol.m_id;
            }
            else
            {
                var go = rnd_object.Item2.GetComponent<GenericObject>();
                go.setServerFocus(true, true);
                object_to_track = go.id;
            }
            if (StudyLogger.Singleton != null)
            {
                StudyLogger.Singleton.write($"Object to transition: {rnd_object.Item2.name}", currentTaskID);
            }
            EventManager.Singleton.SpawnGhostObject(rnd_object.Item1);
            if (object_to_track != Guid.Empty)
            {
                EventManager.Singleton.ObjectToTrack(object_to_track);
            }
            else
            {
                UnityEngine.Debug.LogError("[TaskManager] Error while getting object to track Guid.");
            }

        }

        Transform ghostObject = null;
        public void spawnGhostObject(string path)
        {
            UnityEngine.Debug.Log($"[spawnGhostObject] path: {path}");
            if (object_paths.Contains(path))
            {
                if (Path.GetExtension(path) == ".fbx")
                {
                    var instance = GenericObject.create(path);
                    instance.setIntractable(false);
                    instance.setOpacity(0.5f);
                    var col = instance.GetComponent<BoxCollider>();
                    if (col != null) col.enabled = false;
                    foreach (Transform inst in instance.transform)
                    {
                        col = inst.GetComponent<BoxCollider>();
                        if (col != null) col.enabled = false;
                    }
                    ghostObject = instance.transform;
                }
                else
                {
                    var instance_list = GlobalCtrl.Singleton.LoadMolecule(path, true);
                    instance_list[0].setIntractable(false);
                    instance_list[0].setOpacity(0.5f);
                    var col = instance_list[0].GetComponent<BoxCollider>();
                    if (col != null) col.enabled = false;
                    foreach (Transform inst in instance_list[0].transform)
                    {
                        col = inst.GetComponent<BoxCollider>();
                        if (col != null) col.enabled = false;
                    }
                    ghostObject = instance_list[0].transform;
                }
            }
            else
            {
                UnityEngine.Debug.LogError("[spawnGhostObject] No match with an object in resources found. Abort.");
                return;
            }

            ghostObject.rotation = Quaternion.Euler(getRandomRotation());
            ghostObject.localScale = UnityEngine.Random.Range(0.8f * ghostObject.localScale.x, 1.2f * ghostObject.localScale.x) * Vector3.one;

            if (LoginData.isServer)
            {
                var rnd_rot_x = UnityEngine.Random.Range(0, 20f);
                var rnd_rot_y = UnityEngine.Random.Range(0, 20f);

                var cam_to_spawn = GlobalCtrl.Singleton.getCurrentSpawnPos() - GlobalCtrl.Singleton.currentCamera.transform.position;
                ghostObject.position = GlobalCtrl.Singleton.currentCamera.transform.position + Quaternion.Euler(rnd_rot_x, rnd_rot_y, 0f) * cam_to_spawn;
                StudyLogger.Singleton.write($"Object to transition: {ghostObject.name}", currentTaskID);
            }
            else
            {
#if CHARPACK_MRTK_2_8
                var cam_to_screen = screenAlignment.Singleton.getScreenCenter() - GlobalCtrl.Singleton.currentCamera.transform.position;
                var rnd_angle = UnityEngine.Random.Range(45f, 90f);
                if (SettingsData.handedness == HandTracking.Handedness.Left)
                {
                    rnd_angle = UnityEngine.Random.Range(-45f, -90f);
                }

                var rnd_dist = UnityEngine.Random.Range(0.5f * cam_to_screen.magnitude, cam_to_screen.magnitude);
                ghostObject.position = GlobalCtrl.Singleton.currentCamera.transform.position + rnd_dist * (Quaternion.Euler(0f, rnd_angle, 0f) * cam_to_screen);
#endif
            }
        }

        private Vector3 getRandomRotation()
        {
            float x = UnityEngine.Random.Range(0f, 360f);
            float y = UnityEngine.Random.Range(0f, 360f);
            float z = UnityEngine.Random.Range(0f, 360f);

            return new Vector3(x, y, z);
        }


        private Guid objectToTrack;
        public void setObjectToTrack(Guid id)
        {
            objectToTrack = id;
        }

        void Start()
        {
            UnityEngine.Random.InitState(SettingsData.randomSeed);
            descriptionPrefab = Resources.Load<GameObject>("prefabs/TaskDescription");
            grayOutPrefab = Resources.Load<GameObject>("prefabs/GrayOutPannel");
            // Check for fbx and xml assets in Resources using file info dump
            if (object_paths == null)
            {
                object_paths = new List<string>();
            }
            object_paths.Clear();

            //Load as TextAsset
            TextAsset fileNamesAsset = Resources.Load<TextAsset>("FileNames");
            //De-serialize it
            FileNameInfo fileInfoLoaded = JsonUtility.FromJson<FileNameInfo>(fileNamesAsset.text);

            foreach (string fName in fileInfoLoaded.fileNames)
            {
                if (fName.Contains("Resources/other"))
                {
                    //UnityEngine.Debug.Log($"[TaskManager] resources: {fName}; extension: {Path.GetExtension(fName)}");
                    if (Path.GetExtension(fName) == ".fbx")
                    {
                        object_paths.Add(Path.GetFileName(fName));
                    }
                    else if (Path.GetExtension(fName) == ".xml")
                    {
                        var reduced = fName.Split("Resources/")[1];
                        object_paths.Add(reduced);
                    }
                }
            }
            UnityEngine.Debug.Log($"[TaskManager] Number of detected objects {object_paths.Count}");

            // settings preparation
            //if (NetworkManagerServer.Singleton != null)
            //{
            //    // Check for available settings 
            //    if (settings_paths == null)
            //    {
            //        settings_paths = new List<string>();
            //    }
            //    settings_paths.Clear();
            //    //The Resources folder path
            //    string resourcsPath = Application.streamingAssetsPath + "/StudySettings";

            //    //Get file names except the ".meta" extension
            //    string[] fileNames = Directory.GetFiles(resourcsPath, "*.*", SearchOption.AllDirectories)
            //        .Where(x => Path.GetExtension(x) != ".meta").ToArray();

            //    foreach (var path in fileNames)
            //    {
            //        settings_paths.Add(path.Replace("\\", "/"));
            //    }
            //}
        }

        public void reportUnsuccessfullTransition(TransitionManager.InteractionType triggered_by)
        {
            if (isTaskInProgress)
            {
                StudyLogger.Singleton.write($"Unsuccessful transition triggered by {triggered_by}.", currentTaskID);
            }
        }


        bool isTaskInProgress = false;
        public void triggerNextTask()
        {
            if (isTaskInProgress)
            {
                UnityEngine.Debug.LogWarning("[StudyTaskManager] Task is still in progress. Finish current task first.");
                return;
            }

        }

        public void abortSession()
        {

        }

        public float? resultAngle = null;
        public float? resultDist = null;
        public void evaluateCorrectness()
        {
            var task = currentTaskSet[currentTaskID];
            if (task.objectSpawn == StudyTask.objectSpawnEnvironment.DESKTOP)
            {
                // Get data from Client
                EventManager.Singleton.RequestResults(currentTaskID);
            }
            else
            {
                var angle = getErrorAngle();
                var dist = getErrorDist();
                var scale = getErrorScale();
                StudyLogger.Singleton.write($"AngleError: {angle}", currentTaskID);
                StudyLogger.Singleton.write($"DistError: {dist}", currentTaskID);
                StudyLogger.Singleton.write($"ScaleError: {scale}", currentTaskID);
                // log finised task
                StudyLogger.Singleton.write($"finished.", currentTaskID);
            }
        }

        public float getErrorAngle()
        {
            if (ghostObject != null)
            {
                Quaternion rotA = ghostObject.transform.rotation;
                Quaternion rotB;
                if (GlobalCtrl.Singleton.List_curMolecules.ContainsKey(objectToTrack))
                {
                    rotB = GlobalCtrl.Singleton.List_curMolecules[objectToTrack].transform.rotation;
                }
                else if (GenericObject.objects.ContainsKey(objectToTrack))
                {
                    rotB = GenericObject.objects[objectToTrack].transform.rotation;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[getErrorAngle] Could not find object to track");
                    return -1f;
                }

                var angle = Quaternion.Angle(rotA, rotB);
                return angle;

            }
            return -1;
        }

        public float getErrorDist()
        {
            if (ghostObject != null)
            {
                var posA = ghostObject.transform.position;
                Vector3 posB;
                if (GlobalCtrl.Singleton.List_curMolecules.ContainsKey(objectToTrack))
                {
                    posB = GlobalCtrl.Singleton.List_curMolecules[objectToTrack].transform.position;
                }
                else if (GenericObject.objects.ContainsKey(objectToTrack))
                {
                    posB = GenericObject.objects[objectToTrack].transform.position;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[getErrorDist] Could not find object to track");
                    return -1f;
                }

                var dist = (posA - posB).magnitude;
                return dist;
            }
            return -1;
        }

        public float getErrorScale()
        {
            if (ghostObject != null)
            {
                var scaleA = ghostObject.transform.localScale.x;
                float scaleB;
                if (GlobalCtrl.Singleton.List_curMolecules.ContainsKey(objectToTrack))
                {
                    scaleB = GlobalCtrl.Singleton.List_curMolecules[objectToTrack].transform.localScale.x;
                }
                else if (GenericObject.objects.ContainsKey(objectToTrack))
                {
                    scaleB = GenericObject.objects[objectToTrack].transform.localScale.x;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[getErrorScale] Could not find object to track");
                    return 0f;
                }

                var scale = scaleA - scaleB;
                return scale;
            }
            return 0f;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        public void startAndFinishTask()
        {
            if (currentTaskID < currentTaskSet.Count)
            {
                if (isTaskInProgress) // finishing task
                {
                    isTaskInProgress = false;

                    // process and log data
                    stopwatch.Stop();
                    StudyLogger.Singleton.write($"Time: {stopwatch.ElapsedMilliseconds}", currentTaskID);
                    evaluateCorrectness();

                    // clear scene and reset objects 
                    ghostObject = null;
                    objectToTrack = Guid.Empty;

                    // prepare for next task
                    currentTaskID++;
                    if (currentTaskID == currentTaskSet.Count) // finish study
                    {
                        StudyLogger.Singleton.write($"Study finished.");
                        showDescriptionText("Study finished.");
                    }
                    else
                    {
                        activateTask(currentTaskSet[currentTaskID]);
                    }
                }
                else // starting task
                {
                    GlobalCtrl.Singleton.DeleteAllUI();
                    isTaskInProgress = true;
                    hideDescriptionText();
                    GlobalCtrl.Singleton.currentCamera.transform.position = Vector3.zero;
                    GlobalCtrl.Singleton.currentCamera.transform.rotation = Quaternion.identity;
                    stopwatch.Restart();

                    StudyLogger.Singleton.write($"Started.", currentTaskID);

                    var task = currentTaskSet[currentTaskID];
                    StudyTask.activateTaskSettings(task);
                    descriptionInstance.SetActive(true);
                    if (task.objectSpawn == StudyTask.objectSpawnEnvironment.DESKTOP)
                    {
                        generateObjects();
                    }
                    else
                    {
                        EventManager.Singleton.SpawnObjectCollection(currentTaskID);
                    }
                }
            }
        }

        public void restartTask()
        {
            if (currentTaskID < currentTaskSet.Count)
            {
                if (isTaskInProgress) // finishing task
                {
                    StudyLogger.Singleton.write($"Restart.", currentTaskID);

                    isTaskInProgress = false;

                    // clear scene and reset objects 
                    ghostObject = null;
                    objectToTrack = Guid.Empty;

                    activateTask(currentTaskSet[currentTaskID]);
                }
            }
        }

        GameObject descriptionPrefab;
        GameObject descriptionInstance = null;
        GameObject grayOutPrefab;
        GameObject grayOutInstance = null;

        private void showDescriptionText(string text)
        {
            if (descriptionInstance == null)
            {
                descriptionInstance = Instantiate(descriptionPrefab);
                descriptionInstance.transform.SetParent(UICanvas.Singleton.transform);
                var rect = descriptionInstance.transform as RectTransform;
                rect.anchoredPosition = Vector3.zero;
            }
            descriptionInstance.GetComponent<TMP_Text>().text = text;
            grayOutInstance = Instantiate(grayOutPrefab);
            grayOutInstance.transform.SetParent(UICanvas.Singleton.transform);
            var gray_rect = grayOutInstance.transform as RectTransform;
            gray_rect.anchorMin = Vector2.zero;
            gray_rect.anchorMax = Vector2.one;
            gray_rect.pivot = 0.5f * Vector2.one;
            gray_rect.sizeDelta = Vector2.zero;
            gray_rect.anchoredPosition = Vector3.zero;

            descriptionInstance.transform.SetAsLastSibling();
        }


        public void logTransitionGrab(Molecule mol, GenericObject go, TransitionManager.InteractionType trigger)
        {
            if (!isTaskInProgress) return;
            if (mol != null)
            {
                StudyLogger.Singleton.write($"TransitionGrab triggered by {trigger} successfull on Molecule {mol.gameObject.name}.", currentTaskID);
            }
            else if (go != null)
            {
                StudyLogger.Singleton.write($"TransitionGrab triggered by {trigger} successfull on GenericObject {go.gameObject.name}.", currentTaskID);
            }
            else
            {
                StudyLogger.Singleton.write($"Unsuccessfull TransitionGrab triggered by {trigger}.", currentTaskID);
            }
        }

        public void logTransition(string obj_name, TransitionManager.InteractionType trigger)
        {
            if (!isTaskInProgress) return;
            StudyLogger.Singleton.write($"Object {obj_name} transitoned by {trigger}.", currentTaskID);
        }

        private void hideDescriptionText()
        {
            //if (descriptionInstance != null)
            //{
            //    Destroy(descriptionInstance);
            //    descriptionInstance = null;
            //}
            if (grayOutInstance != null)
            {
                Destroy(grayOutInstance);
                grayOutInstance = null;
            }
        }

        public void activateTask(StudyTask task)
        {
            var split = task.description.Split("\n");
            var interaction = split.First(text => text.Contains("interactionType")) + "\n" + split.First(text => text.Contains("withAnimation"));
            showDescriptionText(interaction);
        }

        int currentTaskID = 0;
        List<StudyTask> currentTaskSet = new List<StudyTask>();
        public void startStudy()
        {
            var log_file_path = StudyLogger.Singleton.getLogFilePath();
            var settings_file_path = Path.Join(Path.GetDirectoryName(log_file_path), Path.GetFileNameWithoutExtension(log_file_path)) + "_settings.json";
            var task_list_file = Path.Join(Path.GetDirectoryName(log_file_path), Path.GetFileNameWithoutExtension(log_file_path)) + "_tasks.json";


            if (StudyLogger.Singleton.getIsResuming())
            {
                // reload settings
                if (File.Exists(settings_file_path))
                {
                    SettingsData.readSettingsFromJSON(settings_file_path);
#if CHARPACK_GUI
                    settingsControl.Singleton.updateSettings();
#endif
                }
                else
                {
                    UnityEngine.Debug.LogError("[startStudy] could not retreive settings from session to resume. Abort.");
                    return;
                }
                // reload tasks
                if (File.Exists(task_list_file))
                {
                    currentTaskSet = StudyTask.readTasksFromFile(task_list_file);
                }
                else
                {
                    UnityEngine.Debug.LogError("[startStudy] could not retreive tasks from session to resume. Abort.");
                    return;
                }
                currentTaskID = StudyLogger.Singleton.getResumingTaskID();
            }
            else
            {
                currentTaskID = 0;
                SettingsData.dumpSettingsToJSON(settings_file_path);
                currentTaskSet = StudyTask.getAllTasks();
                StudyTask.writeTasksToFile(currentTaskSet, task_list_file);
            }
            activateTask(currentTaskSet[currentTaskID]);
        }

        public void overrideTaskID(int task_id)
        {
            currentTaskID = task_id;
        }

    }
}
