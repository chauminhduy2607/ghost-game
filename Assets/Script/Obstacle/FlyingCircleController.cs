using UnityEngine;

public class FlyingCircleController : MonoBehaviour
{
    [Header("=== ROTATION ===")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float radius = 3f;
    [SerializeField] private bool clockwise = true;
    
    [Header("=== SPRITE ROTATION ===")]
    [SerializeField] private bool rotateTowardsCenter = true;
    [SerializeField] private float spriteRotationOffset = 90f;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugGizmos = true;
    
    private class PaddleData
    {
        public Transform transform;
        public float currentAngle;
    }
    
    private PaddleData[] paddles;
    private float currentRotationSpeed;
    
    void Start()
    {
        SetupPaddles();
        currentRotationSpeed = clockwise ? rotationSpeed : -rotationSpeed;
    }
    
    void Update()
    {
        if (paddles == null) return;
        
        float deltaAngle = currentRotationSpeed * Time.deltaTime;
        
        foreach (var paddle in paddles)
        {
            if (paddle.transform == null) continue;
            
            paddle.currentAngle += deltaAngle;
            
            if (paddle.currentAngle >= 360f)
                paddle.currentAngle -= 360f;
            else if (paddle.currentAngle < 0f)
                paddle.currentAngle += 360f;
            
            float angleInRadians = paddle.currentAngle * Mathf.Deg2Rad;
            float x = transform.position.x + Mathf.Cos(angleInRadians) * radius;
            float y = transform.position.y + Mathf.Sin(angleInRadians) * radius;
            
            paddle.transform.position = new Vector3(x, y, paddle.transform.position.z);
        }
    }
    
    void LateUpdate()
    {
        if (!rotateTowardsCenter || paddles == null) return;
        
        foreach (var paddle in paddles)
        {
            if (paddle.transform == null) continue;
            
            Vector3 directionToCenter = transform.position - paddle.transform.position;
            float angle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;
            angle += spriteRotationOffset;
            
            paddle.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    void SetupPaddles()
    {
        int childCount = transform.childCount;
        if (childCount == 0)
        {
            return;
        }
        
        paddles = new PaddleData[childCount];
        
        float angleStep = 360f / childCount;
        
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            
            paddles[i] = new PaddleData
            {
                transform = child,
                currentAngle = i * angleStep
            };
            
            float angleInRadians = paddles[i].currentAngle * Mathf.Deg2Rad;
            float x = transform.position.x + Mathf.Cos(angleInRadians) * radius;
            float y = transform.position.y + Mathf.Sin(angleInRadians) * radius;
            
            child.position = new Vector3(x, y, child.position.z);
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            currentRotationSpeed = clockwise ? Mathf.Abs(rotationSpeed) : -Mathf.Abs(rotationSpeed);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Gizmos.color = Color.yellow;
        
        int segments = 50;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = new Vector3(
                transform.position.x + Mathf.Cos(angle1) * radius,
                transform.position.y + Mathf.Sin(angle1) * radius,
                transform.position.z
            );
            
            Vector3 point2 = new Vector3(
                transform.position.x + Mathf.Cos(angle2) * radius,
                transform.position.y + Mathf.Sin(angle2) * radius,
                transform.position.z
            );
            
            Gizmos.DrawLine(point1, point2);
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        
        if (Application.isPlaying && paddles != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var paddle in paddles)
            {
                if (paddle.transform != null)
                {
                    Gizmos.DrawLine(transform.position, paddle.transform.position);
                }
            }
        }
    }
    
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Abs(speed);
        currentRotationSpeed = clockwise ? rotationSpeed : -rotationSpeed;
    }
    
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.1f, newRadius);
        
        if (paddles != null)
        {
            foreach (var paddle in paddles)
            {
                if (paddle.transform == null) continue;
                
                float angleInRadians = paddle.currentAngle * Mathf.Deg2Rad;
                float x = transform.position.x + Mathf.Cos(angleInRadians) * radius;
                float y = transform.position.y + Mathf.Sin(angleInRadians) * radius;
                
                paddle.transform.position = new Vector3(x, y, paddle.transform.position.z);
            }
        }
    }
    
    public void SetClockwise(bool isClockwise)
    {
        clockwise = isClockwise;
        currentRotationSpeed = clockwise ? Mathf.Abs(rotationSpeed) : -Mathf.Abs(rotationSpeed);
    }
    
    public void Stop() => enabled = false;
    public void Resume() => enabled = true;
}