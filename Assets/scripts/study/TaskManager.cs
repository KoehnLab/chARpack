using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

public class TaskManager : MonoBehaviour
{

    private static TaskManager _singleton;

    public static TaskManager Singleton
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
                Debug.Log($"[{nameof(TaskManager)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    private void Awake()
    {
        Singleton = this;
    }


    List<Tuple<string,Transform>> objects = new List<Tuple<string, Transform>>();
    List<string> object_paths = new List<string>();

    public void generateObjects()
    {
        UnityEngine.Random.InitState(SettingsData.randomSeed);

        if (objects == null)
        {
            objects = new List<Tuple<string, Transform>>();
        }
        objects.Clear();


        Debug.Log($"[TaskManager] Number of detected objects {object_paths.Count}");

        float longest_edge = 0;

        // instantiate 16 objects 
        for (int i = 0; i < 16; i++)
        {
            var rnd_id = UnityEngine.Random.Range(0, object_paths.Count);
            var path = object_paths[rnd_id];

            if (Path.GetExtension(path) == ".fbx")
            {
                var instance = GenericObject.create(path);
                longest_edge = Mathf.Max(longest_edge, instance.GetComponent<myBoundingBox>().localBounds.size.maxDimValue());
                objects.Add(new Tuple<string, Transform>(path,instance.transform));
            }
            else
            {
                Debug.Log($"[TaskManager] Molecule path {path}");
                var instance_list = GlobalCtrl.Singleton.LoadMolecule(path, true);
                longest_edge = Mathf.Max(longest_edge, instance_list[0].GetComponent<myBoundingBox>().localBounds.size.maxDimValue());
                objects.Add(new Tuple<string, Transform>(path, instance_list[0].transform));
            }
            object_paths.RemoveAt(rnd_id);
        }

        var forward = (objects[0].Item2.transform.position - GlobalCtrl.Singleton.currentCamera.transform.position).normalized;
        var up = GlobalCtrl.Singleton.currentCamera.transform.up;
        var right = Vector3.Cross(forward, up).normalized;

        var start_pos = objects[0].Item2.transform.position + 1.5f * longest_edge * up - 1.5f * longest_edge * right;
        var start_dist = (start_pos - objects[0].Item2.transform.position).magnitude;
        start_pos += start_dist * forward;

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                var obj = objects[j + 4 * i].Item2;
                obj.position = start_pos + j * longest_edge * right;
                Debug.Log($"[TaskManager] obj pos {obj.position}");
            }
            start_pos -= longest_edge * up;
        }
        highlightRandomObject();
    }


    public void highlightRandomObject()
    {
        UnityEngine.Random.InitState(SettingsData.randomSeed);

        var rnd_id = UnityEngine.Random.Range(0, objects.Count);
        var rnd_object = objects[rnd_id];

        var object_to_track = Guid.Empty;
        var mol = rnd_object.Item2.GetComponent<Molecule>();
        if (mol != null)
        {
            mol.setServerFocus(true);
            object_to_track = mol.m_id;
        }
        else
        {
            var go = rnd_object.Item2.GetComponent<GenericObject>();
            go.setServerFocus(true);
            object_to_track = go.id;
        }
        EventManager.Singleton.SpawnGhostObject(rnd_object.Item1);
        if (object_to_track != Guid.Empty)
        {
            EventManager.Singleton.ObjectToTrack(object_to_track);
        }
        else
        {
            Debug.LogError("[TaskManager] Error while getting object to track Guid.");
        }

    }

    Transform ghostObject = null;
    public void spawnGhostObject(string path)
    {
        UnityEngine.Random.InitState(SettingsData.randomSeed);

        foreach (var obj_path in object_paths)
        {
            if (obj_path.Contains(path))
            {
                if (Path.GetExtension(path) == ".fbx")
                {
                    var instance = GenericObject.create(path);
                    instance.setIntractable(false);
                    instance.setOpacity(0.5f);
                    ghostObject = instance.transform;
                }
                else
                {
                    var instance_list = GlobalCtrl.Singleton.LoadMolecule(path, true);
                    instance_list[0].setIntractable(false);
                    instance_list[0].setOpacity(0.5f);
                    ghostObject = instance_list[0].transform;
                }
            }
        }

        ghostObject.rotation = Quaternion.Euler(getRandomRotation());

        if (NetworkManagerServer.Singleton != null)
        {
            var rnd_rot_x = UnityEngine.Random.Range(0,20f);
            var rnd_rot_y = UnityEngine.Random.Range(0, 20f);

            var cam_to_spawn = GlobalCtrl.Singleton.getCurrentSpawnPos() - GlobalCtrl.Singleton.currentCamera.transform.position;
            ghostObject.position = GlobalCtrl.Singleton.currentCamera.transform.position + Quaternion.Euler(rnd_rot_x, rnd_rot_y, 0f) * cam_to_spawn;
        }

        if (NetworkManagerClient.Singleton != null)
        {
            var cam_to_screen = screenAlignment.Singleton.getScreenCenter() - GlobalCtrl.Singleton.currentCamera.transform.position;
            var rnd_angle = UnityEngine.Random.Range(45f, 90f);
            if (SettingsData.handedness == Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left)
            {
                rnd_angle = UnityEngine.Random.Range(-45f, -90f);
            }

            var rnd_dist = UnityEngine.Random.Range(0.5f * cam_to_screen.magnitude, cam_to_screen.magnitude);
            ghostObject.position = GlobalCtrl.Singleton.currentCamera.transform.position + rnd_dist * (Quaternion.Euler(0f, rnd_angle, 0f) * cam_to_screen);
        }
    }

    private Vector3 getRandomRotation()
    {
        UnityEngine.Random.InitState(SettingsData.randomSeed);

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
                Debug.Log($"[TaskManager] resources: {fName}; extension: {Path.GetExtension(fName)}");
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

    }


    void Update()
    {
        
    }
}
