using UnityEngine;

/// <summary>
/// Object quay quanh điểm tâm tạo hình tròn - XOAY NHƯ KIM ĐỒNG HỒ
/// </summary>
public class CircleRotation : MonoBehaviour
{
    [Header("=== ROTATION SETTINGS ===")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float radius = 1f;
    [SerializeField] private float startAngle = 0f;
    
    [Header("=== CENTER POINT ===")]
    [SerializeField] private Transform centerPoint;
    
    [Header("=== SPRITE ROTATION ===")]
    [SerializeField] private bool rotateSprite = false;
    [SerializeField] private bool rotateTowardsCenter = true; // MỚI - Xoay về tâm như kim đồng hồ
    [SerializeField] private float spriteRotationOffset = 90f;
    [SerializeField] private bool keepOriginalFlip = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLine = true;
    
    private float currentAngle;
    private Vector3 centerPosition;
    private Vector3 lastPosition;
    private SpriteRenderer spriteRenderer;
    private bool originalFlipX;
    private bool originalFlipY;
    
    void Start()
    {
        // ===== THÊM ĐOẠN NÀY Ở ĐẦU HÀM START =====
        // Tắt tự động nếu có parent FlyingCircleController
        if (transform.parent != null && transform.parent.GetComponent<FlyingCircleController>() != null)
        {
            // Vẫn phải setup centerPosition trước khi tắt
            if (centerPoint == null && transform.parent != null)
            {
                centerPosition = transform.parent.position;
            }
            else if (centerPoint != null)
            {
                centerPosition = centerPoint.position;
            }
            else
            {
                centerPosition = transform.position;
            }
            
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && keepOriginalFlip)
            {
                originalFlipX = spriteRenderer.flipX;
                originalFlipY = spriteRenderer.flipY;
            }
            
            currentAngle = startAngle;
            UpdatePosition();
            lastPosition = transform.position;
            
            enabled = false; // Tắt Update của script này
            return; // Dừng Start() tại đây
        }
        // ==========================================
        
        // CODE CŨ GIỮ NGUYÊN
        if (centerPoint == null && transform.parent != null)
        {
            centerPosition = transform.parent.position;
        }
        else if (centerPoint != null)
        {
            centerPosition = centerPoint.position;
        }
        else
        {
            centerPosition = transform.position;
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && keepOriginalFlip)
        {
            originalFlipX = spriteRenderer.flipX;
            originalFlipY = spriteRenderer.flipY;
        }
        
        currentAngle = startAngle;
        UpdatePosition();
        lastPosition = transform.position;
    }
    
    void Update()
    {
        if (centerPoint != null)
        {
            centerPosition = centerPoint.position;
        }
        else if (transform.parent != null)
        {
            centerPosition = transform.parent.position;
        }
        
        currentAngle += rotationSpeed * Time.deltaTime;
        
        if (currentAngle >= 360f)
            currentAngle -= 360f;
        else if (currentAngle < 0f)
            currentAngle += 360f;
        
        UpdatePosition();
        
        if (rotateSprite)
        {
            UpdateSpriteRotation();
        }
    }
    
    void LateUpdate()
    {
        if (rotateTowardsCenter)
        {
            // XOAY PADDLE ĐỂ HƯỚNG VỀ TÂM (như kim đồng hồ)
            Vector3 directionToCenter = centerPosition - transform.position;
            float angle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;
            
            // Offset để căn chỉnh sprite (tùy hướng sprite gốc)
            angle += spriteRotationOffset;
            
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else if (!rotateSprite)
        {
            // Giữ nguyên rotation ban đầu (nằm ngang)
            transform.rotation = Quaternion.identity;
        }
    }
    
    void UpdatePosition()
    {
        lastPosition = transform.position;
        
        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        float x = centerPosition.x + Mathf.Cos(angleInRadians) * radius;
        float y = centerPosition.y + Mathf.Sin(angleInRadians) * radius;
        
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
    
    void OnDrawGizmos()
    {
        if (!showDebugLine) return;
        
        Vector3 center = Application.isPlaying ? centerPosition : 
                        (transform.parent != null ? transform.parent.position : transform.position);
        
        Gizmos.color = Color.yellow;
        
        int segments = 50;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = new Vector3(
                center.x + Mathf.Cos(angle1) * radius,
                center.y + Mathf.Sin(angle1) * radius,
                center.z
            );
            
            Vector3 point2 = new Vector3(
                center.x + Mathf.Cos(angle2) * radius,
                center.y + Mathf.Sin(angle2) * radius,
                center.z
            );
            
            Gizmos.DrawLine(point1, point2);
        }
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, transform.position);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.1f);
    }
    
    public void SetRotationSpeed(float speed) => rotationSpeed = speed;
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.1f, newRadius);
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
    public void ReverseDirection() => rotationSpeed = -rotationSpeed;
    public void Stop() => enabled = false;
    public void Resume() => enabled = true;
}