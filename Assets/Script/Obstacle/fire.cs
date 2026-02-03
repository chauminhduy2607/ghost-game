using UnityEngine;

/// <summary>
/// Figure-8 movement responsive với LineSystem
/// </summary>
public class fire : MonoBehaviour
{
    [Header("Center of the path")]
    public Transform center;

    [Header("Figure-8 settings - RESPONSIVE")]
    [Range(1f, 9f)] public float lineSpan = 3f;
    [Range(0.1f, 2f)] public float heightRatio = 0.5f;
    public float speed = 1.5f;

    [Header("Start & Direction")]
    [Range(0f, 360f)] public float startPhaseDeg = 0f;
    public bool reverseDirection = false;
    public bool randomStartPhase = false;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private Vector3 _startPos;
    private float _phaseRad;
    private float _amplitudeX;
    private float _amplitudeY;

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

        CalculateAmplitudes();
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
        float dir = reverseDirection ? -1f : 1f;
        float t = (Time.time * speed * dir) + _phaseRad;

        float x = _amplitudeX * Mathf.Sin(t);
        float y = _amplitudeY * Mathf.Sin(2f * t);

        transform.position = _startPos + new Vector3(x, y, 0f);
    }

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

        Vector3 center = _startPos;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(_amplitudeX * 2f, _amplitudeY * 2f, 0.1f));
    }
}