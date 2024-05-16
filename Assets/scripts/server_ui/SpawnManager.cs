using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Canvas mainCanvas;
    private RectTransform[] allRectTransforms;

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
    
    allRectTransforms = mainCanvas.GetComponentsInChildren<RectTransform>();
    // Exclude the spawnRectTransform from the list
    allRectTransforms = allRectTransforms.Where(val => val != spawnRectTransform).ToArray();
    allRectTransforms = allRectTransforms.Where(val => val.transform.parent == spawnRectTransform.parent).ToArray();
    //allRectTransforms = allRectTransforms.Where(val => val.gameObject.GetComponent<ServerAtomTooltip>()!=null).ToArray();

    Debug.Log($"Transforms Count: {allRectTransforms.Length}");

    // Set the initial spawn position to (0, 0) in local coordinates
    // spawnRectTransform.localPosition = Vector2.zero;
    var mainRect = mainCanvas.transform as RectTransform;
    var offset = 0.05f * mainRect.sizeDelta;
    spawnRectTransform.localPosition =  mainCanvas.transform.InverseTransformPoint(Input.mousePosition) + new Vector3(offset.x, offset.y, 0f);

    Vector2 spawnPosition = spawnRectTransform.position;
    var spawnRect = spawnRectTransform.transform as RectTransform;

    // Adjust the spawn position to the left until it doesn't overlap
    spawnPosition = FindPosition(spawnRectTransform,spawnPosition);

    return spawnPosition;
}

    private Vector2 FindPosition(Transform spawnRectTransform, Vector2 spawnPosition)
{
    var mainRect = mainCanvas.transform as RectTransform;
    var newRect = spawnRectTransform.transform as RectTransform;
    Bounds spawnBounds = new Bounds(spawnPosition, spawnRectTransform.GetComponent<RectTransform>().sizeDelta);
    List<Vector2> positions = new List<Vector2>();
    positions.Add(spawnPosition);

    // Check if initial position is within canvas and there's no overlap
    if (IsPositionWithinCanvas(spawnPosition, newRect.sizeDelta, mainRect) && !IsOverlap(spawnRectTransform, spawnPosition))
    {
        return spawnPosition;
    }
    else
    {
        // Check surrounding positions
        float xOffset = newRect.sizeDelta.x / 2;
        float yOffset = newRect.sizeDelta.y / 2;

        for (float x = -xOffset; x <= xOffset; x++)
        {
            for (float y = -yOffset; y <= yOffset; y++)
            {
                Vector2 newPos = new Vector2(spawnPosition.x + x, spawnPosition.y + y);

                // Check if surrounding position is within canvas and there's no overlap
                if (IsPositionWithinCanvas(newPos, newRect.sizeDelta, mainRect) && !IsOverlap(spawnRectTransform, newPos))
                {
                    return newPos; // Found a suitable position
                }
            }
        }
    }

    return new Vector2(0, 0); // Default return if no suitable position found
}

// Check if a position is within the canvas
private bool IsPositionWithinCanvas(Vector2 position, Vector2 size, RectTransform canvasRect)
{
    return position.x + size.x / 2 <= canvasRect.sizeDelta.x &&
           position.y + size.y / 2 <= canvasRect.sizeDelta.y &&
           position.x - size.x / 2 >= 0 &&
           position.y - size.y / 2 >= 0;
}

    // Check if the spawn position overlaps with any existing objects
    private bool IsOverlap(Transform spawnRectTransform, Vector2 spawnPosition)
{
    // Calculate the bounds of the spawned rectangle in local space
    Bounds spawnBounds = new Bounds(spawnPosition, spawnRectTransform.GetComponent<RectTransform>().sizeDelta);

    // Check if any part of the spawned rectangle would overlap with any part of another rectangle
    foreach (RectTransform rectTransform in allRectTransforms)
    {
        // Calculate the bounds of the existing rectangle in local space
        Bounds existingBounds = new Bounds(rectTransform.position, rectTransform.sizeDelta);

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
