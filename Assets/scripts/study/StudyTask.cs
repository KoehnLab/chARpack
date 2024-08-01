using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using UnityEngine;


public class StudyTask
{

    public enum objectSpawnEnvironment
    {
        DESKTOP = 0,
        IMMERSIVE = 1
    }


    public string description;
    public TransitionManager.InteractionType? interactionType = null;
    public objectSpawnEnvironment? objectSpawn = null;
    public bool? withAnimation = null;

    // Button
    public TransitionManager.DesktopTarget? B_desktopTarget = null;
    public TransitionManager.ImmersiveTarget? B_immersiveTarget = null;

    // Distant Grab
    public TransitionManager.DesktopTarget? DG_desktopTarget = null;
    public TransitionManager.ImmersiveTarget? DG_immersiveTarget = null;
    public bool? DG_holdGrab = null;


    public StudyTask(StudyTask input)
    {
        description = input.description;
        interactionType = input.interactionType;
        objectSpawn = input.objectSpawn;
        withAnimation = input.withAnimation;
        B_desktopTarget = input.B_desktopTarget;
        B_immersiveTarget = input.B_immersiveTarget;
        DG_desktopTarget = input.DG_desktopTarget;
        DG_immersiveTarget = input.DG_immersiveTarget;
        DG_holdGrab = input.DG_holdGrab;
    }

    public StudyTask(){}

    public static List<StudyTask> getAllTasks()
    {
        var tasks = new List<StudyTask>();

        // Task0.1g
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.DISTANT_GRAB;
        tasks.Last().DG_holdGrab = true;
        tasks.Last().DG_desktopTarget = TransitionManager.DesktopTarget.HOVER;
        tasks.Last().DG_immersiveTarget = TransitionManager.ImmersiveTarget.HAND_FOLLOW;

        // Task0.2p
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.DISTANT_GRAB;
        tasks.Last().DG_holdGrab = false;
        tasks.Last().DG_desktopTarget = TransitionManager.DesktopTarget.HOVER;
        tasks.Last().DG_immersiveTarget = TransitionManager.ImmersiveTarget.HAND_FIXED;

        // Task0.3p
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.DISTANT_GRAB;
        tasks.Last().DG_holdGrab = false;
        tasks.Last().DG_desktopTarget = TransitionManager.DesktopTarget.CENTER_OF_SCREEN;
        tasks.Last().DG_immersiveTarget = TransitionManager.ImmersiveTarget.HAND_FIXED;

        // Task0.4p
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.DISTANT_GRAB;
        tasks.Last().DG_holdGrab = false;
        tasks.Last().DG_desktopTarget = TransitionManager.DesktopTarget.CURSOR_POSITION;
        tasks.Last().DG_immersiveTarget = TransitionManager.ImmersiveTarget.HAND_FIXED;

        // Task0.5p
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.DISTANT_GRAB;
        tasks.Last().DG_holdGrab = false;
        tasks.Last().DG_desktopTarget = TransitionManager.DesktopTarget.CENTER_OF_SCREEN;
        tasks.Last().DG_immersiveTarget = TransitionManager.ImmersiveTarget.HAND_FOLLOW;

        // Task0.6p
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.DISTANT_GRAB;
        tasks.Last().DG_holdGrab = false;
        tasks.Last().DG_desktopTarget = TransitionManager.DesktopTarget.CENTER_OF_SCREEN;
        tasks.Last().DG_immersiveTarget = TransitionManager.ImmersiveTarget.FRONT_OF_SCREEN;

        // Task0.7p
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.DISTANT_GRAB;
        tasks.Last().DG_holdGrab = false;
        tasks.Last().DG_desktopTarget = TransitionManager.DesktopTarget.CENTER_OF_SCREEN;
        tasks.Last().DG_immersiveTarget = TransitionManager.ImmersiveTarget.CAMERA;

        // Task1.1
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.CLOSE_GRAB;

        // Task2.1
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.BUTTON_PRESS;
        tasks.Last().B_desktopTarget= TransitionManager.DesktopTarget.CENTER_OF_SCREEN;
        tasks.Last().B_immersiveTarget = TransitionManager.ImmersiveTarget.FRONT_OF_SCREEN;

        // Task2.2
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.BUTTON_PRESS;
        tasks.Last().B_desktopTarget = TransitionManager.DesktopTarget.CURSOR_POSITION;
        tasks.Last().B_immersiveTarget = TransitionManager.ImmersiveTarget.FRONT_OF_SCREEN;

        // Task2.3
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.BUTTON_PRESS;
        tasks.Last().B_desktopTarget = TransitionManager.DesktopTarget.CENTER_OF_SCREEN;
        tasks.Last().B_immersiveTarget = TransitionManager.ImmersiveTarget.CAMERA;

        // Task2.4
        tasks.Add(new StudyTask());
        tasks.Last().interactionType = TransitionManager.InteractionType.BUTTON_PRESS;
        tasks.Last().B_desktopTarget = TransitionManager.DesktopTarget.CURSOR_POSITION;
        tasks.Last().B_immersiveTarget = TransitionManager.ImmersiveTarget.CAMERA;

        var count = tasks.Count;
        for (int i = 0; i < count; i++)
        {
            var task = new StudyTask(tasks[i]);
            tasks[i].withAnimation = true;
            task.withAnimation = false;
            tasks.Add(task);
        }
        assignSpawnEnvironment(tasks);
        foreach(var task in tasks)
        {
            generateDescription(task);
        }
        tasks.Shuffle();

        return tasks;
    }

    public static void generateDescription(StudyTask task)
    {
        if (task == null)
        {
            Debug.LogError("[StudyTask] Cannot generate description for null task.");
            return;
        }

        task.description = "";
        foreach (var thisVar in task.GetType().GetFields())
        {
            if (thisVar.GetValue(task) != null && thisVar.Name != "description")
            {
                var value = Convert.ChangeType(thisVar.GetValue(task), Nullable.GetUnderlyingType(thisVar.FieldType));
                task.description += $"{thisVar.Name} = {value}\n";
            }
        }
    }

    public static void assignSpawnEnvironment(List<StudyTask> tasks)
    {
        List<int> index_list = Enumerable.Range(0, tasks.Count).ToList();
        index_list.Shuffle();

        // Take the first half
        var first_half = index_list.Take(index_list.Count() / 2).ToList();
        // Take the last half
        var last_half = index_list.Skip(index_list.Count() / 2).ToList();

        Debug.Log($"[assignSpawnEnvironment] first_half.Count {first_half.Count}, last_half.Count {last_half.Count}, tasks.Count {tasks.Count}");
        for (int i = 0; i< first_half.Count; i++)
        {
            tasks[first_half[i]].objectSpawn = objectSpawnEnvironment.DESKTOP;
            tasks[last_half[i]].objectSpawn = objectSpawnEnvironment.IMMERSIVE;
        }
    }

    public static void writeTasksToFile(List<StudyTask> tasks, string path)
    {
        var tasksJson = JsonConvert.SerializeObject(tasks);
        tasksJson = JValue.Parse(tasksJson).ToString(Formatting.Indented); // make it pretty
        File.WriteAllText(path, tasksJson);
    }

    public static List<StudyTask> readTasksFromFile(string path)
    {
        StreamReader reader = new StreamReader(path);
        var file_content = reader.ReadToEnd();
        reader.Close();

        var tasks = JsonConvert.DeserializeObject<List<StudyTask>>(file_content);

        return tasks;
    }

    public static void activateTaskSettings(StudyTask task)
    {
        if (task != null)
        {
            if (task.withAnimation != null)
            {
                if (task.withAnimation.Value)
                {
                    SettingsData.transitionMode = TransitionManager.TransitionMode.DESKTOP_2D;
                    SettingsData.transitionAnimation = TransitionManager.TransitionAnimation.BOTH;
                }
                else
                {
                    SettingsData.transitionMode = TransitionManager.TransitionMode.INSTANT;
                    SettingsData.transitionAnimation = TransitionManager.TransitionAnimation.NONE;
                }
            }
            if (task.B_desktopTarget != null)
            {
                SettingsData.desktopTarget = task.B_desktopTarget.Value;
            }
            if (task.B_immersiveTarget != null)
            {
                SettingsData.immersiveTarget = task.B_immersiveTarget.Value;
            }
            if (task.DG_desktopTarget != null)
            {
                SettingsData.desktopTarget = task.DG_desktopTarget.Value;
            }
            if (task.DG_immersiveTarget != null)
            {
                SettingsData.immersiveTarget = task.DG_immersiveTarget.Value;
            }
            if (task.DG_holdGrab != null)
            {
                SettingsData.requireGrabHold = task.DG_holdGrab.Value;
            }
        }
        settingsControl.Singleton.updateSettings();
        EventManager.Singleton.UpdateSettings();
    }

}
