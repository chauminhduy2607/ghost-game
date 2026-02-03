using UnityEngine;

/// <summary>
/// Camera follow với logic khác nhau khi ma rơi vs bay lên
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("=== TARGET ===")]
    [SerializeField] private Transform target;
    
    [Header("=== TỐC ĐỘ THEO ===")]
    [SerializeField] private float smoothSpeed = 3f;
    
    [Header("=== DEAD ZONE ===")]
    [SerializeField] private float deadZoneHeight = 0f;
    
    [Header("=== OFFSET ===")]
    [SerializeField] private float yOffset = 5f;
    
    [Header("=== GIỚI HẠN ===")]
    [SerializeField] private bool useMinY = false;
    [SerializeField] private float minY = -10f;
    [SerializeField] private bool useMaxY = false;
    [SerializeField] private float maxY = 100f;
    
    private float initialZ;
    private Rigidbody2D targetRb;
    private GhostController ghostController;
    
    void Start()
    {
        if (target == null)
        {
            GhostController ghost = FindObjectOfType<GhostController>();
            if (ghost != null)
            {
                target = ghost.transform;
                ghostController = ghost;
            }
            else
            {
                enabled = false;
                return;
            }
        }
        
        targetRb = target.GetComponent<Rigidbody2D>();
        initialZ = transform.position.z;
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        if (ghostController != null && ghostController.HitObstacle)
            return;
        
        bool isFalling = false;
        if (targetRb != null)
        {
            isFalling = targetRb.linearVelocity.y < -0.1f;
        }
        
        float targetY;
        if (isFalling)
        {
            targetY = target.position.y;
        }
        else
        {
            targetY = target.position.y + yOffset;
            
            float distanceToTarget = targetY - transform.position.y;
            
            if (Mathf.Abs(distanceToTarget) < deadZoneHeight)
            {
                // Trong dead zone
            }
        }
        
        Vector3 desiredPosition = new Vector3(
            transform.position.x,
            targetY,
            initialZ
        );
        
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        
        if (useMinY)
            smoothedPosition.y = Mathf.Max(smoothedPosition.y, minY);
        
        if (useMaxY)
            smoothedPosition.y = Mathf.Min(smoothedPosition.y, maxY);
        
        transform.position = smoothedPosition;
    }
    
    public void SetSmoothSpeed(float speed) => smoothSpeed = speed;
    public void SetDeadZone(float height) => deadZoneHeight = height;
    
    public void SnapToTarget()
    {
        if (target == null) return;
        
        Vector3 pos = transform.position;
        pos.y = target.position.y + yOffset;
        transform.position = pos;
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetRb = newTarget.GetComponent<Rigidbody2D>();
    }
}