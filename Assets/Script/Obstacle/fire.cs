using UnityEngine;

public class fire : MonoBehaviour
{
    [Header("Center of the path")]
    public Transform center;               // tâm quỹ đạo (để trống = vị trí ban đầu)

    [Header("Figure-8 settings")]
    public float amplitudeX = 2f;          // độ rộng ngang
    public float amplitudeY = 1f;          // độ cao
    public float speed = 1.5f;             // tốc độ

    [Header("Start & Direction")]
    [Range(0f, 360f)] public float startPhaseDeg = 0f;  // điểm bắt đầu (độ)
    public bool reverseDirection = false;               // true = chạy ngược
    public bool randomStartPhase = false;               // true = mỗi object tự random phase

    Vector3 _startPos;
    float _phaseRad;

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
    }

    void Update()
    {
        float dir = reverseDirection ? -1f : 1f;
        float t = (Time.time * speed * dir) + _phaseRad;

        // hình số 8 ngang (∞)
        float x = amplitudeX * Mathf.Sin(t);
        float y = amplitudeY * Mathf.Sin(2f * t);

        transform.position = _startPos + new Vector3(x, y, 0f);
    }
}
