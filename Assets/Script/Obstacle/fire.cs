using UnityEngine;

public class fire : MonoBehaviour
{
    [Header("Center of the path")]
    public Transform center;

    [Header("Figure-8 settings - RESPONSIVE")]
    [Tooltip("Số lượng line mà figure-8 chiếm (1-9)")]
    [Range(1f, 9f)] public float lineSpan = 3f;  // Chiếm bao nhiêu line
    
    [Tooltip("Tỷ lệ chiều cao so với chiều rộng")]
    [Range(0.1f, 2f)] public float heightRatio = 0.5f;  // amplitudeY = amplitudeX * ratio
    
    public float speed = 1.5f;

    [Header("Start & Direction")]
    [Range(0f, 360f)] public float startPhaseDeg = 0f;
    public bool reverseDirection = false;
    public bool randomStartPhase = false;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // Private
    private Vector3 _startPos;
    private float _phaseRad;
    private float _amplitudeX;  // Tính động
    private float _amplitudeY;  // Tính động

    void Start()
    {
        _startPos = (center != null) ? center.position : transform.position;

        if (randomStartPhase)
        {
            _phaseRad = Random.Range(0f, Mathf.PI * 2f);
        }
        else
        {
            _phaseRad = startPhaseDeg * Mathf.Deg2Rad;
        }

        // ⭐ Tính amplitude dựa vào LineSystem
        CalculateAmplitudes();
    }

    void CalculateAmplitudes()
    {
        if (LineSystem.Instance == null)
        {
            Debug.LogWarning("⚠️ LineSystem not found! Using default values.");
            _amplitudeX = 2f;
            _amplitudeY = 1f;
            return;
        }

        // Lấy khoảng cách giữa các line
        float lineSpacing = LineSystem.Instance.GetLineSpacing();
        
        // amplitudeX = lineSpacing * số line muốn chiếm / 2
        _amplitudeX = (lineSpacing * lineSpan) / 2f;
        
        // amplitudeY dựa vào tỷ lệ
        _amplitudeY = _amplitudeX * heightRatio;

        if (showDebug)
        {
            Debug.Log($"🔥 {gameObject.name}:");
            Debug.Log($"   ├─ Line Spacing: {lineSpacing:F2}");
            Debug.Log($"   ├─ Line Span: {lineSpan}");
            Debug.Log($"   ├─ AmplitudeX: {_amplitudeX:F2}");
            Debug.Log($"   └─ AmplitudeY: {_amplitudeY:F2}");
        }
    }

    void Update()
    {
        float dir = reverseDirection ? -1f : 1f;
        float t = (Time.time * speed * dir) + _phaseRad;

        // Hình số 8 ngang (∞) với amplitude động
        float x = _amplitudeX * Mathf.Sin(t);
        float y = _amplitudeY * Mathf.Sin(2f * t);

        transform.position = _startPos + new Vector3(x, y, 0f);
    }

    // ⭐ Recalculate khi thay đổi resolution
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            CalculateAmplitudes();
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebug || !Application.isPlaying) return;

        // Vẽ bounding box của figure-8
        Vector3 center = _startPos;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(_amplitudeX * 2f, _amplitudeY * 2f, 0.1f));
    }
}