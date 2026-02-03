using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Background loop vô hạn với parallax
/// </summary>
public class InfiniteBackground : MonoBehaviour
{
    [Header("=== CAMERA ===")]
    [SerializeField] private Camera mainCamera;

    [Header("=== PARALLAX ===")]
    [SerializeField] private float parallaxSpeed = 0.5f;

    [Header("=== INFINITE SCROLL ===")]
    [SerializeField] [Range(0.3f, 0.8f)] private float spawnThreshold = 0.5f;
    [SerializeField] private bool removeOldBackgrounds = true;

    [Header("=== BACKGROUNDS ===")]
    [SerializeField] private List<Sprite> backgroundSprites = new List<Sprite>();
    [SerializeField] private List<Vector3> backgroundScales = new List<Vector3>();
    [SerializeField] private int sortingOrder = -1;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showGizmos = false;

    private List<GameObject> backgrounds = new List<GameObject>();
    private Vector3 lastCameraPosition;
    private int currentBackgroundIndex = 0;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (backgroundSprites.Count == 0)
        {
            enabled = false;
            return;
        }

        if (backgroundSprites.Count != backgroundScales.Count)
        {
            enabled = false;
            return;
        }

        CreateInitialBackground();
        lastCameraPosition = mainCamera.transform.position;
    }

    void CreateInitialBackground()
    {
        SpriteRenderer existingRenderer = GetComponent<SpriteRenderer>();
        
        if (existingRenderer == null)
        {
            existingRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        existingRenderer.sprite = backgroundSprites[0];
        existingRenderer.sortingOrder = sortingOrder;
        transform.localScale = backgroundScales[0];

        backgrounds.Add(gameObject);
        gameObject.name = "Background_0";

        currentBackgroundIndex = 1 % backgroundSprites.Count;
    }

    void LateUpdate()
    {
        MoveBackgroundWithParallax();
        CheckAndSpawnNewBackground();

        if (removeOldBackgrounds)
        {
            RemoveOldBackgrounds();
        }

        lastCameraPosition = mainCamera.transform.position;
    }

    void MoveBackgroundWithParallax()
    {
        float cameraDeltaY = mainCamera.transform.position.y - lastCameraPosition.y;

        foreach (GameObject bg in backgrounds)
        {
            if (bg != null)
            {
                Vector3 pos = bg.transform.position;
                pos.y += cameraDeltaY * parallaxSpeed;
                bg.transform.position = pos;
            }
        }
    }

    void CheckAndSpawnNewBackground()
    {
        if (backgrounds.Count == 0) return;

        GameObject topBackground = backgrounds[backgrounds.Count - 1];
        if (topBackground == null) return;

        SpriteRenderer topSpriteRenderer = topBackground.GetComponent<SpriteRenderer>();
        if (topSpriteRenderer == null) return;

        float topBgHeight = topSpriteRenderer.bounds.size.y;
        float topBgTopEdge = topBackground.transform.position.y + (topBgHeight / 2f);
        float cameraTopEdge = mainCamera.transform.position.y + (mainCamera.orthographicSize);

        float distanceToTop = topBgTopEdge - cameraTopEdge;
        float thresholdDistance = topBgHeight * spawnThreshold;

        if (distanceToTop < thresholdDistance)
        {
            SpawnNewBackground(topBackground, topBgHeight);
        }
    }

    void SpawnNewBackground(GameObject previousBackground, float previousHeight)
    {
        if (backgroundSprites.Count == 0) return;

        Sprite nextSprite = backgroundSprites[currentBackgroundIndex];
        Vector3 nextScale = backgroundScales[currentBackgroundIndex];

        float newY = previousBackground.transform.position.y + previousHeight;

        Vector3 newPosition = new Vector3(
            previousBackground.transform.position.x, 
            newY, 
            previousBackground.transform.position.z
        );

        GameObject newBackground = new GameObject($"Background_{backgrounds.Count}");
        newBackground.transform.position = newPosition;

        SpriteRenderer newSpriteRenderer = newBackground.AddComponent<SpriteRenderer>();
        newSpriteRenderer.sprite = nextSprite;
        newSpriteRenderer.sortingOrder = sortingOrder;

        newBackground.transform.localScale = nextScale;

        backgrounds.Add(newBackground);

        currentBackgroundIndex = (currentBackgroundIndex + 1) % backgroundSprites.Count;
    }

    void RemoveOldBackgrounds()
    {
        if (backgrounds.Count <= 2) return;

        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;

        GameObject oldestBackground = backgrounds[0];
        if (oldestBackground == null)
        {
            backgrounds.RemoveAt(0);
            return;
        }

        SpriteRenderer sr = oldestBackground.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            backgrounds.RemoveAt(0);
            Destroy(oldestBackground);
            return;
        }

        float bgHeight = sr.bounds.size.y;
        float bgBottomEdge = oldestBackground.transform.position.y - (bgHeight / 2f);

        if (bgBottomEdge > cameraBottomEdge + bgHeight)
        {
            backgrounds.RemoveAt(0);
            Destroy(oldestBackground);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        foreach (GameObject bg in backgrounds)
        {
            if (bg != null)
            {
                SpriteRenderer sr = bg.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Gizmos.DrawWireCube(bg.transform.position, sr.bounds.size);
                }
            }
        }

        if (backgrounds.Count > 0 && backgrounds[backgrounds.Count - 1] != null)
        {
            GameObject topBg = backgrounds[backgrounds.Count - 1];
            SpriteRenderer sr = topBg.GetComponent<SpriteRenderer>();
            
            if (sr != null)
            {
                float bgHeight = sr.bounds.size.y;
                float spawnLineY = topBg.transform.position.y + (bgHeight / 2f) - (bgHeight * spawnThreshold);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    new Vector3(-100, spawnLineY, 0),
                    new Vector3(100, spawnLineY, 0)
                );
            }
        }
    }

    public void SetParallaxSpeed(float speed) => parallaxSpeed = Mathf.Clamp01(speed);
    public void SetSpawnThreshold(float threshold) => spawnThreshold = Mathf.Clamp(threshold, 0.3f, 0.8f);

    public void ResetBackgrounds()
    {
        for (int i = backgrounds.Count - 1; i > 0; i--)
        {
            if (backgrounds[i] != null && backgrounds[i] != gameObject)
            {
                Destroy(backgrounds[i]);
            }
        }

        backgrounds.Clear();
        CreateInitialBackground();
    }

    public void AddBackgroundSprite(Sprite sprite, Vector3 scale)
    {
        backgroundSprites.Add(sprite);
        backgroundScales.Add(scale);
    }

    public int GetCurrentBackgroundIndex() => currentBackgroundIndex;
    public int GetTotalBackgrounds() => backgrounds.Count;
}