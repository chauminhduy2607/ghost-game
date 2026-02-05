using UnityEngine;
using System.Collections;

/// <summary>
/// Fire di chuyển figure-8 với CENTER ƯU TIÊN Ở NGOÀI
/// Thỉnh thoảng "tấn công" vào giữa (line 4,5,6)
/// </summary>
public class FireNew : MonoBehaviour
{
    [Header("Figure-8 settings")]
    [Tooltip("Số line dao động (biên độ ngang)")]
    [Range(2f, 4f)] public float lineSpan = 3f;
    
    [Tooltip("Tỷ lệ chiều cao / chiều rộng")]
    [Range(0.3f, 1.5f)] public float heightRatio = 0.6f;
    
    [Tooltip("Tốc độ di chuyển trên quỹ đạo")]
    public float speed = 1.5f;

    [Header("Dynamic Center Movement")]
    [Tooltip("Bật để center di chuyển")]
    public bool enableDynamicCenter = true;
    
    [Tooltip("Thời gian ở mỗi center (giây)")]
    [Range(2f, 10f)] public float centerDuration = 5f;
    
    [Tooltip("Thời gian chuyển center (giây)")]
    [Range(0.5f, 3f)] public float centerTransitionTime = 1.5f;
    
    [Header("Center Zone Weights (Tỷ lệ xuất hiện)")]
    [Tooltip("Tỷ lệ xuất hiện ở line NGOÀI (0,1,2,3,7,8)")]
    [Range(0f, 100f)] public float outerZoneWeight = 80f;
    
    [Tooltip("Tỷ lệ xuất hiện ở line GIỮA (4,5,6) - NGUY HIỂM!")]
    [Range(0f, 100f)] public float centerZoneWeight = 20f;
    
    [Header("Phase Distribution")]
    [Tooltip("Tự động chia đều phase cho các fire cùng parent")]
    public bool autoDistributePhase = true;
    
    [Tooltip("Hoặc dùng phase thủ công")]
    [Range(0f, 360f)] public float manualPhaseDeg = 0f;

    [Header("Direction")]
    public bool reverseDirection = false;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool showPathPreview = true;
    [SerializeField] private bool showZones = true;

    // Private variables
    private Vector3 _currentCenter;
    private Vector3 _targetCenter;
    private float _phaseRad;
    private float _amplitudeX;
    private float _amplitudeY;
    private Coroutine _centerMovementCoroutine;
    private bool _isTransitioningCenter = false;
    
    // Zone definitions (cho 9 line: 0-8)
    private int[] _outerZoneLines = new int[] { 0, 1, 2, 3, 7, 8 }; // 6 line ngoài
    private int[] _centerZoneLines = new int[] { 4, 5, 6 };          // 3 line giữa

    void Start()
    {
        InitializePhase();
        InitializeCenter();
        CalculateAmplitudes();
        
        if (enableDynamicCenter)
        {
            _centerMovementCoroutine = StartCoroutine(CenterMovementLoop());
        }
        
        Debug.Log($"[{gameObject.name}] Phase: {_phaseRad * Mathf.Rad2Deg:F1}° | Start: Line {GetCurrentCenterLineIndex()} ({GetZoneName(GetCurrentCenterLineIndex())})");
    }

    void InitializePhase()
    {
        if (autoDistributePhase && transform.parent != null)
        {
            _phaseRad = CalculateAutoPhase();
        }
        else
        {
            _phaseRad = manualPhaseDeg * Mathf.Deg2Rad;
        }
    }

    float CalculateAutoPhase()
    {
        Transform parent = transform.parent;
        if (parent == null) return 0f;

        int siblingCount = 0;
        int myIndex = 0;
        
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            fire fireComponent = child.GetComponent<fire>();
            
            if (fireComponent != null && fireComponent.autoDistributePhase)
            {
                if (child == transform)
                {
                    myIndex = siblingCount;
                }
                siblingCount++;
            }
        }

        if (siblingCount <= 1) return 0f;

        float phaseStep = (Mathf.PI * 2f) / siblingCount;
        return myIndex * phaseStep;
    }

    void InitializeCenter()
    {
        if (LineSystem.Instance == null)
        {
            _currentCenter = transform.position;
            _targetCenter = _currentCenter;
            Debug.LogWarning($"[{gameObject.name}] LineSystem not found!");
            return;
        }

        // Bắt đầu ở outer zone
        int centerLine = GetWeightedRandomLine();
        float centerX = LineSystem.Instance.GetLineX(centerLine);
        float centerY = transform.position.y;
        
        _currentCenter = new Vector3(centerX, centerY, 0f);
        _targetCenter = _currentCenter;
    }

    void CalculateAmplitudes()
    {
        if (LineSystem.Instance == null)
        {
            _amplitudeX = 2f;
            _amplitudeY = 1f;
            return;
        }

        float lineSpacing = LineSystem.Instance.GetLineSpacing();
        _amplitudeX = (lineSpacing * lineSpan) / 2f;
        _amplitudeY = _amplitudeX * heightRatio;
    }

    void Update()
    {
        // Tính vị trí trên quỹ đạo figure-8
        float dir = reverseDirection ? -1f : 1f;
        float t = (Time.time * speed * dir) + _phaseRad;

        float x = _amplitudeX * Mathf.Sin(t);
        float y = _amplitudeY * Mathf.Sin(2f * t);

        transform.position = _currentCenter + new Vector3(x, y, 0f);
    }

    // ========================================
    // WEIGHTED RANDOM CENTER SYSTEM
    // ========================================

    IEnumerator CenterMovementLoop()
    {
        while (true)
        {
            // 1. Ở center hiện tại một lúc
            yield return new WaitForSeconds(centerDuration);
            
            // 2. Chọn center mới theo tỷ lệ (weighted random)
            int newCenterLine = GetWeightedRandomLine(excludeCurrentLine: true);
            
            if (LineSystem.Instance != null)
            {
                string oldZone = GetZoneName(GetCurrentCenterLineIndex());
                string newZone = GetZoneName(newCenterLine);
                
                float newCenterX = LineSystem.Instance.GetLineX(newCenterLine);
                _targetCenter = new Vector3(newCenterX, _currentCenter.y, 0f);
                
                Debug.Log($"[{gameObject.name}] Moving: Line {GetCurrentCenterLineIndex()} ({oldZone}) → Line {newCenterLine} ({newZone})");
                
                // 3. Di chuyển center mượt mà
                yield return StartCoroutine(TransitionCenter(_currentCenter, _targetCenter, centerTransitionTime));
            }
        }
    }

    IEnumerator TransitionCenter(Vector3 fromCenter, Vector3 toCenter, float duration)
    {
        _isTransitioningCenter = true;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _currentCenter = Vector3.Lerp(fromCenter, toCenter, t);
            yield return null;
        }
        
        _currentCenter = toCenter;
        _isTransitioningCenter = false;
    }

    /// <summary>
    /// Chọn line theo tỷ lệ: outerZoneWeight% cho outer, centerZoneWeight% cho center
    /// </summary>
    int GetWeightedRandomLine(bool excludeCurrentLine = false)
    {
        if (LineSystem.Instance == null) return 4;

        float totalWeight = outerZoneWeight + centerZoneWeight;
        float randomValue = Random.Range(0f, totalWeight);
        
        int[] selectedZone;
        
        // Quyết định zone dựa trên random value
        if (randomValue < outerZoneWeight)
        {
            // Chọn OUTER zone (line 0,1,2,3,7,8)
            selectedZone = _outerZoneLines;
        }
        else
        {
            // Chọn CENTER zone (line 4,5,6) - NGUY HIỂM!
            selectedZone = _centerZoneLines;
        }
        
        // Loại bỏ current line nếu cần
        if (excludeCurrentLine && selectedZone.Length > 1)
        {
            int currentLine = GetCurrentCenterLineIndex();
            System.Collections.Generic.List<int> filtered = new System.Collections.Generic.List<int>();
            
            foreach (int line in selectedZone)
            {
                if (line != currentLine)
                {
                    filtered.Add(line);
                }
            }
            
            if (filtered.Count > 0)
            {
                selectedZone = filtered.ToArray();
            }
        }

        return selectedZone[Random.Range(0, selectedZone.Length)];
    }

    int GetCurrentCenterLineIndex()
    {
        if (LineSystem.Instance == null) return 4;
        return LineSystem.Instance.GetNearestLineIndex(_currentCenter.x);
    }

    string GetZoneName(int lineIndex)
    {
        if (System.Array.Exists(_centerZoneLines, line => line == lineIndex))
        {
            return "CENTER ⚠️";
        }
        else
        {
            return "OUTER";
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            CalculateAmplitudes();
        }
    }

    void OnDestroy()
    {
        if (_centerMovementCoroutine != null)
        {
            StopCoroutine(_centerMovementCoroutine);
        }
    }

    // ========================================
    // DEBUG VISUALIZATION
    // ========================================

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        Vector3 centerPos = Application.isPlaying ? _currentCenter : transform.position;

        // Vẽ zones
        if (showZones && LineSystem.Instance != null)
        {
            DrawZones();
        }

        // Vẽ center hiện tại
        int currentLine = GetCurrentCenterLineIndex();
        bool isInCenterZone = System.Array.Exists(_centerZoneLines, line => line == currentLine);
        
        Gizmos.color = isInCenterZone ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(centerPos, 0.25f);
        
        // Vẽ target center nếu đang transition
        if (Application.isPlaying && _isTransitioningCenter)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_targetCenter, 0.15f);
            Gizmos.DrawLine(centerPos, _targetCenter);
        }

        // Vẽ quỹ đạo figure-8
        if (showPathPreview)
        {
            DrawFigure8Path(centerPos);
        }
    }

    void DrawZones()
    {
        if (LineSystem.Instance == null) return;
        
        float screenHeight = Camera.main.orthographicSize * 2f;
        float bottomY = Camera.main.transform.position.y - screenHeight / 2f;
        float topY = Camera.main.transform.position.y + screenHeight / 2f;
        
        // Vẽ OUTER zone (xanh lá - an toàn)
        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
        foreach (int lineIndex in _outerZoneLines)
        {
            float x = LineSystem.Instance.GetLineX(lineIndex);
            float width = LineSystem.Instance.GetLineSpacing();
            Vector3 center = new Vector3(x, (topY + bottomY) / 2f, 0f);
            Vector3 size = new Vector3(width * 0.8f, screenHeight, 0.1f);
            DrawTransparentBox(center, size, new Color(0f, 1f, 0f, 0.1f));
        }
        
        // Vẽ CENTER zone (đỏ - nguy hiểm)
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        foreach (int lineIndex in _centerZoneLines)
        {
            float x = LineSystem.Instance.GetLineX(lineIndex);
            float width = LineSystem.Instance.GetLineSpacing();
            Vector3 center = new Vector3(x, (topY + bottomY) / 2f, 0f);
            Vector3 size = new Vector3(width * 0.8f, screenHeight, 0.1f);
            DrawTransparentBox(center, size, new Color(1f, 0f, 0f, 0.2f));
        }
    }

    void DrawTransparentBox(Vector3 center, Vector3 size, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawCube(center, size);
    }

    void DrawFigure8Path(Vector3 centerPos)
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        
        float ampX = _amplitudeX;
        float ampY = _amplitudeY;
        
        if (!Application.isPlaying && LineSystem.Instance != null)
        {
            float lineSpacing = LineSystem.Instance.GetLineSpacing();
            ampX = (lineSpacing * lineSpan) / 2f;
            ampY = ampX * heightRatio;
        }
        else if (!Application.isPlaying)
        {
            ampX = 1.5f;
            ampY = 0.9f;
        }
        
        int segments = 80;
        Vector3 prevPoint = Vector3.zero;
        
        for (int i = 0; i <= segments; i++)
        {
            float t = (i / (float)segments) * Mathf.PI * 2f;
            float x = ampX * Mathf.Sin(t);
            float y = ampY * Mathf.Sin(2f * t);
            
            Vector3 point = centerPos + new Vector3(x, y, 0f);
            
            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, point);
            }
            
            prevPoint = point;
        }
    }

    // ========================================
    // PUBLIC API
    // ========================================

    public Vector3 GetCurrentCenter() => _currentCenter;
    public float GetPhase() => _phaseRad;
    public int GetCurrentCenterLine() => GetCurrentCenterLineIndex();
    public bool IsInCenterZone() => System.Array.Exists(_centerZoneLines, line => line == GetCurrentCenterLineIndex());
}