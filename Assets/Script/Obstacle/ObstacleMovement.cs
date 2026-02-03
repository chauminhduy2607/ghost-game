using UnityEngine;

/// <summary>
/// Di chuyển vật cản qua 9 line, đổi hướng khi chạm viền
/// </summary>
public class ObstacleMovement : MonoBehaviour
{
    [Header("=== DI CHUYỂN NGANG ===")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 4f;
    
    [Header("=== LINE SETTINGS ===")]
    [SerializeField] private bool randomStartLine = true;
    [SerializeField] private bool randomStartDirection = true;
    
    private LineSystem lineSystem;
    private int currentLineIndex;
    private float currentTargetX;
    private bool movingRight = true;
    private float actualSpeed;
    
    public float CurrentX => transform.position.x;
    public float TargetX => currentTargetX;
    
    void Awake()
    {
        lineSystem = LineSystem.Instance;
        
        if (lineSystem == null)
        {
            enabled = false;
            return;
        }
        
        RandomizeMovement();
        SetRandomStartPosition();
    }
    
    void Update()
    {
        MoveToTarget();
    }
    
    void RandomizeMovement()
    {
        actualSpeed = moveSpeed;
    }
    
    void SetRandomStartPosition()
    {
        if (randomStartLine)
        {
            currentLineIndex = Random.Range(0, lineSystem.GetTotalLines());
        }
        else
        {
            currentLineIndex = lineSystem.GetTotalLines() / 2;
        }
        
        if (randomStartDirection)
        {
            movingRight = Random.value > 0.5f;
        }
        
        Vector3 pos = transform.position;
        pos.x = lineSystem.GetLineX(currentLineIndex);
        transform.position = pos;
        
        SetNextTarget();
    }
    
    void SetNextTarget()
    {
        int nextLineIndex;
        
        if (movingRight)
        {
            nextLineIndex = currentLineIndex + 1;
            
            if (nextLineIndex >= lineSystem.GetTotalLines())
            {
                nextLineIndex = lineSystem.GetTotalLines() - 1;
                movingRight = false;
            }
        }
        else
        {
            nextLineIndex = currentLineIndex - 1;
            
            if (nextLineIndex < 0)
            {
                nextLineIndex = 0;
                movingRight = true;
            }
        }
        
        currentLineIndex = nextLineIndex;
        currentTargetX = lineSystem.GetLineX(currentLineIndex);
    }
    
    void MoveToTarget()
    {
        Vector3 pos = transform.position;
        float direction = movingRight ? 1f : -1f;
        
        pos.x += direction * actualSpeed * Time.deltaTime;
        
        bool reachedTarget = false;
        
        if (movingRight)
        {
            if (pos.x >= currentTargetX)
            {
                pos.x = currentTargetX;
                reachedTarget = true;
            }
        }
        else
        {
            if (pos.x <= currentTargetX)
            {
                pos.x = currentTargetX;
                reachedTarget = true;
            }
        }
        
        transform.position = pos;
        
        if (reachedTarget)
        {
            SetNextTarget();
        }
    }
    
    public void Randomize() => RandomizeMovement();
    public void SetSpeed(float speed)
    {
        moveSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        actualSpeed = moveSpeed;
    }
    public void ResetPosition() => SetRandomStartPosition();
    public void Stop() => enabled = false;
    public void Resume() => enabled = true;
    public int GetCurrentLine() => currentLineIndex;
    public bool IsMovingRight() => movingRight;
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || lineSystem == null) return;
        
        Gizmos.color = Color.cyan;
        Vector3 targetPos = new Vector3(currentTargetX, transform.position.y, 0f);
        Gizmos.DrawWireSphere(targetPos, 0.2f);
        Gizmos.DrawLine(transform.position, targetPos);
        
        Vector3 arrowDir = movingRight ? Vector3.right : Vector3.left;
        Gizmos.color = movingRight ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, arrowDir * 0.3f);
    }
}