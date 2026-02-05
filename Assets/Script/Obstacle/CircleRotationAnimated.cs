using UnityEngine;
using System.Collections;

/// <summary>
/// Object quay quanh điểm tâm với hiệu ứng co giãn (Animated Version)
/// </summary>
public class CircleRotationAnimated : MonoBehaviour
{
    [Header("=== ROTATION SETTINGS ===")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float radius = 3f;
    [SerializeField] private float startAngle = 0f;
    
    [Header("=== CENTER POINT ===")]
    [SerializeField] private Transform centerPoint;
    
    [Header("=== SPRITE ROTATION ===")]
    [SerializeField] private bool rotateSprite = false;
    [SerializeField] private bool rotateTowardsCenter = true;
    [SerializeField] private float spriteRotationOffset = 90f;
    [SerializeField] private bool keepOriginalFlip = true;
    
    [Header("=== SHRINK/EXPAND ANIMATION ===")]
    [SerializeField] private bool enableShrinkExpand = true;
    [SerializeField] private float shrinkRadius = 0.5f; // Bán kính khi co lại
    [SerializeField] private float shrinkDuration = 1f; // Thời gian rút vào: 1s
    [SerializeField] private float shrinkHoldTime = 2f; // Giữ ở trong: 2s
    [SerializeField] private float expandDuration = 1f; // Thời gian đẩy ra: 1s
    [SerializeField] private float expandHoldTime = 2f; // Giữ ở ngoài: 2s
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLine = true;
    [SerializeField] private Color outerCircleColor = Color.yellow;
    [SerializeField] private Color innerCircleColor = Color.cyan;
    
    // Private variables
    private float currentAngle;
    private Vector3 centerPosition;
    private Vector3 lastPosition;
    private SpriteRenderer spriteRenderer;
    private bool originalFlipX;
    private bool originalFlipY;
    private float currentRadius;
    private Coroutine shrinkExpandCoroutine;
    
    void Start()
    {
        InitializeCenter();
        InitializeSprite();
        InitializeRadius();
        
        currentAngle = startAngle;
        UpdatePosition();
        lastPosition = transform.position;
        
        // Bắt đầu animation co giãn
        if (enableShrinkExpand)
        {
            shrinkExpandCoroutine = StartCoroutine(ShrinkExpandLoop());
        }
    }
    
    void InitializeCenter()
    {
        if (centerPoint != null)
        {
            centerPosition = centerPoint.position;
        }
        else if (transform.parent != null)
        {
            centerPosition = transform.parent.position;
        }
        else
        {
            centerPosition = transform.position;
        }
    }
    
    void InitializeSprite()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && keepOriginalFlip)
        {
            originalFlipX = spriteRenderer.flipX;
            originalFlipY = spriteRenderer.flipY;
        }
    }
    
    void InitializeRadius()
    {
        currentRadius = radius;
    }
    
    void Update()
    {
        UpdateCenter();
        UpdateRotationAngle();
        UpdatePosition();
        
        if (rotateSprite)
        {
            UpdateSpriteRotation();
        }
    }
    
    void UpdateCenter()
    {
        if (centerPoint != null)
        {
            centerPosition = centerPoint.position;
        }
        else if (transform.parent != null)
        {
            centerPosition = transform.parent.position;
        }
    }
    
    void UpdateRotationAngle()
    {
        currentAngle += rotationSpeed * Time.deltaTime;
        
        // Normalize angle to 0-360
        if (currentAngle >= 360f)
            currentAngle -= 360f;
        else if (currentAngle < 0f)
            currentAngle += 360f;
    }
    
    void LateUpdate()
    {
        if (rotateTowardsCenter)
        {
            RotatePaddleTowardsCenter();
        }
        else if (!rotateSprite)
        {
            transform.rotation = Quaternion.identity;
        }
    }
    
    void RotatePaddleTowardsCenter()
    {
        Vector3 directionToCenter = centerPosition - transform.position;
        float angle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;
        angle += spriteRotationOffset;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    void UpdatePosition()
    {
        lastPosition = transform.position;
        
        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        float x = centerPosition.x + Mathf.Cos(angleInRadians) * currentRadius;
        float y = centerPosition.y + Mathf.Sin(angleInRadians) * currentRadius;
        
        transform.position = new Vector3(x, y, transform.position.z);
    }
    
    void UpdateSpriteRotation()
    {
        Vector3 direction = transform.position - lastPosition;
        
        if (direction.magnitude > 0.001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            angle += spriteRotationOffset;
            
            if (spriteRenderer != null && keepOriginalFlip)
            {
                if (originalFlipX)
                {
                    angle += 180f;
                }
                
                spriteRenderer.flipX = originalFlipX;
                spriteRenderer.flipY = originalFlipY;
            }
            
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    // ========================================
    // SHRINK/EXPAND ANIMATION SYSTEM
    // ========================================
    
    IEnumerator ShrinkExpandLoop()
    {
        while (true)
        {
            // 1. RÚT VÀO: từ radius → shrinkRadius (1s)
            yield return StartCoroutine(AnimateRadius(radius, shrinkRadius, shrinkDuration));
            
            // 2. GIỮ Ở TRONG: 2s
            yield return new WaitForSeconds(shrinkHoldTime);
            
            // 3. ĐẨY RA: từ shrinkRadius → radius (1s)
            yield return StartCoroutine(AnimateRadius(shrinkRadius, radius, expandDuration));
            
            // 4. GIỮ Ở NGOÀI: 2s
            yield return new WaitForSeconds(expandHoldTime);
        }
    }
    
    IEnumerator AnimateRadius(float fromRadius, float toRadius, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Sử dụng animation curve để có chuyển động mượt mà
            float curveValue = animationCurve.Evaluate(t);
            currentRadius = Mathf.Lerp(fromRadius, toRadius, curveValue);
            
            yield return null;
        }
        
        currentRadius = toRadius;
    }
    
    // ========================================
    // PUBLIC API METHODS
    // ========================================
    
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.1f, newRadius);
        
        if (!enableShrinkExpand)
        {
            currentRadius = radius;
        }
        
        UpdatePosition();
    }
    
    public void SetAngle(float angle)
    {
        currentAngle = angle % 360f;
        UpdatePosition();
    }
    
    public void RandomizeStartAngle()
    {
        currentAngle = Random.Range(0f, 360f);
        UpdatePosition();
    }
    
    public void ReverseDirection()
    {
        rotationSpeed = -rotationSpeed;
    }
    
    public void Stop()
    {
        enabled = false;
        
        if (shrinkExpandCoroutine != null)
        {
            StopCoroutine(shrinkExpandCoroutine);
        }
    }
    
    public void Resume()
    {
        enabled = true;
        
        if (enableShrinkExpand && shrinkExpandCoroutine == null)
        {
            shrinkExpandCoroutine = StartCoroutine(ShrinkExpandLoop());
        }
    }
    
    public void EnableShrinkExpand(bool enable)
    {
        enableShrinkExpand = enable;
        
        if (enable && shrinkExpandCoroutine == null)
        {
            shrinkExpandCoroutine = StartCoroutine(ShrinkExpandLoop());
        }
        else if (!enable && shrinkExpandCoroutine != null)
        {
            StopCoroutine(shrinkExpandCoroutine);
            shrinkExpandCoroutine = null;
            currentRadius = radius;
        }
    }
    
    public void SetShrinkRadius(float newShrinkRadius)
    {
        shrinkRadius = Mathf.Max(0.1f, newShrinkRadius);
    }
    
    public void SetTimings(float shrinkTime, float shrinkHold, float expandTime, float expandHold)
    {
        shrinkDuration = Mathf.Max(0.1f, shrinkTime);
        shrinkHoldTime = Mathf.Max(0f, shrinkHold);
        expandDuration = Mathf.Max(0.1f, expandTime);
        expandHoldTime = Mathf.Max(0f, expandHold);
    }
    
    public float GetCurrentRadius()
    {
        return currentRadius;
    }
    
    public float GetCurrentAngle()
    {
        return currentAngle;
    }
    
    // ========================================
    // DEBUG VISUALIZATION
    // ========================================
    
    void OnDrawGizmos()
    {
        if (!showDebugLine) return;
        
        Vector3 center = Application.isPlaying ? centerPosition : 
                        (transform.parent != null ? transform.parent.position : transform.position);
        
        // Vẽ vòng tròn ngoài (radius gốc)
        Gizmos.color = outerCircleColor;
        DrawCircle(center, radius, 64);
        
        // Vẽ vòng tròn trong (shrink radius) nếu bật animation
        if (enableShrinkExpand)
        {
            Gizmos.color = innerCircleColor;
            DrawCircle(center, shrinkRadius, 64);
        }
        
        // Vẽ đường nối từ tâm đến paddle
        Gizmos.color = Color.green;
        Gizmos.DrawLine(center, transform.position);
        
        // Vẽ điểm tâm
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.15f);
        
        // Vẽ vị trí hiện tại của paddle
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
    
    void DrawCircle(Vector3 center, float circleRadius, int segments)
    {
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = new Vector3(
                center.x + Mathf.Cos(angle1) * circleRadius,
                center.y + Mathf.Sin(angle1) * circleRadius,
                center.z
            );
            
            Vector3 point2 = new Vector3(
                center.x + Mathf.Cos(angle2) * circleRadius,
                center.y + Mathf.Sin(angle2) * circleRadius,
                center.z
            );
            
            Gizmos.DrawLine(point1, point2);
        }
    }
    
    // ========================================
    // EDITOR HELPERS
    // ========================================
    
    void OnValidate()
    {
        // Đảm bảo các giá trị hợp lệ khi thay đổi trong Inspector
        radius = Mathf.Max(0.1f, radius);
        shrinkRadius = Mathf.Max(0.1f, shrinkRadius);
        shrinkDuration = Mathf.Max(0.1f, shrinkDuration);
        expandDuration = Mathf.Max(0.1f, expandDuration);
        shrinkHoldTime = Mathf.Max(0f, shrinkHoldTime);
        expandHoldTime = Mathf.Max(0f, expandHoldTime);
    }
}