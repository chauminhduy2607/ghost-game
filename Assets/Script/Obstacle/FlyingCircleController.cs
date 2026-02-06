using UnityEngine;

/// <summary>
/// Quản lý Flying Circle với rotation tự động - HỖ TRỢ NHIỀU PADDLE
/// </summary>
public class FlyingCircleController : MonoBehaviour
{
    [Header("=== ROTATION ===")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float radius = 3f;
    [SerializeField] private bool clockwise = true; // CHECKBOX MỚI - tick = xuôi, không tick = ngược
    
    [Header("=== AUTO SETUP ===")]
    [SerializeField] private bool autoSetup = true;
    [SerializeField] private bool distributeEvenly = true; // Tự động chia đều 360°
    
    [Header("=== MANUAL ANGLES (nếu không dùng distributeEvenly) ===")]
    [SerializeField] private float[] customStartAngles = new float[] { 0f, 180f }; // Góc thủ công
    
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
        // Tìm tất cả CircleRotation trong children
        circles = GetComponentsInChildren<CircleRotation>();
        
        // Nếu không có, tự động thêm vào các child
        if (circles.Length == 0)
        {
            int childCount = transform.childCount;
            circles = new CircleRotation[childCount];
            
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                CircleRotation rotation = child.GetComponent<CircleRotation>();
                
                if (rotation == null)
                {
                    rotation = child.gameObject.AddComponent<CircleRotation>();
                }
                
                circles[i] = rotation;
            }
        }
        
        // Tính tốc độ thực tế dựa trên chiều xoay
        float actualSpeed = clockwise ? rotationSpeed : -rotationSpeed;
        
        // Setup cho từng paddle
        if (distributeEvenly)
        {
            // CHIA ĐỀU: 360° / số lượng paddle
            float angleStep = 360f / circles.Length;
            
            for (int i = 0; i < circles.Length; i++)
            {
                if (circles[i] != null)
                {
                    circles[i].SetRotationSpeed(actualSpeed);
                    circles[i].SetRadius(radius);
                    circles[i].SetAngle(i * angleStep); // 0°, 90°, 180°, 270° (nếu 4 paddle)
                }
            }
        }
        else
        {
            // DÙNG GÓC THỦ CÔNG
            for (int i = 0; i < circles.Length; i++)
            {
                if (circles[i] != null)
                {
                    circles[i].SetRotationSpeed(actualSpeed);
                    circles[i].SetRadius(radius);
                    
                    if (i < customStartAngles.Length)
                    {
                        circles[i].SetAngle(customStartAngles[i]);
                    }
                    else
                    {
                        circles[i].SetAngle(0f);
                    }
                }
            }
        }
        
        Debug.Log($"[FlyingCircle] Setup {circles.Length} paddles với góc mỗi cái: {360f / circles.Length}° - Chiều: {(clockwise ? "Xuôi" : "Ngược")}");
    }
    
    // Cập nhật chiều xoay real-time khi thay đổi trong Inspector
    void OnValidate()
    {
        if (Application.isPlaying && circles != null)
        {
            float actualSpeed = clockwise ? Mathf.Abs(rotationSpeed) : -Mathf.Abs(rotationSpeed);
            SetRotationSpeed(actualSpeed);
        }
    }
    
    public void SetPositionY(float y)
    {
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }
    
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Abs(speed); // Lưu giá trị dương
        float actualSpeed = clockwise ? rotationSpeed : -rotationSpeed;
        
        if (circles != null)
        {
            foreach (var circle in circles)
            {
                if (circle != null)
                    circle.SetRotationSpeed(actualSpeed);
            }
        }
    }
    
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
    
    public void SetClockwise(bool isClockwise)
    {
        clockwise = isClockwise;
        float actualSpeed = clockwise ? Mathf.Abs(rotationSpeed) : -Mathf.Abs(rotationSpeed);
        
        if (circles != null)
        {
            foreach (var circle in circles)
            {
                if (circle != null)
                    circle.SetRotationSpeed(actualSpeed);
            }
        }
    }
    
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
    
    public void RedistributeEvenly()
    {
        if (circles == null) return;
        
        float angleStep = 360f / circles.Length;
        
        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i] != null)
            {
                circles[i].SetAngle(i * angleStep);
            }
        }
    }
    
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