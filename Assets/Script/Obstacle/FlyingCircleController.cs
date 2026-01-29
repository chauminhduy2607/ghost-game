using UnityEngine;

/// <summary>
/// ⭐ QUẢN LÝ FLYING CIRCLE
/// - Tự động setup 2 Circle con quay tròn
/// - Di chuyển xuống theo camera (parallax)
/// - Gắn vào FlyingCircle (parent)
/// </summary>
public class FlyingCircleController : MonoBehaviour
{
    [Header("=== ROTATION ===")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float radius = 1f;
    
    [Header("=== AUTO SETUP ===")]
    [Tooltip("Tự động thêm CircleRotation vào các con")]
    [SerializeField] private bool autoSetup = true;
    
    [Tooltip("Góc bắt đầu của Circle 1")]
    [SerializeField] private float circle1StartAngle = 0f;
    
    [Tooltip("Góc bắt đầu của Circle 2")]
    [SerializeField] private float circle2StartAngle = 180f;
    
    // Private
    private CircleRotation[] circles;
    
    void Start()
    {
        if (autoSetup)
        {
            SetupCircles();
        }
    }
    
    void SetupCircles()
    {
        // Tìm tất cả CircleRotation trong con
        circles = GetComponentsInChildren<CircleRotation>();
        
        if (circles.Length == 0)
        {
            // Tự động thêm CircleRotation vào các con
            int childCount = transform.childCount;
            circles = new CircleRotation[childCount];
            
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                CircleRotation rotation = child.GetComponent<CircleRotation>();
                
                if (rotation == null)
                {
                    rotation = child.gameObject.AddComponent<CircleRotation>();
                    Debug.Log($"➕ Đã thêm CircleRotation vào {child.name}");
                }
                
                circles[i] = rotation;
            }
        }
        
        // Setup từng circle
        if (circles.Length >= 1)
        {
            circles[0].SetRotationSpeed(rotationSpeed);
            circles[0].SetRadius(radius);
            circles[0].SetAngle(circle1StartAngle);
        }
        
        if (circles.Length >= 2)
        {
            circles[1].SetRotationSpeed(rotationSpeed);
            circles[1].SetRadius(radius);
            circles[1].SetAngle(circle2StartAngle);
        }
        
        Debug.Log($"✅ FlyingCircle: Setup {circles.Length} circles");
    }
    
    // ==================== PUBLIC METHODS ====================
    
    /// <summary>
    /// Đặt vị trí Y
    /// </summary>
    public void SetPositionY(float y)
    {
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }
    
    /// <summary>
    /// Đặt tốc độ quay cho tất cả circles
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
        
        if (circles != null)
        {
            foreach (var circle in circles)
            {
                if (circle != null)
                    circle.SetRotationSpeed(speed);
            }
        }
    }
    
    /// <summary>
    /// Đặt bán kính cho tất cả circles
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        
        if (circles != null)
        {
            foreach (var circle in circles)
            {
                if (circle != null)
                    circle.SetRadius(newRadius);
            }
        }
    }
    
    /// <summary>
    /// Random góc bắt đầu
    /// </summary>
    public void RandomizeAngles()
    {
        if (circles != null)
        {
            foreach (var circle in circles)
            {
                if (circle != null)
                    circle.RandomizeStartAngle();
            }
        }
    }
    
    /// <summary>
    /// Dừng quay
    /// </summary>
    public void Stop()
    {
        if (circles != null)
        {
            foreach (var circle in circles)
            {
                if (circle != null)
                    circle.Stop();
            }
        }
    }
    
    /// <summary>
    /// Tiếp tục quay
    /// </summary>
    public void Resume()
    {
        if (circles != null)
        {
            foreach (var circle in circles)
            {
                if (circle != null)
                    circle.Resume();
            }
        }
    }
}