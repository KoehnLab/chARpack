using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Canvas mainCanvas;

    private static SpawnManager _singleton;
    public static SpawnManager Singleton
    {
        get
        {
            if (_singleton == null)
            {
                _singleton = FindObjectOfType<SpawnManager>();
                if (_singleton == null)
                {
                    GameObject singleton = new GameObject(typeof(SpawnManager).Name);
                    _singleton = singleton.AddComponent<SpawnManager>();
                }
            }
            return _singleton;
        }
    }

    void Awake()
    {
        if (_singleton == null)
        {
            _singleton = this;
        }
        else if (_singleton != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public Vector2 GetSpawnLocalPosition(Transform spawnRectTransform)
{
    // Get all RectTransforms under the same parent canvas
    RectTransform[] allRectTransforms = mainCanvas.GetComponentsInChildren<RectTransform>();

    // Exclude the spawnRectTransform from the list
    allRectTransforms = allRectTransforms.Where(val => val != spawnRectTransform).ToArray();
    allRectTransforms = allRectTransforms.Where(val => val.transform.parent == spawnRectTransform.parent).ToArray();

    // Set the initial spawn position to (0, 0) in local coordinates
    spawnRectTransform.localPosition = Vector2.zero;

    Vector2 spawnPosition = spawnRectTransform.localPosition;

    // Convert spawn position to world space
    Vector3 spawnWorldPosition = spawnRectTransform.TransformPoint(spawnPosition);

    // Adjust the spawn position to the left until it doesn't overlap
    while (IsOverlap(spawnRectTransform, spawnWorldPosition, allRectTransforms))
    {
        spawnPosition.x -= 1f; // Adjust in local coordinates
        spawnWorldPosition = spawnRectTransform.TransformPoint(spawnPosition); // Convert to world space
    }

    return spawnPosition;
}


    // Check if the spawn position overlaps with any existing objects
    private bool IsOverlap(Transform spawnRectTransform, Vector3 spawnPosition, RectTransform[] allRectTransforms)
{
    // Convert spawn position to local space of the Canvas
    Vector2 localSpawnPosition = mainCanvas.transform.InverseTransformPoint(spawnPosition);

    // Calculate the bounds of the spawned rectangle in local space
    Bounds spawnBounds = new Bounds(localSpawnPosition, spawnRectTransform.GetComponent<RectTransform>().sizeDelta);

    // Check if any part of the spawned rectangle would overlap with any part of another rectangle
    foreach (RectTransform rectTransform in allRectTransforms)
    {
        // Calculate the bounds of the existing rectangle in local space
        Bounds existingBounds = new Bounds(mainCanvas.transform.InverseTransformPoint(rectTransform.position), rectTransform.sizeDelta);

        // Check if the bounds overlap
        if (BoundsOverlap(spawnBounds, existingBounds))
        {
            Debug.Log("Overlap detected.");
            return true; // Overlaps
        }
    }
    return false; // No overlap
}

    // Helper method to check if two bounds overlap
    private bool BoundsOverlap(Bounds bounds1, Bounds bounds2)
    {
    Debug.Log($"Bounds1: Min = {bounds1.min}, Max = {bounds1.max}");
    Debug.Log($"Bounds2: Min = {bounds2.min}, Max = {bounds2.max}");
    Debug.Log(bounds1.Contains(new Vector2 (bounds2.min.x,bounds2.min.y)) || 
           bounds1.Contains(new Vector2 (bounds2.min.x,bounds2.max.y)) ||
           bounds1.Contains(new Vector2 (bounds2.max.x,bounds2.min.y)) || 
           bounds1.Contains(new Vector2 (bounds2.max.x,bounds2.max.y)) ||
           bounds1.Equals(bounds2));

    return bounds1.Contains(new Vector2 (bounds2.min.x,bounds2.min.y)) || 
           bounds1.Contains(new Vector2 (bounds2.min.x,bounds2.max.y)) ||
           bounds1.Contains(new Vector2 (bounds2.max.x,bounds2.min.y)) || 
           bounds1.Contains(new Vector2 (bounds2.max.x,bounds2.max.y)) ||
           bounds1.Equals(bounds2);
    }

}
