using UnityEngine;

public class GhostAI : MonoBehaviour
{
    [Header("=== AI SETTINGS ===")]
    [SerializeField] private bool enableAI = true;
    [SerializeField] private float autoSwipeInterval = 1.2f;
    [SerializeField] private float swipeForce = 25f;
    [SerializeField] private float obstacleCheckDistance = 2.5f;
    
    [Header("=== OBSTACLE DETECTION ===")]
    [SerializeField] private string obstacleTag = "Obstacle";
    [SerializeField] private float horizontalCheckRadius = 1.0f;
    [SerializeField] private bool showDebug = true;
    
    [Header("=== HEIGHT LIMITS ===")]
    [SerializeField] private float minHeight = 0.5f;
    [SerializeField] private float emergencySwipeForce = 35f;
    
    private GhostController ghostController;
    private Rigidbody2D rb;
    private float timeSinceLastSwipe;
    private bool hasObstacleAhead;
    
    void Awake()
    {
        ghostController = GetComponent<GhostController>();
        rb = GetComponent<Rigidbody2D>();
        
        if (ghostController == null)
        {
            enabled = false;
            return;
        }
    }
    
    void Update()
    {
        if (!enableAI) return;
        if (ghostController.IsGameOver) return;
        if (ghostController.HitObstacle) return;
        
        timeSinceLastSwipe += Time.deltaTime;
        
        if (transform.position.y < minHeight)
        {
            SwipeUp(emergencySwipeForce, "EMERGENCY");
            return;
        }
        
        CheckForObstacles();
        
        if (hasObstacleAhead)
        {
            if (timeSinceLastSwipe >= autoSwipeInterval * 0.5f)
            {
                SwipeUp(swipeForce * 1.2f, "AVOID OBSTACLE");
            }
        }
        else
        {
            if (timeSinceLastSwipe >= autoSwipeInterval)
            {
                SwipeUp(swipeForce, "AUTO RISE");
            }
        }
    }
    
    void CheckForObstacles()
    {
        hasObstacleAhead = false;
        
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag(obstacleTag);
        
        foreach (GameObject obs in obstacles)
        {
            Vector3 obsPos = obs.transform.position;
            Vector3 myPos = transform.position;
            
            float verticalDist = obsPos.y - myPos.y;
            float horizontalDist = Mathf.Abs(obsPos.x - myPos.x);
            
            if (verticalDist > 0 && verticalDist < obstacleCheckDistance)
            {
                if (horizontalDist < horizontalCheckRadius)
                {
                    hasObstacleAhead = true;
                    
                    if (showDebug)
                    {
                        Debug.DrawLine(myPos, obsPos, Color.red);
                    }
                    
                    return;
                }
            }
        }
    }
    
    void SwipeUp(float force, string reason)
    {
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        
        timeSinceLastSwipe = 0f;
    }
    
    public void SetAIEnabled(bool enabled)
    {
        enableAI = enabled;
    }
    
    public void SetSwipeInterval(float interval)
    {
        autoSwipeInterval = interval;
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (!enableAI) return;
        
        Gizmos.color = hasObstacleAhead ? Color.red : Color.green;
        
        Vector3 boxCenter = transform.position + Vector3.up * (obstacleCheckDistance / 2);
        Vector3 boxSize = new Vector3(horizontalCheckRadius * 2, obstacleCheckDistance, 0.1f);
        
        Gizmos.DrawWireCube(boxCenter, boxSize);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-10, minHeight, 0), new Vector3(10, minHeight, 0));
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}