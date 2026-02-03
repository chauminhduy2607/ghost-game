using UnityEngine;

/// <summary>
/// Quản lý môi trường game với giới hạn đáy
/// </summary>
public class GameEnvironment : MonoBehaviour
{
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghost;
    
    [Header("=== GIỚI HẠN ĐÁY ===")]
    [SerializeField] private float groundY = -4f;
    [SerializeField] private bool constrainX = true;
    
    private Camera mainCamera;
    private float objectHeight;
    private float maxHeightReached = 0f;

    void Start()
    {
        mainCamera = Camera.main;
        
        if (ghost == null)
        {
            ghost = Object.FindFirstObjectByType<GhostController>();
            if (ghost == null)
            {
                enabled = false;
                return;
            }
        }
        
        SpriteRenderer spriteRenderer = ghost.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            objectHeight = spriteRenderer.bounds.extents.y;
        }
        
        if (constrainX)
        {
            ghost.Rigidbody.constraints = RigidbodyConstraints2D.FreezePositionX;
        }
    }
    
    void FixedUpdate()
    {
        CheckGround();
        TrackMaxHeight();
    }
    
    void CheckGround()
    {
        Vector3 pos = ghost.transform.position;
        Vector2 vel = ghost.Velocity;
        
        float minY = groundY + objectHeight;
        
        if (pos.y <= minY)
        {
            pos.y = minY;
            
            if (vel.y < -0.1f && !ghost.IsGroundSquashing)
            {
                ghost.TriggerGroundSquash();
            }
            
            vel.y = Mathf.Max(vel.y, 0);
            
            ghost.transform.position = pos;
            ghost.SetVelocity(vel);
        }
    }
    
    void TrackMaxHeight()
    {
        float currentHeight = ghost.transform.position.y;
        
        if (currentHeight > maxHeightReached)
        {
            maxHeightReached = currentHeight;
        }
    }
    
    public void SetGroundY(float newGroundY) => groundY = newGroundY;
    
    public void ResetMaxHeight()
    {
        maxHeightReached = ghost.transform.position.y;
    }
    
    public float GetMaxHeight() => maxHeightReached;
}