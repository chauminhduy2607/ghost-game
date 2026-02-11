using UnityEngine;

/// <summary>
/// Camera follow - Ghost luôn cố định ở phía dưới màn hình (~1/4)
/// Freeze hoàn toàn khi chạm obstacle hoặc chết
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("=== TARGET ===")]
    [SerializeField] private Transform target;
    
    [Header("=== VỊ TRÍ GHOST TRÊN MÀN HÌNH ===")]
    [Tooltip("0 = dưới cùng, 1 = trên cùng. 0.33 = 1/3 từ dưới")]
    [SerializeField] private float ghostScreenYRatio = 0.33f;
    
    [Header("=== TỐC ĐỘ THEO ===")]
    [SerializeField] private float smoothSpeed = 8f;

    [Header("=== GIỚI HẠN ===")]
    [SerializeField] private bool useMinY = false;
    [SerializeField] private float minY = -10f;
    
    private float initialZ;
    private GhostController ghostController;
    private Camera cam;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        
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
        else
        {
            ghostController = target.GetComponent<GhostController>();
        }
        
        initialZ = transform.position.z;
        SnapToTarget();
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Freeze ngay khi chạm obstacle, không chờ game over
        if (ghostController != null && (ghostController.HitObstacle || ghostController.IsGameOver))
            return;
        
        // Tính camY sao cho ghost luôn ở ghostScreenYRatio
        // ghostWorldY = camY - halfHeight + ratio * fullHeight
        // => camY = ghostWorldY + halfHeight - ratio * fullHeight
        float halfHeight = cam.orthographicSize;
        float targetCamY = target.position.y + halfHeight - ghostScreenYRatio * (halfHeight * 2f);
        
        Vector3 desired = new Vector3(transform.position.x, targetCamY, initialZ);
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        
        if (useMinY)
            smoothed.y = Mathf.Max(smoothed.y, minY);
        
        transform.position = smoothed;
    }
    
    public void SnapToTarget()
    {
        if (target == null || cam == null) return;
        float halfHeight = cam.orthographicSize;
        float targetCamY = target.position.y + halfHeight - ghostScreenYRatio * (halfHeight * 2f);
        transform.position = new Vector3(transform.position.x, targetCamY, initialZ);
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ghostController = newTarget.GetComponent<GhostController>();
    }
    
    public void SetSmoothSpeed(float speed) => smoothSpeed = speed;
}