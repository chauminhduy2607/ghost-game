using UnityEngine;

public class PulsingCircleOrbitController : MonoBehaviour
{
    [Header("=== ROTATION ===")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float radius = 3f;
    [SerializeField] private bool clockwise = true;

    [Header("=== SPRITE ROTATION ===")]
    [SerializeField] private bool rotateTowardsCenter = true;
    [SerializeField] private float spriteRotationOffset = 90f;

    [Header("=== RETRACT / EXPAND CYCLE ===")]
    [SerializeField] private float retractDuration = 2f;
    [SerializeField] private float holdDuration    = 0f;
    [SerializeField] private float expandDuration  = 1f;
    [SerializeField] private float pauseDuration   = 1.5f;
    [SerializeField] private AnimationCurve retractCurve = AnimationCurve.EaseInOut(0,0,1,1);
    [SerializeField] private AnimationCurve expandCurve  = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugGizmos = true;

    private class PaddleData
    {
        public Transform transform;
        public float currentAngle;
    }

    private enum CycleState { Pausing, Retracting, Holding, Expanding }

    private PaddleData[] paddles;
    private float currentRotationSpeed;
    private CycleState cycleState = CycleState.Pausing;
    private float cycleTimer      = 0f;
    private float currentRadius;

    void Start()
    {
        currentRadius = radius;
        SetupPaddles();
        currentRotationSpeed = clockwise ? rotationSpeed : -rotationSpeed;
        cycleState = CycleState.Pausing;
        cycleTimer = pauseDuration;
    }

    void Update()
    {
        if (paddles == null) return;
        UpdateCycle();

        float deltaAngle = currentRotationSpeed * Time.deltaTime;
        foreach (var paddle in paddles)
        {
            if (paddle.transform == null) continue;

            paddle.currentAngle += deltaAngle;
            if      (paddle.currentAngle >= 360f) paddle.currentAngle -= 360f;
            else if (paddle.currentAngle <    0f) paddle.currentAngle += 360f;

            float rad = paddle.currentAngle * Mathf.Deg2Rad;
            paddle.transform.position = new Vector3(
                transform.position.x + Mathf.Cos(rad) * currentRadius,
                transform.position.y + Mathf.Sin(rad) * currentRadius,
                paddle.transform.position.z
            );
        }
    }

    void UpdateCycle()
    {
        cycleTimer -= Time.deltaTime;

        switch (cycleState)
        {
            case CycleState.Pausing:
                currentRadius = radius;
                if (cycleTimer <= 0f)
                {
                    cycleState = CycleState.Retracting;
                    cycleTimer = retractDuration;
                }
                break;

            case CycleState.Retracting:
            {
                float t = 1f - Mathf.Clamp01(cycleTimer / retractDuration);
                currentRadius = Mathf.Lerp(radius, 0f, retractCurve.Evaluate(t));
                if (cycleTimer <= 0f)
                {
                    currentRadius = 0f;
                    if (holdDuration > 0f) { cycleState = CycleState.Holding;  cycleTimer = holdDuration; }
                    else                   { cycleState = CycleState.Expanding; cycleTimer = expandDuration; }
                }
                break;
            }

            case CycleState.Holding:
                currentRadius = 0f;
                if (cycleTimer <= 0f)
                {
                    cycleState = CycleState.Expanding;
                    cycleTimer = expandDuration;
                }
                break;

            case CycleState.Expanding:
            {
                float t = 1f - Mathf.Clamp01(cycleTimer / expandDuration);
                currentRadius = Mathf.Lerp(0f, radius, expandCurve.Evaluate(t));
                if (cycleTimer <= 0f)
                {
                    currentRadius = radius;
                    cycleState    = CycleState.Pausing;
                    cycleTimer    = pauseDuration;
                }
                break;
            }
        }
    }

    void LateUpdate()
    {
        if (!rotateTowardsCenter || paddles == null) return;
        foreach (var paddle in paddles)
        {
            if (paddle.transform == null) continue;
            Vector3 dir = transform.position - paddle.transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteRotationOffset;
            paddle.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void SetupPaddles()
    {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        paddles = new PaddleData[childCount];
        float angleStep = 360f / childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            paddles[i] = new PaddleData { transform = child, currentAngle = i * angleStep };

            float rad = paddles[i].currentAngle * Mathf.Deg2Rad;
            child.position = new Vector3(
                transform.position.x + Mathf.Cos(rad) * radius,
                transform.position.y + Mathf.Sin(rad) * radius,
                child.position.z
            );
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            currentRotationSpeed = clockwise ? Mathf.Abs(rotationSpeed) : -Mathf.Abs(rotationSpeed);
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        float drawRadius = Application.isPlaying ? currentRadius : radius;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < 50; i++)
        {
            float a1 = i * (360f / 50) * Mathf.Deg2Rad;
            float a2 = (i + 1) * (360f / 50) * Mathf.Deg2Rad;
            Gizmos.DrawLine(
                new Vector3(transform.position.x + Mathf.Cos(a1) * drawRadius, transform.position.y + Mathf.Sin(a1) * drawRadius, transform.position.z),
                new Vector3(transform.position.x + Mathf.Cos(a2) * drawRadius, transform.position.y + Mathf.Sin(a2) * drawRadius, transform.position.z)
            );
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        if (Application.isPlaying && paddles != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var p in paddles)
                if (p.transform != null)
                    Gizmos.DrawLine(transform.position, p.transform.position);
        }
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed        = Mathf.Abs(speed);
        currentRotationSpeed = clockwise ? rotationSpeed : -rotationSpeed;
    }

    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.1f, newRadius);
        if (paddles == null) return;
        foreach (var p in paddles)
        {
            if (p.transform == null) continue;
            float rad = p.currentAngle * Mathf.Deg2Rad;
            p.transform.position = new Vector3(
                transform.position.x + Mathf.Cos(rad) * radius,
                transform.position.y + Mathf.Sin(rad) * radius,
                p.transform.position.z
            );
        }
    }

    public void SetClockwise(bool isClockwise)
    {
        clockwise            = isClockwise;
        currentRotationSpeed = clockwise ? Mathf.Abs(rotationSpeed) : -Mathf.Abs(rotationSpeed);
    }

    public void Stop()   => enabled = false;
    public void Resume() => enabled = true;
}