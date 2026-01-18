using UnityEngine;

public class caigai : MonoBehaviour
{
    public float moveDistance = 2f;
    public float period = 2f;
    public float phaseOffset = 0f;

    public float speedMultiplier = 1f; // tốc độ (1 = bình thường)

    [Header("Extra Smooth")]
    [Range(0.01f, 0.5f)]
    public float smoothTime = 0.08f;   // ✅ càng nhỏ càng nhanh, càng lớn càng mượt
    public float maxSpeed = 999f;      // giới hạn tốc độ (để lớn cho tự do)

    public enum MoveMode
    {
        PingPong,
        OnlyRight,
        OnlyLeft
    }

    public MoveMode moveMode = MoveMode.PingPong;

    private Vector3 startPos;
    private float xVel = 0f;
    private float currentOffset = 0f;

    void Start()
    {
        startPos = transform.position;
        currentOffset = 0f;
    }

    void Update()
    {
        if (period <= 0.0001f) return;

        float time = (Time.time * speedMultiplier) + phaseOffset;

        // t = 0..1..0..1 (mượt)
        float t = (Mathf.Sin(time * (2f * Mathf.PI) / period) + 1f) * 0.5f;

        // target offset theo mode
        float targetOffset;
        if (moveMode == MoveMode.PingPong)
        {
            targetOffset = Mathf.Lerp(-moveDistance, moveDistance, t);
        }
        else if (moveMode == MoveMode.OnlyRight)
        {
            targetOffset = Mathf.Lerp(0f, moveDistance, t);
        }
        else // OnlyLeft
        {
            targetOffset = Mathf.Lerp(0f, -moveDistance, t);
        }

        // ✅ SmoothDamp cho offset X -> siêu mượt, không giật
        currentOffset = Mathf.SmoothDamp(currentOffset, targetOffset, ref xVel, smoothTime, maxSpeed, Time.deltaTime);

        transform.position = new Vector3(startPos.x + currentOffset, startPos.y, startPos.z);
    }
}
