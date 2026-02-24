using UnityEngine;
using System.Collections.Generic;

public class InfiniteBackground : MonoBehaviour
{
    [Header("=== CAMERA ===")]
    [SerializeField] private Camera mainCamera;

    [Header("=== PARALLAX ===")]
    [SerializeField] private float parallaxSpeed = 0.5f;

    [Header("=== INFINITE SCROLL ===")]
    [SerializeField][Range(0.3f, 0.8f)] private float spawnThreshold = 0.5f;
    [SerializeField] private bool removeOldBackgrounds = true;

    [Header("=== BACKGROUNDS ===")]
    [SerializeField] private List<Sprite> backgroundSprites = new List<Sprite>();
    [SerializeField] private int sortingOrder = -1;

    [Header("=== FIT TO SCREEN ===")]
    [SerializeField] private float widthMultiplier = 1f;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showGizmos = false;

    private List<GameObject> backgrounds = new List<GameObject>();
    private Vector3 lastCameraPosition;
    private int currentBackgroundIndex = 0;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (backgroundSprites.Count == 0) { enabled = false; return; }

        // Reset scale về 1 trước khi tính
        transform.localScale = Vector3.one;

        CreateInitialBackground();
        lastCameraPosition = mainCamera.transform.position;
    }

    Vector3 CalcFitScale(Sprite sprite)
    {
        if (sprite == null || mainCamera == null) return Vector3.one;

        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth  = camHeight * mainCamera.aspect * widthMultiplier;

        float spriteNativeWidth  = sprite.texture.width  / sprite.pixelsPerUnit;
        float spriteNativeHeight = sprite.texture.height / sprite.pixelsPerUnit;

        float scaleX = camWidth  / spriteNativeWidth;
        float scaleY = camHeight / spriteNativeHeight;

        // Dùng Max để cover toàn màn hình (không bị hở)
        float scale = Mathf.Max(scaleX, scaleY);

        return new Vector3(scale, scale, 1f);
    }

    void CreateInitialBackground()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        Sprite spr = backgroundSprites[0];
        sr.sprite = spr;
        sr.sortingOrder = sortingOrder;

        transform.localScale = CalcFitScale(spr);

        // Sau khi có scale, tính height thật
        float bgHeight = sr.bounds.size.y;

        // Đặt background sao cho bottom edge = camera bottom edge
        float camBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
        transform.position = new Vector3(
            mainCamera.transform.position.x,
            camBottom + bgHeight / 2f,   // << căn đáy background = đáy camera
            transform.position.z
        );

        backgrounds.Add(gameObject);
        gameObject.name = "Background_0";
        currentBackgroundIndex = 1 % backgroundSprites.Count;
    }

    void LateUpdate()
    {
        MoveBackgroundWithParallax();
        CheckAndSpawnNewBackground();
        if (removeOldBackgrounds) RemoveOldBackgrounds();
        lastCameraPosition = mainCamera.transform.position;
    }

    void MoveBackgroundWithParallax()
    {
        float dy = mainCamera.transform.position.y - lastCameraPosition.y;

        foreach (GameObject bg in backgrounds)
        {
            if (bg != null)
            {
                Vector3 p = bg.transform.position;
                p.y += dy * parallaxSpeed;
                p.x = mainCamera.transform.position.x; // << luôn căn giữa theo camera X
                bg.transform.position = p;
            }
        }
    }

    void CheckAndSpawnNewBackground()
    {
        if (backgrounds.Count == 0) return;
        GameObject topBg = backgrounds[backgrounds.Count - 1];
        if (topBg == null) return;

        SpriteRenderer sr = topBg.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        float h = sr.bounds.size.y;
        float topEdge = topBg.transform.position.y + h / 2f;
        float camTop = mainCamera.transform.position.y + mainCamera.orthographicSize;

        if (topEdge - camTop < h * spawnThreshold)
            SpawnNewBackground(topBg, h);
    }

    void SpawnNewBackground(GameObject prevBg, float prevHeight)
    {
        Sprite nextSprite = backgroundSprites[currentBackgroundIndex];

        GameObject newBg = new GameObject($"Background_{backgrounds.Count}");
        newBg.transform.SetParent(null); // không có parent, tránh bị ảnh hưởng scale

        newBg.transform.position = new Vector3(
            prevBg.transform.position.x,
            prevBg.transform.position.y + prevHeight,
            prevBg.transform.position.z
        );

        SpriteRenderer sr = newBg.AddComponent<SpriteRenderer>();
        sr.sprite = nextSprite;
        sr.sortingOrder = sortingOrder;
        newBg.transform.localScale = CalcFitScale(nextSprite);

        backgrounds.Add(newBg);
        currentBackgroundIndex = (currentBackgroundIndex + 1) % backgroundSprites.Count;
    }

    void RemoveOldBackgrounds()
    {
        if (backgrounds.Count <= 2) return;

        GameObject oldest = backgrounds[0];
        if (oldest == null) { backgrounds.RemoveAt(0); return; }

        SpriteRenderer sr = oldest.GetComponent<SpriteRenderer>();
        if (sr == null) { backgrounds.RemoveAt(0); Destroy(oldest); return; }

        float h = sr.bounds.size.y;
        float botEdge = oldest.transform.position.y - h / 2f;
        float camBot = mainCamera.transform.position.y - mainCamera.orthographicSize;

        if (botEdge > camBot + h)
        {
            backgrounds.RemoveAt(0);
            if (oldest != gameObject) Destroy(oldest);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;
        Gizmos.color = Color.yellow;
        foreach (GameObject bg in backgrounds)
        {
            if (bg == null) continue;
            SpriteRenderer sr = bg.GetComponent<SpriteRenderer>();
            if (sr != null) Gizmos.DrawWireCube(bg.transform.position, sr.bounds.size);
        }
    }

    public void SetParallaxSpeed(float speed) => parallaxSpeed = Mathf.Clamp01(speed);
    public void SetSpawnThreshold(float t) => spawnThreshold = Mathf.Clamp(t, 0.3f, 0.8f);
    public int GetCurrentBackgroundIndex() => currentBackgroundIndex;
    public int GetTotalBackgrounds() => backgrounds.Count;

    public void ResetBackgrounds()
    {
        for (int i = backgrounds.Count - 1; i > 0; i--)
            if (backgrounds[i] != null && backgrounds[i] != gameObject)
                Destroy(backgrounds[i]);
        backgrounds.Clear();
        CreateInitialBackground();
    }
}