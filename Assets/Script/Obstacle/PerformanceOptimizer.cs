using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tối ưu hiệu suất bằng cách tắt/bật components dựa trên khoảng cách
/// Giảm lag và tăng FPS
/// </summary>
public class PerformanceOptimizer : MonoBehaviour
{
    [Header("=== CULLING SETTINGS ===")]
    [Tooltip("Khoảng cách tắt renderer (tính từ camera)")]
    [SerializeField] private float cullingDistance = 25f;
    
    [Tooltip("Khoảng cách tắt physics (tính từ camera)")]
    [SerializeField] private float physicsDistance = 30f;
    
    [Tooltip("Khoảng cách tắt scripts (tính từ camera)")]
    [SerializeField] private float scriptDistance = 35f;
    
    [Header("=== UPDATE FREQUENCY ===")]
    [Tooltip("Số frame giữa mỗi lần check (càng cao càng tối ưu, nhưng kém mượt)")]
    [SerializeField] private int checkInterval = 5;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool enableCulling = true;
    
    // Tracking variables
    private int frameCounter = 0;
    private int culledRenderers = 0;
    private int culledColliders = 0;
    private int culledScripts = 0;
    
    // Cache
    private List<SpriteRenderer> allRenderers = new List<SpriteRenderer>();
    private List<Collider2D> allColliders = new List<Collider2D>();
    private List<MonoBehaviour> allScripts = new List<MonoBehaviour>();
    
    private Dictionary<SpriteRenderer, bool> rendererStates = new Dictionary<SpriteRenderer, bool>();
    private Dictionary<Collider2D, bool> colliderStates = new Dictionary<Collider2D, bool>();
    
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (player == null)
        {
            GhostController ghost = FindAnyObjectByType<GhostController>();
            if (ghost != null)
                player = ghost.transform;
        }
        
        CacheAllComponents();
    }
    
    void CacheAllComponents()
    {
        // Cache tất cả SpriteRenderers
        allRenderers.Clear();
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in renderers)
        {
            // Bỏ qua player và UI
            if (renderer.CompareTag("Player") || renderer.gameObject.layer == LayerMask.NameToLayer("UI"))
                continue;
            
            allRenderers.Add(renderer);
            rendererStates[renderer] = renderer.enabled;
        }
        
        // Cache tất cả Collider2Ds
        allColliders.Clear();
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Player"))
                continue;
            
            allColliders.Add(collider);
            colliderStates[collider] = collider.enabled;
        }
        
        // Cache các scripts có thể tắt được
        allScripts.Clear();
        ObstacleMovement[] movements = FindObjectsByType<ObstacleMovement>(FindObjectsSortMode.None);
        foreach (var movement in movements)
        {
            allScripts.Add(movement);
        }
        
        CircleRotation[] rotations = FindObjectsByType<CircleRotation>(FindObjectsSortMode.None);
        foreach (var rotation in rotations)
        {
            allScripts.Add(rotation);
        }
        
        Debug.Log($"[Optimizer] Cached {allRenderers.Count} renderers, {allColliders.Count} colliders, {allScripts.Count} scripts");
    }
    
    void Update()
    {
        if (!enableCulling || player == null || mainCamera == null)
            return;
        
        frameCounter++;
        
        // Chỉ check mỗi checkInterval frames
        if (frameCounter >= checkInterval)
        {
            frameCounter = 0;
            PerformCulling();
        }
    }
    
    void PerformCulling()
    {
        Vector3 cameraPos = mainCamera.transform.position;
        float cameraHeight = mainCamera.orthographicSize * 2f;
        
        culledRenderers = 0;
        culledColliders = 0;
        culledScripts = 0;
        
        // Cull renderers
        foreach (var renderer in allRenderers)
        {
            if (renderer == null) continue;
            
            float distance = Mathf.Abs(renderer.transform.position.y - cameraPos.y);
            bool shouldBeVisible = distance <= cullingDistance;
            
            if (renderer.enabled != shouldBeVisible)
            {
                renderer.enabled = shouldBeVisible;
                if (!shouldBeVisible) culledRenderers++;
            }
        }
        
        // Cull physics
        foreach (var collider in allColliders)
        {
            if (collider == null) continue;
            
            float distance = Mathf.Abs(collider.transform.position.y - cameraPos.y);
            bool shouldBeActive = distance <= physicsDistance;
            
            if (collider.enabled != shouldBeActive)
            {
                collider.enabled = shouldBeActive;
                if (!shouldBeActive) culledColliders++;
            }
        }
        
        // Cull scripts
        foreach (var script in allScripts)
        {
            if (script == null) continue;
            
            float distance = Mathf.Abs(script.transform.position.y - cameraPos.y);
            bool shouldBeActive = distance <= scriptDistance;
            
            if (script.enabled != shouldBeActive)
            {
                script.enabled = shouldBeActive;
                if (!shouldBeActive) culledScripts++;
            }
        }
    }
    
    public void RefreshCache()
    {
        CacheAllComponents();
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.green;
        style.alignment = TextAnchor.UpperRight;
        style.fontStyle = FontStyle.Bold;
        
        int totalRenderers = allRenderers.Count;
        int totalColliders = allColliders.Count;
        int totalScripts = allScripts.Count;
        
        string info = $"=== PERFORMANCE OPTIMIZER ===\n";
        info += $"Renderers: {totalRenderers - culledRenderers}/{totalRenderers}\n";
        info += $"Colliders: {totalColliders - culledColliders}/{totalColliders}\n";
        info += $"Scripts: {totalScripts - culledScripts}/{totalScripts}\n";
        info += $"Check Interval: {checkInterval} frames\n";
        info += $"FPS: {(1f / Time.deltaTime):F0}";
        
        GUI.Label(new Rect(Screen.width - 350, 10, 340, 150), info, style);
    }
    
    void OnDrawGizmosSelected()
    {
        if (mainCamera == null || !Application.isPlaying) return;
        
        Vector3 camPos = mainCamera.transform.position;
        
        // Vẽ culling distances
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawLine(new Vector3(-20, camPos.y + cullingDistance, 0), 
                       new Vector3(20, camPos.y + cullingDistance, 0));
        Gizmos.DrawLine(new Vector3(-20, camPos.y - cullingDistance, 0), 
                       new Vector3(20, camPos.y - cullingDistance, 0));
        
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawLine(new Vector3(-20, camPos.y + physicsDistance, 0), 
                       new Vector3(20, camPos.y + physicsDistance, 0));
        Gizmos.DrawLine(new Vector3(-20, camPos.y - physicsDistance, 0), 
                       new Vector3(20, camPos.y - physicsDistance, 0));
        
        Gizmos.color = new Color(0, 0, 1, 0.3f);
        Gizmos.DrawLine(new Vector3(-20, camPos.y + scriptDistance, 0), 
                       new Vector3(20, camPos.y + scriptDistance, 0));
        Gizmos.DrawLine(new Vector3(-20, camPos.y - scriptDistance, 0), 
                       new Vector3(20, camPos.y - scriptDistance, 0));
    }
}