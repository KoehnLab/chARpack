using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace chARpack
{
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

        public Vector2 GetSpawnLocalPosition(RectTransform spawnRectTransform)
        {
            // Get all RectTransforms under the same parent canvas
            allRectTransforms = mainCanvas.GetComponentsInChildren<RectTransform>();
            // Exclude the spawnRectTransform from the list
            allRectTransforms = allRectTransforms.Where(val => val != spawnRectTransform).ToArray();
            allRectTransforms = allRectTransforms.Where(val => val.transform.parent == spawnRectTransform.parent).ToArray();

            //Debug.Log($"Transforms Count: {allRectTransforms.Length}");

            // Set the initial spawn position to the mouse position in world space
            Vector2 spawnPosition = Input.mousePosition;

            // Adjust the spawn position if it overlaps
            spawnPosition = FindPosition(spawnRectTransform, spawnPosition);

            return spawnPosition;
        }

        private Vector2 FindPosition(Transform spawnRectTransform, Vector2 spawnPosition)
        {
            var mainRect = mainCanvas.transform as RectTransform;
            var newRect = spawnRectTransform as RectTransform;

            // Define grid step size
            float gridStepSize = Mathf.Min(newRect.sizeDelta.x / 10, newRect.sizeDelta.y / 10);

            // BFS queue
            Queue<Vector2> positionsToCheck = new Queue<Vector2>();
            positionsToCheck.Enqueue(spawnPosition);

            // HashSet to track visited positions
            HashSet<Vector2> visitedPositions = new HashSet<Vector2> { spawnPosition };

            // Directions for BFS: right, up, left, down, and diagonals
            Vector2[] directions = {
            new Vector2(gridStepSize, 0), new Vector2(-gridStepSize, 0),
            new Vector2(0, gridStepSize), new Vector2(0, -gridStepSize),
            new Vector2(gridStepSize, gridStepSize), new Vector2(gridStepSize, -gridStepSize),
            new Vector2(-gridStepSize, gridStepSize), new Vector2(-gridStepSize, -gridStepSize),
            // Additional directions for more exhaustive search
            new Vector2(gridStepSize * 2, 0), new Vector2(-gridStepSize * 2, 0),
            new Vector2(0, gridStepSize * 2), new Vector2(0, -gridStepSize * 2),
            new Vector2(gridStepSize * 2, gridStepSize), new Vector2(-gridStepSize * 2, gridStepSize),
            new Vector2(gridStepSize * 2, -gridStepSize), new Vector2(-gridStepSize * 2, -gridStepSize),
            new Vector2(gridStepSize, gridStepSize * 2), new Vector2(-gridStepSize, gridStepSize * 2),
            new Vector2(gridStepSize, -gridStepSize * 2), new Vector2(-gridStepSize, -gridStepSize * 2),
            new Vector2(gridStepSize * 2, gridStepSize * 2), new Vector2(gridStepSize * 2, -gridStepSize * 2),
            new Vector2(-gridStepSize * 2, gridStepSize * 2), new Vector2(-gridStepSize * 2, -gridStepSize * 2)
        };

            while (positionsToCheck.Count > 0)
            {
                var current = positionsToCheck.Dequeue();

                if (IsPositionWithinCanvas(current, newRect.sizeDelta, mainRect) && !IsOverlap(newRect, current))
                {
                    return current;
                }

                foreach (var direction in directions)
                {
                    Vector2 newPos = current + direction;

                    if (!visitedPositions.Contains(newPos) && IsPositionWithinCanvas(newPos, newRect.sizeDelta, mainRect))
                    {
                        positionsToCheck.Enqueue(newPos);
                        visitedPositions.Add(newPos);
                    }
                }
            }

            // Fallback to the center of the canvas if no suitable position is found
            Vector2 canvasCenter = mainCanvas.transform.TransformPoint(mainRect.rect.center);
            return canvasCenter;
        }

        private bool IsPositionWithinCanvas(Vector2 position, Vector2 size, RectTransform canvasRect)
        {
            Vector2 worldTopRight = mainCanvas.transform.TransformPoint(canvasRect.rect.max);
            Vector2 worldBottomLeft = mainCanvas.transform.TransformPoint(canvasRect.rect.min);

            return position.x + size.x / 2 <= worldTopRight.x &&
                   position.y + size.y / 2 <= worldTopRight.y &&
                   position.x - size.x / 2 >= worldBottomLeft.x &&
                   position.y - size.y / 2 >= worldBottomLeft.y;
        }

        private bool IsOverlap(RectTransform spawnRectTransform, Vector2 spawnPosition)
        {
            Vector2 size = spawnRectTransform.sizeDelta;
            Bounds spawnBounds = new Bounds(spawnPosition, size);

            foreach (RectTransform rectTransform in allRectTransforms)
            {
                Vector2 existingWorldPosition = rectTransform.TransformPoint(Vector2.zero);
                Bounds existingBounds = new Bounds(existingWorldPosition, rectTransform.sizeDelta);

                if (BoundsOverlap(spawnBounds, existingBounds))
                {
                    return true; // Overlaps
                }
            }
            return false; // No overlap
        }

        private bool BoundsOverlap(Bounds bounds1, Bounds bounds2)
        {
            bounds1.size = bounds1.size * 0.75f;
            bounds2.size = bounds2.size * 0.75f;
            return bounds1.min.x < bounds2.max.x && bounds1.max.x > bounds2.min.x &&
                   bounds1.min.y < bounds2.max.y && bounds1.max.y > bounds2.min.y;
        }
    }
}
