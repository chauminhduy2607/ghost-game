using UnityEngine;

/// <summary>
/// Hệ thống 9 line responsive theo màn hình
/// </summary>
public class LineSystem : MonoBehaviour
{
    [Header("=== LINE SETTINGS ===")]
    [SerializeField] private int totalLines = 9;
    [SerializeField] private float marginPercent = 0.05f;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLines = true;
    [SerializeField] private Color lineColor = Color.yellow;
    
    private Camera mainCamera;
    private float[] linePositionsX;
    private float screenWidth;
    private float lineSpacing;
    
    public static LineSystem Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        mainCamera = Camera.main;
        CalculateLines();
    }
    
    void CalculateLines()
    {
        if (mainCamera == null) return;
        
        screenWidth = mainCamera.orthographicSize * 2f * mainCamera.aspect;
        float margin = screenWidth * marginPercent;
        float usableWidth = screenWidth - (margin * 2f);
        lineSpacing = usableWidth / (totalLines - 1);
        
        if (linePositionsX == null || linePositionsX.Length != totalLines)
        {
            linePositionsX = new float[totalLines];
        }
        
        float startX = -screenWidth / 2f + margin;
        
        for (int i = 0; i < totalLines; i++)
        {
            linePositionsX[i] = startX + (i * lineSpacing);
        }
    }
    
    public float GetLineX(int lineIndex)
    {
        if (linePositionsX == null || linePositionsX.Length == 0)
        {
            CalculateLines();
        }
        
        if (lineIndex < 0 || lineIndex >= totalLines)
        {
            return 0f;
        }
        
        if (linePositionsX == null || lineIndex >= linePositionsX.Length)
        {
            return 0f;
        }
        
        return linePositionsX[lineIndex];
    }
    
    public float GetCenterLineX() => GetLineX(totalLines / 2);
    
    public float GetRandomLineX(int[] allowedLines)
    {
        if (allowedLines == null || allowedLines.Length == 0)
        {
            return GetCenterLineX();
        }
        
        int randomIndex = allowedLines[Random.Range(0, allowedLines.Length)];
        return GetLineX(randomIndex);
    }
    
    public int GetNearestLineIndex(float x)
    {
        int nearestIndex = 0;
        float minDistance = Mathf.Abs(x - linePositionsX[0]);
        
        for (int i = 1; i < totalLines; i++)
        {
            float distance = Mathf.Abs(x - linePositionsX[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }
        
        return nearestIndex;
    }
    
    public float SnapToNearestLine(float x)
    {
        int lineIndex = GetNearestLineIndex(x);
        return GetLineX(lineIndex);
    }
    
    public float[] GetAllLinePositions() => linePositionsX;
    public float GetLineSpacing() => lineSpacing;
    public int GetTotalLines() => totalLines;
    
    void OnDrawGizmos()
    {
        if (!showDebugLines) return;
        
        if (linePositionsX == null || linePositionsX.Length == 0) 
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            
            if (mainCamera != null)
                CalculateLines();
            
            if (linePositionsX == null || linePositionsX.Length == 0)
                return;
        }
        
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (mainCamera == null) return;
        
        float screenHeight = mainCamera.orthographicSize * 2f;
        float bottomY = mainCamera.transform.position.y - screenHeight / 2f;
        float topY = mainCamera.transform.position.y + screenHeight / 2f;
        
        for (int i = 0; i < linePositionsX.Length && i < totalLines; i++)
        {
            if (i == totalLines / 2)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = lineColor;
            }
            
            Vector3 bottom = new Vector3(linePositionsX[i], bottomY, 0f);
            Vector3 top = new Vector3(linePositionsX[i], topY, 0f);
            
            Gizmos.DrawLine(bottom, top);
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                new Vector3(linePositionsX[i], topY - 0.5f, 0f),
                $"L{i}",
                new GUIStyle() { normal = new GUIStyleState() { textColor = Gizmos.color } }
            );
#endif
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && mainCamera != null)
        {
            CalculateLines();
        }
    }
}