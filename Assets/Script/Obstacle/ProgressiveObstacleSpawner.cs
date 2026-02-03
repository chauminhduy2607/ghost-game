using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Progressive spawner với auto speed up
/// </summary>
public class ProgressiveObstacleSpawner : MonoBehaviour
{
    [Header("=== PREFABS ===")]
    [SerializeField] private GameObject fireLinePrefab;
    [SerializeField] private GameObject flyingCirclePrefab;
    [SerializeField] private GameObject flyingFirePrefab;
    
    [Header("=== PATTERN ===")]
    [SerializeField] private int fireLinesPerGroup = 4;
    [SerializeField] private int circlesPerGroup = 1;
    [SerializeField] private int flyingFiresPerGroup = 4;
    
    [Header("=== SPAWN POSITION ===")]
    [SerializeField] private float baseSpawnDistance = 25f;
    [SerializeField] private float velocityMultiplier = 3f;
    [SerializeField] private float maxSpawnDistance = 60f;
    
    [Header("=== SPACING ===")]
    [SerializeField] private float spacingY = 5.0f;
    [SerializeField] private float groupSpacing = 15f;
    
    [Header("=== CIRCLE SETTINGS ===")]
    [SerializeField] private float circleRadius = 2.5f;
    [SerializeField] private float circleToFireSpacing = 3f;
    [SerializeField] private bool spawnFireAboveCircle = false;
    [SerializeField] private float startY = 5f;
    
    [Header("=== SPEED PROGRESSION ===")]
    [SerializeField] private float initialSpeed = 1.0f;
    [SerializeField] private float speedIncrement = 0.2f;
    [SerializeField] private float maxSpeed = 5.0f;
    [SerializeField] private int obstaclesPerSpeedIncrease = 3;
    
    [Header("=== ROTATION SPEED ===")]
    [SerializeField] private float initialRotationSpeed = 120f;
    [SerializeField] private float rotationSpeedIncrement = 20f;
    [SerializeField] private float maxRotationSpeed = 360f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;
    
    [Header("=== USE EXISTING ===")]
    [SerializeField] private bool useExistingObstacles = true;
    
    [Header("=== LOOP ===")]
    [SerializeField] private float loopThreshold = 1.5f;
    [SerializeField] private bool randomizeOnLoop = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showGizmos = false;
    
    private List<GameObject> allObstacles = new List<GameObject>();
    private float screenHeight;
    private int totalLooped = 0;
    private int currentWave = 0;
    private float currentSpeed;
    private float currentRotationSpeed;
    private HashSet<GameObject> passedObstacles = new HashSet<GameObject>();
    private int obstaclesPassed = 0;
    private float lastSpeedIncreaseAt = 0;
    
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (player == null)
        {
            GhostController ghost = FindObjectOfType<GhostController>();
            if (ghost != null)
                player = ghost.transform;
        }
        
        screenHeight = mainCamera.orthographicSize * 2f;
        
        if (player != null && startY == 0f)
        {
            startY = player.position.y + screenHeight * 0.3f;
        }
        
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        
        if (useExistingObstacles)
        {
            UseExistingObstacles();
        }
        else
        {
            SpawnInitialPattern();
        }
        
        DisableCircleRotations();
        ArrangeAllObstaclesInPattern();
        StartCoroutine(EnableCircleRotationsDelayed());
        Invoke("ForceEnableAllCircles", 0.5f);
    }
    
    void ForceEnableAllCircles()
    {
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>(true);
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = true;
        }
    }
    
    void DisableCircleRotations()
    {
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>();
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = false;
        }
    }
    
    System.Collections.IEnumerator EnableCircleRotationsDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        CircleRotation[] rotations = FindObjectsOfType<CircleRotation>();
        foreach (CircleRotation rotation in rotations)
        {
            rotation.enabled = true;
        }
    }
    
    void Update()
    {
        CheckAndLoopObstacles();
        CheckObstaclesPassed();
    }
    
    void CheckObstaclesPassed()
    {
        if (player == null || allObstacles.Count == 0) return;
        
        float playerY = player.position.y;
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            if (!passedObstacles.Contains(obstacle))
            {
                if (playerY > obstacle.transform.position.y + 2f)
                {
                    passedObstacles.Add(obstacle);
                    obstaclesPassed++;
                    
                    if (obstaclesPassed - lastSpeedIncreaseAt >= obstaclesPerSpeedIncrease)
                    {
                        IncreaseSpeed();
                        lastSpeedIncreaseAt = obstaclesPassed;
                    }
                }
            }
        }
    }
    
    void IncreaseSpeed()
    {
        currentSpeed = Mathf.Min(currentSpeed + speedIncrement, maxSpeed);
        currentRotationSpeed = Mathf.Min(currentRotationSpeed + rotationSpeedIncrement, maxRotationSpeed);
        ApplySpeedToAllObstacles();
    }
    
    void ApplySpeedToAllObstacles()
    {
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            ApplyCurrentSpeedToObstacle(obstacle);
        }
    }
    
    void UseExistingObstacles()
    {
        allObstacles.Clear();
        
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            
            string name = child.name.ToLower();
            
            if (name.Contains("fire") && name.Contains("line"))
            {
                SetupFireLine(child.gameObject, currentSpeed);
                allObstacles.Add(child.gameObject);
            }
            else if (name.Contains("circle") && name.Contains("flying"))
            {
                SetupFlyingCircle(child.gameObject, currentRotationSpeed);
                allObstacles.Add(child.gameObject);
            }
            else if (name.Contains("flying") && name.Contains("fire"))
            {
                SetupFlyingFire(child.gameObject, currentSpeed);
                allObstacles.Add(child.gameObject);
            }
        }
    }
    
    void SpawnInitialPattern()
    {
        if (fireLinePrefab == null || flyingCirclePrefab == null) return;
        
        allObstacles.Clear();
        int spawnCounter = 0;
        
        for (int group = 0; group < 3; group++)
        {
            for (int i = 0; i < fireLinesPerGroup; i++)
            {
                GameObject fireLine = Instantiate(fireLinePrefab, transform);
                fireLine.name = $"Fire&Line_{spawnCounter++}";
                SetupFireLine(fireLine, currentSpeed);
                allObstacles.Add(fireLine);
            }
            
            for (int i = 0; i < circlesPerGroup; i++)
            {
                GameObject circle = Instantiate(flyingCirclePrefab, transform);
                circle.name = $"FlyingCircle_{spawnCounter++}";
                SetupFlyingCircle(circle, currentRotationSpeed);
                allObstacles.Add(circle);
            }
            
            if (flyingFirePrefab != null)
            {
                for (int i = 0; i < flyingFiresPerGroup; i++)
                {
                    GameObject flyingFire = Instantiate(flyingFirePrefab, transform);
                    flyingFire.name = $"FlyingFire_{spawnCounter++}";
                    SetupFlyingFire(flyingFire, currentSpeed);
                    allObstacles.Add(flyingFire);
                }
            }
        }
    }
    
    void SetupFireLine(GameObject obj, float speed)
    {
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner == null)
            spawner = obj.AddComponent<ObstacleSpawner>();
        
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper != null)
        {
            looper.enabled = false;
        }
        
        StartCoroutine(SetFireLineSpeed(obj, speed));
    }
    
    System.Collections.IEnumerator SetFireLineSpeed(GameObject fireLine, float speed)
    {
        yield return null;
        
        ObstacleSpawner spawner = fireLine.GetComponent<ObstacleSpawner>();
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (GameObject obstacle in spawner.SpawnedObstacles)
            {
                if (obstacle == null) continue;
                
                ObstacleMovement movement = obstacle.GetComponent<ObstacleMovement>();
                if (movement != null)
                {
                    movement.SetSpeed(speed);
                }
            }
        }
    }
    
    void SetupFlyingCircle(GameObject obj, float rotSpeed)
    {
        FlyingCircleController controller = obj.GetComponent<FlyingCircleController>();
        if (controller == null)
            controller = obj.AddComponent<FlyingCircleController>();
        
        controller.SetRotationSpeed(rotSpeed);
    }
    
    void SetupFlyingFire(GameObject obj, float speed)
    {
        ObstacleSpawner spawner = obj.GetComponent<ObstacleSpawner>();
        if (spawner == null)
            spawner = obj.AddComponent<ObstacleSpawner>();
        
        ObstacleLooper looper = obj.GetComponent<ObstacleLooper>();
        if (looper != null)
        {
            looper.enabled = false;
        }
        
        StartCoroutine(SetFlyingFireSpeed(obj, speed));
    }
    
    System.Collections.IEnumerator SetFlyingFireSpeed(GameObject flyingFire, float speed)
    {
        yield return null;
        
        ObstacleSpawner spawner = flyingFire.GetComponent<ObstacleSpawner>();
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (GameObject obstacle in spawner.SpawnedObstacles)
            {
                if (obstacle == null) continue;
                
                ObstacleMovement movement = obstacle.GetComponent<ObstacleMovement>();
                if (movement != null)
                {
                    movement.SetSpeed(speed);
                }
            }
        }
    }
    
    void ArrangeAllObstaclesInPattern()
    {
        if (allObstacles.Count == 0) return;
        
        List<GameObject> fireLineContainers = new List<GameObject>();
        List<GameObject> circleContainers = new List<GameObject>();
        List<GameObject> flyingFireContainers = new List<GameObject>();
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            string name = obstacle.name.ToLower();
            
            if (name.Contains("flying") && name.Contains("fire"))
            {
                flyingFireContainers.Add(obstacle);
            }
            else if (name.Contains("flying") && name.Contains("circle"))
            {
                circleContainers.Add(obstacle);
            }
            else if (name.Contains("fire") && name.Contains("line"))
            {
                fireLineContainers.Add(obstacle);
            }
        }
        
        float currentY = startY;
        int fireIndex = 0;
        int circleIndex = 0;
        int flyingFireIndex = 0;
        
        int totalGroups = Mathf.Max(
            Mathf.CeilToInt((float)fireLineContainers.Count / fireLinesPerGroup),
            Mathf.Max(
                Mathf.CeilToInt((float)circleContainers.Count / circlesPerGroup),
                Mathf.CeilToInt((float)flyingFireContainers.Count / flyingFiresPerGroup)
            )
        );
        
        for (int groupIndex = 0; groupIndex < totalGroups; groupIndex++)
        {
            for (int f = 0; f < fireLinesPerGroup && fireIndex < fireLineContainers.Count; f++)
            {
                Vector3 pos = fireLineContainers[fireIndex].transform.position;
                pos.y = currentY;
                fireLineContainers[fireIndex].transform.position = pos;
                currentY += spacingY;
                fireIndex++;
            }
            
            currentY += groupSpacing;
            
            float circleCenterY = currentY;
            
            for (int c = 0; c < circlesPerGroup && circleIndex < circleContainers.Count; c++)
            {
                Vector3 pos = circleContainers[circleIndex].transform.position;
                pos.y = circleCenterY;
                circleContainers[circleIndex].transform.position = pos;
                circleIndex++;
            }
            
            float flyingFireStartY;
            
            if (spawnFireAboveCircle)
            {
                float circleTopEdge = circleCenterY + circleRadius;
                flyingFireStartY = circleTopEdge + circleToFireSpacing;
            }
            else
            {
                float circleBottomEdge = circleCenterY - circleRadius;
                flyingFireStartY = circleBottomEdge - circleToFireSpacing;
            }
            
            currentY = flyingFireStartY;
            
            for (int ff = 0; ff < flyingFiresPerGroup && flyingFireIndex < flyingFireContainers.Count; ff++)
            {
                Vector3 pos = flyingFireContainers[flyingFireIndex].transform.position;
                pos.y = currentY;
                flyingFireContainers[flyingFireIndex].transform.position = pos;
                currentY += spacingY;
                flyingFireIndex++;
            }
            
            currentY += groupSpacing;
        }
    }
    
    void CheckAndLoopObstacles()
    {
        if (allObstacles.Count == 0 || player == null) return;
        
        float ghostY = player.position.y;
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        
        GhostController ghost = player.GetComponent<GhostController>();
        float ghostVelocityY = ghost != null ? ghost.Velocity.y : 0f;
        
        float dynamicSpawnDistance = baseSpawnDistance + (Mathf.Abs(ghostVelocityY) * velocityMultiplier);
        dynamicSpawnDistance = Mathf.Min(dynamicSpawnDistance, maxSpawnDistance);
        
        float loopLine = cameraBottomEdge - (screenHeight * loopThreshold);
        
        float lowestY = float.MaxValue;
        float highestY = float.MinValue;
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            float y = obstacle.transform.position.y;
            
            if (y < lowestY)
                lowestY = y;
            if (y > highestY)
                highestY = y;
        }
        
        float requiredHighestY = ghostY + dynamicSpawnDistance;
        
        bool needLoopBecauseBehind = lowestY < loopLine;
        bool needLoopBecauseAhead = highestY < requiredHighestY;
        
        if (needLoopBecauseBehind || needLoopBecauseAhead)
        {
            LoopOneCompleteGroup(loopLine, highestY);
        }
    }
    
    void LoopOneCompleteGroup(float loopLine, float currentHighestY)
    {
        List<GameObject> fireLines = new List<GameObject>();
        List<GameObject> circles = new List<GameObject>();
        List<GameObject> flyingFires = new List<GameObject>();
        
        foreach (GameObject obstacle in allObstacles)
        {
            if (obstacle == null) continue;
            
            string name = obstacle.name.ToLower();
            
            if (name.Contains("flying") && name.Contains("fire"))
            {
                flyingFires.Add(obstacle);
            }
            else if (name.Contains("flying") && name.Contains("circle"))
            {
                circles.Add(obstacle);
            }
            else if (name.Contains("fire") && name.Contains("line"))
            {
                fireLines.Add(obstacle);
            }
        }
        
        fireLines = fireLines.OrderBy(o => o.transform.position.y).ToList();
        circles = circles.OrderBy(o => o.transform.position.y).ToList();
        flyingFires = flyingFires.OrderBy(o => o.transform.position.y).ToList();
        
        List<GameObject> toLoopFire = fireLines.Take(fireLinesPerGroup).ToList();
        List<GameObject> toLoopCircle = circles.Take(circlesPerGroup).ToList();
        List<GameObject> toLoopFlyingFire = flyingFires.Take(flyingFiresPerGroup).ToList();
        
        if (toLoopFire.Count < fireLinesPerGroup || 
            toLoopCircle.Count < circlesPerGroup || 
            toLoopFlyingFire.Count < flyingFiresPerGroup)
        {
            return;
        }
        
        float newY = currentHighestY + groupSpacing;
        
        foreach (GameObject fire in toLoopFire)
        {
            passedObstacles.Remove(fire);
        }
        foreach (GameObject circle in toLoopCircle)
        {
            passedObstacles.Remove(circle);
        }
        foreach (GameObject flyingFire in toLoopFlyingFire)
        {
            passedObstacles.Remove(flyingFire);
        }
        
        foreach (GameObject fire in toLoopFire)
        {
            Vector3 pos = fire.transform.position;
            pos.y = newY;
            fire.transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(fire);
            
            ApplyCurrentSpeedToObstacle(fire);
            
            newY += spacingY;
            totalLooped++;
        }
        
        newY += groupSpacing;
        
        float circleCenterY = newY;
        
        foreach (GameObject circle in toLoopCircle)
        {
            Vector3 pos = circle.transform.position;
            pos.y = circleCenterY;
            circle.transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(circle);
            
            ApplyCurrentSpeedToObstacle(circle);
            
            totalLooped++;
        }
        
        float flyingFireStartY;
        
        if (spawnFireAboveCircle)
        {
            float circleTopEdge = circleCenterY + circleRadius;
            flyingFireStartY = circleTopEdge + circleToFireSpacing;
        }
        else
        {
            float circleBottomEdge = circleCenterY - circleRadius;
            flyingFireStartY = circleBottomEdge - circleToFireSpacing;
        }
        
        newY = flyingFireStartY;
        
        foreach (GameObject flyingFire in toLoopFlyingFire)
        {
            Vector3 pos = flyingFire.transform.position;
            pos.y = newY;
            flyingFire.transform.position = pos;
            
            if (randomizeOnLoop)
                RandomizeObstacle(flyingFire);
            
            ApplyCurrentSpeedToObstacle(flyingFire);
            
            newY += spacingY;
            totalLooped++;
        }
        
        currentWave++;
    }
    
    void RandomizeObstacle(GameObject obstacle)
    {
        var spawner = obstacle.GetComponent<ObstacleSpawner>();
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (var obs in spawner.SpawnedObstacles)
            {
                var controller = obs.GetComponent<ObstacleController>();
                if (controller != null)
                {
                    controller.RandomizeOnly();
                }
            }
        }
        
        var circle = obstacle.GetComponent<FlyingCircleController>();
        if (circle != null)
        {
            circle.RandomizeAngles();
        }
    }
    
    void ApplyCurrentSpeedToObstacle(GameObject obstacle)
    {
        var spawner = obstacle.GetComponent<ObstacleSpawner>();
        if (spawner != null && spawner.SpawnedObstacles != null)
        {
            foreach (var obs in spawner.SpawnedObstacles)
            {
                var movement = obs.GetComponent<ObstacleMovement>();
                if (movement != null)
                {
                    movement.SetSpeed(currentSpeed);
                }
            }
        }
        
        var circle = obstacle.GetComponent<FlyingCircleController>();
        if (circle != null)
        {
            circle.SetRotationSpeed(currentRotationSpeed);
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying || player == null) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.LowerLeft;
        style.fontStyle = FontStyle.Bold;
        
        int remainingToSpeedUp = obstaclesPerSpeedIncrease - ((int)(obstaclesPassed - lastSpeedIncreaseAt));
        
        string info = $"Speed: {currentSpeed:F2}/{maxSpeed:F1}\n";
        info += $"Passed: {obstaclesPassed}\n";
        info += $"Next +SPEED: {remainingToSpeedUp}\n";
        info += $"Wave: {currentWave}";
        
        GUI.Label(new Rect(10, Screen.height - 100, 300, 100), info, style);
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || allObstacles == null) return;
        
        foreach (GameObject obj in allObstacles)
        {
            if (obj == null) continue;
            
            if (obj.name.ToLower().Contains("circle"))
            {
                Vector3 center = obj.transform.position;
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(center, 0.3f);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(center, circleRadius);
            }
        }
    }
    
    public void ResetDifficulty()
    {
        currentWave = 0;
        currentSpeed = initialSpeed;
        currentRotationSpeed = initialRotationSpeed;
        totalLooped = 0;
        obstaclesPassed = 0;
        lastSpeedIncreaseAt = 0;
        passedObstacles.Clear();
    }
}