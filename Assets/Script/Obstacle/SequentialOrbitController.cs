using UnityEngine;
using System.Collections;

public class SequentialOrbitController : MonoBehaviour
{
    [Header("=== ORBIT ===")]
    [SerializeField] private float rotationSpeed  = 90f;
    [SerializeField] private float radius         = 3f;
    [SerializeField] private bool clockwise       = true;

    [Header("=== SPRITE ROTATION ===")]
    [SerializeField] private bool rotateTowardsCenter   = true;
    [SerializeField] private float spriteRotationOffset = 90f;

    [Header("=== TIMING ===")]
    [Tooltip("Delay giữa mỗi paddle búng ra")]
    [SerializeField] private float staggerDelay      = 0.15f;
    [Tooltip("Thời gian paddle tồn tại (bao gồm cả popOut)")]
    [SerializeField] private float paddleLifetime    = 1.5f;
    [Tooltip("Thời gian búng ra từ tâm")]
    [SerializeField] private float popOutDuration    = 0.3f;
    [Tooltip("Thời gian shrink biến mất")]
    [SerializeField] private float disappearDuration = 0.25f;
    [Tooltip("Nghỉ giữa các chu kỳ")]
    [SerializeField] private float cycleRestDuration = 0.3f;

    [Header("=== BOUNCE ===")]
    [SerializeField][Range(0f, 0.6f)] private float overshootAmount = 0.3f;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugGizmos = true;

    private enum PaddleState { Waiting, PoppingOut, Alive, Disappearing }

    private class PaddleData
    {
        public Transform   transform;
        public float       baseAngle;
        public float       currentAngle;
        public PaddleState state         = PaddleState.Waiting;
        public float       stateTimer    = 0f;
        public float       currentRadius = 0f;
        public Vector3     originalScale = Vector3.one;
        public bool        active        = false;
    }

    private PaddleData[] paddles;
    private float baseRotSpeed;

    void Awake()
    {
        baseRotSpeed = clockwise ? rotationSpeed : -rotationSpeed;
        SetupPaddles();
    }

    void Start()
    {
        StartCoroutine(RunCycle());
    }

    void SetupPaddles()
    {
        int count = transform.childCount;
        if (count == 0) return;

        paddles = new PaddleData[count];
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);

            // Lưu scale gốc TRƯỚC khi ẩn
            Vector3 savedScale = child.localScale;
            if (savedScale.magnitude < 0.001f) savedScale = Vector3.one;

            paddles[i] = new PaddleData
            {
                transform     = child,
                baseAngle     = i * angleStep,
                currentAngle  = i * angleStep,
                originalScale = savedScale,
                active        = false
            };

            // Ẩn ban đầu
            child.localScale = Vector3.zero;
        }
    }

    void Update()
    {
        if (paddles == null) return;
        float deltaAngle = baseRotSpeed * Time.deltaTime;

        foreach (var p in paddles)
        {
            if (p.transform == null || !p.active) continue;

            p.currentAngle += deltaAngle;
            if      (p.currentAngle >= 360f) p.currentAngle -= 360f;
            else if (p.currentAngle <    0f) p.currentAngle += 360f;

            UpdatePaddleState(p);

            float rad = p.currentAngle * Mathf.Deg2Rad;
            p.transform.position = new Vector3(
                transform.position.x + Mathf.Cos(rad) * p.currentRadius,
                transform.position.y + Mathf.Sin(rad) * p.currentRadius,
                p.transform.position.z
            );

            if (rotateTowardsCenter && p.currentRadius > 0.01f)
            {
                Vector3 dir = transform.position - p.transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteRotationOffset;
                p.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    void UpdatePaddleState(PaddleData p)
    {
        p.stateTimer -= Time.deltaTime;

        switch (p.state)
        {
            case PaddleState.PoppingOut:
            {
                float t = 1f - Mathf.Clamp01(p.stateTimer / popOutDuration);
                p.currentRadius        = BounceOut(t) * radius;
                p.transform.localScale = p.originalScale * Mathf.Clamp01(t / 0.5f);

                if (p.stateTimer <= 0f)
                {
                    p.currentRadius        = radius;
                    p.transform.localScale = p.originalScale;
                    p.state      = PaddleState.Alive;
                    p.stateTimer = Mathf.Max(0.01f, paddleLifetime - popOutDuration);
                }
                break;
            }

            case PaddleState.Alive:
            {
                p.currentRadius = radius;
                if (p.stateTimer <= 0f)
                {
                    p.state      = PaddleState.Disappearing;
                    p.stateTimer = disappearDuration;
                }
                break;
            }

            case PaddleState.Disappearing:
            {
                float t = 1f - Mathf.Clamp01(p.stateTimer / disappearDuration);
                float s = 1f - t;
                p.currentRadius        = radius * s;
                p.transform.localScale = p.originalScale * s;

                if (p.stateTimer <= 0f)
                {
                    p.transform.localScale = Vector3.zero;
                    p.currentRadius        = 0f;
                    p.state  = PaddleState.Waiting;
                    p.active = false;
                }
                break;
            }
        }
    }

    void ActivatePaddle(PaddleData p)
    {
        p.currentAngle         = p.baseAngle;
        p.currentRadius        = 0f;
        p.transform.localScale = Vector3.zero;

        float rad = p.currentAngle * Mathf.Deg2Rad;
        p.transform.position = new Vector3(
            transform.position.x + Mathf.Cos(rad) * 0.01f,
            transform.position.y + Mathf.Sin(rad) * 0.01f,
            p.transform.position.z
        );

        p.state      = PaddleState.PoppingOut;
        p.stateTimer = popOutDuration;
        p.active     = true;
    }

    IEnumerator RunCycle()
    {
        yield return null; // đợi 1 frame

        while (true)
        {
            foreach (var p in paddles)
            {
                if (p == null || p.transform == null) continue;
                ActivatePaddle(p);
                yield return new WaitForSeconds(staggerDelay);
            }

            // Chờ paddle cuối hoàn thành
            float waitTime = paddleLifetime + disappearDuration + cycleRestDuration;
            yield return new WaitForSeconds(waitTime);
        }
    }

    float BounceOut(float t)
    {
        float peak = 0.6f;
        if (t < peak)
        {
            float t2 = t / peak;
            return (1f + overshootAmount) * (t2 * t2 * (3f - 2f * t2));
        }
        else
        {
            float t2     = (t - peak) / (1f - peak);
            float smooth = t2 * t2 * (3f - 2f * t2);
            return Mathf.Lerp(1f + overshootAmount, 1f, smooth);
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            baseRotSpeed = clockwise ? Mathf.Abs(rotationSpeed) : -Mathf.Abs(rotationSpeed);
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 50; i++)
        {
            float a1 = i       * (360f / 50) * Mathf.Deg2Rad;
            float a2 = (i + 1) * (360f / 50) * Mathf.Deg2Rad;
            Gizmos.DrawLine(
                new Vector3(transform.position.x + Mathf.Cos(a1) * radius,
                            transform.position.y + Mathf.Sin(a1) * radius, transform.position.z),
                new Vector3(transform.position.x + Mathf.Cos(a2) * radius,
                            transform.position.y + Mathf.Sin(a2) * radius, transform.position.z)
            );
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }

    public void SetRotationSpeed(float speed) { rotationSpeed = Mathf.Abs(speed); baseRotSpeed = clockwise ? rotationSpeed : -rotationSpeed; }
    public void SetRadius(float r)            => radius = Mathf.Max(0.1f, r);
    public void SetClockwise(bool cw)         { clockwise = cw; baseRotSpeed = cw ? Mathf.Abs(rotationSpeed) : -Mathf.Abs(rotationSpeed); }
    public void Stop()                        => enabled = false;
    public void Resume()                      => enabled = true;
}