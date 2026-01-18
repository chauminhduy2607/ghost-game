using UnityEngine;

public class caigai : MonoBehaviour
{
    public float moveDistance = 2f;
    public float period = 2f;
    public float phaseOffset = 0f;

    public float speedMultiplier = 1f; // tốc độ (1 = bình thường)

    public enum MoveMode
    {
        PingPong,   // qua lại trái phải (như cũ)
        OnlyRight,  // chỉ chạy sang phải
        OnlyLeft    // chỉ chạy sang trái
    }

    public MoveMode moveMode = MoveMode.PingPong;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (period <= 0.0001f) return;

        float time = (Time.time * speedMultiplier) + phaseOffset;

        float t = (Mathf.Sin(time * (2f * Mathf.PI) / period) + 1f) * 0.5f;
        t = Mathf.SmoothStep(0f, 1f, t);

        float xOffset;

        if (moveMode == MoveMode.PingPong)
        {
            // như cũ: -distance -> +distance -> -distance ...
            xOffset = Mathf.Lerp(-moveDistance, moveDistance, t);
        }
        else if (moveMode == MoveMode.OnlyRight)
        {
            // chỉ chạy từ 0 -> +distance rồi quay về 0 -> +distance ...
            xOffset = Mathf.Lerp(0f, moveDistance, t);
        }
        else // OnlyLeft
        {
            // chỉ chạy từ 0 -> -distance rồi quay về 0 -> -distance ...
            xOffset = Mathf.Lerp(0f, -moveDistance, t);
        }

        transform.position = new Vector3(startPos.x + xOffset, startPos.y, startPos.z);
    }
}
