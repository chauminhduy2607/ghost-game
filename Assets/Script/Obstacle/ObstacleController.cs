using UnityEngine;

/// <summary>
/// Connector đơn giản cho obstacle components
/// </summary>
[RequireComponent(typeof(ObstacleMovement))]
public class ObstacleController : MonoBehaviour
{
    private ObstacleMovement movement;
    
    public float CurrentX => movement != null ? movement.CurrentX : 0f;
    public float TargetX => movement != null ? movement.TargetX : 0f;
    
    void Awake()
    {
        movement = GetComponent<ObstacleMovement>();
        
        if (movement == null)
        {
            movement = gameObject.AddComponent<ObstacleMovement>();
        }
    }
    
    public void RandomizeOnly()
    {
        if (movement != null)
        {
            movement.Randomize();
        }
    }
    
    public void SetPositionY(float y)
    {
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }
    
    public void ResetObstacle()
    {
        if (movement != null)
        {
            movement.ResetPosition();
        }
    }
    
    public void SetSpeed(float speed)
    {
        if (movement != null)
        {
            movement.SetSpeed(speed);
        }
    }
    
    public void Stop()
    {
        if (movement != null)
        {
            movement.Stop();
        }
    }
    
    public void Resume()
    {
        if (movement != null)
        {
            movement.Resume();
        }
    }
}