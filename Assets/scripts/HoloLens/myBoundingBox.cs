using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class myBoundingBox : MonoBehaviour
{

    public Material myLineMaterial;
    private LineRenderer myLR;
    private Bounds localBounds;

    private void getBounds()
    {
        //localBounds = myLR.bounds;
        //localBounds = new Bounds(this.transform.position, Vector3.zero);
        var renderers = GetComponentsInChildren<Renderer>();
        bool first = true;
        foreach (var render in renderers)
        {
            if (render != myLR)
            {
                if (first)
                {
                    localBounds = render.bounds;
                    first = false;
                } else
                {
                    localBounds.Encapsulate(render.bounds);
                }
            }
        }
    }

    private void renderCube()
    {
        // note: line is drawn continously
        // render 12 edges of a cube

        myLR.SetPosition(0, localBounds.min); // LBF
        myLR.SetPosition(1, new Vector3(localBounds.max.x, localBounds.min.y, localBounds.min.z));
        myLR.SetPosition(2, new Vector3(localBounds.max.x, localBounds.max.y, localBounds.min.z));
        myLR.SetPosition(3, new Vector3(localBounds.min.x, localBounds.max.y, localBounds.min.z));
        myLR.SetPosition(4, localBounds.min);
        myLR.SetPosition(5, new Vector3(localBounds.min.x, localBounds.min.y, localBounds.max.z));
        myLR.SetPosition(6, new Vector3(localBounds.max.x, localBounds.min.y, localBounds.max.z));
        myLR.SetPosition(7, localBounds.max);
        myLR.SetPosition(8, new Vector3(localBounds.min.x, localBounds.max.y, localBounds.max.z));
        myLR.SetPosition(9, new Vector3(localBounds.min.x, localBounds.min.y, localBounds.max.z));
        myLR.SetPosition(10, new Vector3(localBounds.min.x, localBounds.max.y, localBounds.max.z));
        myLR.SetPosition(11, new Vector3(localBounds.min.x, localBounds.max.y, localBounds.min.z));
        myLR.SetPosition(12, new Vector3(localBounds.max.x, localBounds.max.y, localBounds.min.z));
        myLR.SetPosition(13, localBounds.max);
        myLR.SetPosition(14, new Vector3(localBounds.max.x, localBounds.min.y, localBounds.max.z));
        myLR.SetPosition(15, new Vector3(localBounds.max.x, localBounds.min.y, localBounds.min.z));

    }

    //private void bakeLineToMesh()
    //{
    //    var meshFilter = gameObject.AddComponent<MeshFilter>();
    //    Mesh mesh = new Mesh();
    //    myLR.BakeMesh(mesh);
    //    meshFilter.sharedMesh = mesh;

    //    var meshRenderer = gameObject.AddComponent<MeshRenderer>();
    //    meshRenderer.sharedMaterial = myLineMaterial;

    //    Destroy(myLR);
    //}

    public void Start()
    {
        
        myLR = gameObject.AddComponent<LineRenderer>();
        myLR.sharedMaterial = myLineMaterial;
        //myLR.useWorldSpace = false;
        myLR.startWidth = 0.001f;
        myLR.endWidth = 0.001f;
        myLR.positionCount = 16;


    }

    public void Update()
    {
        getBounds();
        renderCube();
    }

}
