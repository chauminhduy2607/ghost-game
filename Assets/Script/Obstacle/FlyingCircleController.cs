using UnityEngine;

/// <summary>
/// Quản lý Flying Circle với rotation tự động
/// </summary>
public class FlyingCircleController : MonoBehaviour
{
    [Header("=== ROTATION ===")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float radius = 1f;
    
    [Header("=== AUTO SETUP ===")]
    [SerializeField] private bool autoSetup = true;
    [SerializeField] private float circle1StartAngle = 0f;
    [SerializeField] private float circle2StartAngle = 180f;
    
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
        circles = GetComponentsInChildren<CircleRotation>();
        
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
    }
    
    public void SetPositionY(float y)
    {
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }
    
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