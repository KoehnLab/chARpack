using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnManager : MonoBehaviour
{
    public Canvas mainCanvas;
    public float stepSize = 100f;
    public int maxIterations = 100;
    // Start is called before the first frame update

    private static SpawnManager _singleton;
    public static SpawnManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(SpawnManager)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    private void Awake()
    {
        Singleton = this;
    }


     public Vector2 GetSpawnLocalPosition(RectTransform spawnRectTransform)
    {
        Vector2 canvasCenter = mainCanvas.pixelRect.size / 2f;
        Vector2 spawnPosition = Vector2.zero;

        for (int i = 0; i < maxIterations; i++)
        {
            spawnPosition = canvasCenter;

            Collider2D[] colliders = Physics2D.OverlapCircleAll(mainCanvas.transform.TransformPoint(spawnPosition), 1f);
            bool overlap = false;
            foreach (Collider2D collider in colliders)
            {
                overlap = true;
                break;
            }

            if (!overlap)
            {
                return spawnPosition;
            }

            // Spiral outwards
            Vector2 offset = SpiralOut(i);
            canvasCenter += offset * stepSize;
        }

        // If entire canvas is cluttered, return random position
        return new Vector2(0,0);
    }

    Vector2 SpiralOut(int step)
    {
        float x = 0;
        float y = 0;
        float dx = 0;
        float dy = -1;
        float t = Mathf.Max(mainCanvas.pixelRect.width, mainCanvas.pixelRect.height);
        float maxI = t * t;

        for (int i = 0; i < maxI; i++)
        {
            if ((-mainCanvas.pixelRect.width / 2 <= x) && (x <= mainCanvas.pixelRect.width / 2) && (-mainCanvas.pixelRect.height / 2 <= y) && (y <= mainCanvas.pixelRect.height / 2))
            {
                if (i == step)
                {
                    return new Vector2(x, y);
                }
            }

            if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y)))
            {
                t = dx;
                dx = -dy;
                dy = t;
            }

            x += dx;
            y += dy;
        }

        return Vector2.zero;
    }
}
