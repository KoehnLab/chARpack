using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.IO;
using UnityEditor;

public class ModelImport : MonoBehaviour
{

    public NNModel importModel;

    // Start is called before the first frame update
    void Start()
    {
        //var file = openLoadFileDialog();
        //if (file != null) runModel(file);
        runModel(null);
    }

    void runModel(FileInfo file)
    {
        //var model = ModelLoader.Load(file.FullName);
        Debug.Log("[ModelImport:runModel] Performing load...");
        var model = ModelLoader.Load((NNModel)Resources.Load("ml_models/model"));

        foreach (var layer in model.layers)
            Debug.Log($"[ModelImport:runModel] {layer.name} does {layer.type}");

        var engine = WorkerFactory.CreateWorker(model, WorkerFactory.Device.CPU);

        Debug.Log($"[ModelImport:runModel] model: {model.ToString()}");
        Debug.Log($"[ModelImport:runModel] engine: {engine.ToString()}");

        Debug.Log("[ModelImport:runModel] Executing Model...");
        var input = new Tensor(1, 1, 1, 10);
        var output = engine.Execute(input).PeekOutput();

        Debug.Log($"[ModelImport:runModel] Output: {output}");
    }

    FileInfo openLoadFileDialog()
    {
#if !WINDOWS_UWP
        var path = EditorUtility.OpenFilePanel("Open ONNX Model File", "", "");
        if (path.Length == 0) return null;
        FileInfo fi = new FileInfo(path);

        // do checks on file
        if (!fi.Exists)
        {
            Debug.LogError("[ModelImport] Something went wrong during path conversion. Abort.");
            return null;
        }
        return fi;
#endif
    }

}
