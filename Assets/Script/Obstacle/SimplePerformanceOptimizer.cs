using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple Performance Optimizer - Tắt/bật objects dựa vào khoảng cách camera
/// </summary>
public class SimplePerformanceOptimizer : MonoBehaviour
{
    [Header("=== CULLING DISTANCES ===")]
    [Tooltip("Khoảng cách tắt objects (từ camera)")]
    [SerializeField] private float cullingDistance = 30f;
    
    [Tooltip("Khoảng cách bật lại objects")]
    [SerializeField] private float enableDistance = 25f;
    
    [Header("=== PERFORMANCE ===")]
    [Tooltip("Số frames giữa mỗi lần check (càng cao càng nhẹ, nhưng kém mượt hơn)")]
    [SerializeField] private int checkEveryNFrames = 3;
    
    [Header("=== OPTIONS ===")]
    [Tooltip("Có tắt Renderer không (Sprite Renderer)")]
    [SerializeField] private bool cullRenderers = true;
    
    [Tooltip("Có tắt Collider không")]
    [SerializeField] private bool cullColliders = true;
    
    [Tooltip("Có tắt Scripts không (ObstacleMovement, CircleRotation...)")]
    [SerializeField] private bool cullScripts = false; // Tắt mặc định vì có thể gây lỗi
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool enableOptimization = true;
    
    // ========== PRIVATE VARIABLES ==========
    
    private class OptimizableObject
    {
        public GameObject gameObject;
        public Transform transform;
        public SpriteRenderer[] renderers;
        public Collider2D[] colliders;
        public MonoBehaviour[] scripts;
        public bool isEnabled = true;
        
        public OptimizableObject(GameObject obj)
        {
            gameObject = obj;
            transform = obj.transform;
            
            // Cache components
            renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            colliders = obj.GetComponentsInChildren<Collider2D>();
            
            // Cache scripts có thể tắt
            List<MonoBehaviour> scriptList = new List<MonoBehaviour>();
            
            // ObstacleMovement
            ObstacleMovement[] movements = obj.GetComponentsInChildren<ObstacleMovement>();
            scriptList.AddRange(movements);
            
            // CircleRotation
            CircleRotation[] rotations = obj.GetComponentsInChildren<CircleRotation>();
            scriptList.AddRange(rotations);
            
            scripts = scriptList.ToArray();
        }
        
        public void SetEnabled(bool enabled, bool doRenderers, bool doColliders, bool doScripts)
        {
            if (isEnabled == enabled) return;
            
            isEnabled = enabled;
            
            if (doRenderers)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                        renderer.enabled = enabled;
                }
            }
            
            if (doColliders)
            {
                foreach (var collider in colliders)
                {
                    if (collider != null)
                        collider.enabled = enabled;
                }
            }
            
            if (doScripts)
            {
                foreach (var script in scripts)
                {
                    if (script != null)
                        script.enabled = enabled;
                }
            }
        }
    }
    
    private List<OptimizableObject> allObjects = new List<OptimizableObject>();
    private int frameCounter = 0;
    private int culledCount = 0;
    private float screenHeight;
    
    // ========== UNITY LIFECYCLE ==========
    
    void Start()
    {
        InitializeReferences();
        CacheAllObjects();
    }
    
    void Update()
    {
        if (!enableOptimization || mainCamera == null) return;
        
        frameCounter++;
        if (frameCounter >= checkEveryNFrames)
        {
            frameCounter = 0;
            PerformCulling();
        }
    }
    
    // ========== INITIALIZATION ==========
    
    void InitializeReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (player == null)
        {
            GhostController ghost = FindAnyObjectByType<GhostController>();
            if (ghost != null)
                player = ghost.transform;
        }
        
        if (mainCamera != null)
            screenHeight = mainCamera.orthographicSize * 2f;
    }
    
    void CacheAllObjects()
    {
        allObjects.Clear();
        
        GameObject obstacleManager = GameObject.Find("ObstacleManager");
        if (obstacleManager == null)
        {
            Debug.LogWarning("[SimpleOptimizer] Không tìm thấy ObstacleManager!");
            return;
        }
        
        foreach (Transform child in obstacleManager.transform)
        {
            if (child == null) continue;
            
            string name = child.name.ToLower();
            
            if (name.Contains("fire") || name.Contains("circle") || name.Contains("flying"))
            {
                OptimizableObject obj = new OptimizableObject(child.gameObject);
                allObjects.Add(obj);
            }
        }
        
        Debug.Log($"[SimpleOptimizer] Cached {allObjects.Count} objects");
    }
    
    // ========== CULLING LOGIC ==========
    
    void PerformCulling()
    {
        if (mainCamera == null)
        {
            InitializeReferences();
            return;
        }
        
        Vector3 cameraPos = mainCamera.transform.position;
        culledCount = 0;
        
        foreach (var obj in allObjects)
        {
            if (obj == null || obj.gameObject == null) continue;
            
            float distance = Mathf.Abs(obj.transform.position.y - cameraPos.y);
            
            bool shouldBeEnabled;
            if (obj.isEnabled)
            {
                shouldBeEnabled = distance <= cullingDistance;
            }
            else
            {
                shouldBeEnabled = distance <= enableDistance;
            }
            
            obj.SetEnabled(shouldBeEnabled, cullRenderers, cullColliders, cullScripts);
            
            if (!shouldBeEnabled)
                culledCount++;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (mainCamera == null || !Application.isPlaying) return;
        
        Vector3 camPos = mainCamera.transform.position;
        
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawLine(new Vector3(-20, camPos.y + cullingDistance, 0),
                       new Vector3(20, camPos.y + cullingDistance, 0));
        Gizmos.DrawLine(new Vector3(-20, camPos.y - cullingDistance, 0),
                       new Vector3(20, camPos.y - cullingDistance, 0));
        
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawLine(new Vector3(-20, camPos.y + enableDistance, 0),
                       new Vector3(20, camPos.y + enableDistance, 0));
        Gizmos.DrawLine(new Vector3(-20, camPos.y - enableDistance, 0),
                       new Vector3(20, camPos.y - enableDistance, 0));
    }
    
    public void RefreshCache()
    {
        CacheAllObjects();
        Debug.Log("[SimpleOptimizer] Cache refreshed!");
    }

    public void EnableAll()
    {
        foreach (var obj in allObjects)
        {
            obj.SetEnabled(true, cullRenderers, cullColliders, cullScripts);
        }
        Debug.Log("[SimpleOptimizer] All objects enabled!");
    }
}