using System.Collections;
using UnityEngine;

public class GhostController : MonoBehaviour
{
    [Header("=== VẬT LÝ GHOST ===")]
    [SerializeField] private float mass = 0.01f;
    [SerializeField] private float linearDrag = 0.5f;
    [SerializeField] private float angularDrag = 1f;
    [SerializeField] private float gravityScale = 0f;
    
    [Header("=== TỰ BAY LÊN ===")]
    [SerializeField] private float autoRiseForce = 0.25f;
    [SerializeField] private float maxAutoRiseSpeed = 0.5f;
    
    [Header("=== RỚT XUỐNG KHI NHẤN ===")]
    [SerializeField] private float fallForceMultiplier = 30.0f;
    
    [Header("=== VẬT CẢN ===")]
    [SerializeField] private float obstacleKnockbackForce = 15f;
    
    [Header("=== ADS TIMING ===")]
    [SerializeField] private float delayBeforeAds = 1f;
    [SerializeField] private float adsTimeout = 30f;
    
    [Header("=== XOAY ĐẦU KHI RỚT ===")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 4f;
    
    [Header("=== ĐIỀU KHIỂN VUỐT ===")]
    [SerializeField] private float swipeForceMultiplier = 300f;
    [SerializeField] private float maxSwipeForce = 100f;
    [SerializeField] private float minSwipeDistance = 0.01f;
    
    [Header("=== TỐC ĐỘ ===")]
    [SerializeField] private float maxRiseSpeed = 20f;
    
    [Header("=== HIỆU ỨNG MÓP ===")]
    [SerializeField] private bool enableSquashStretch = true;
    [SerializeField] private float squashAmount = 0.15f;
    [SerializeField] private float squashSpeed = 8f;
    [SerializeField] private float minVelocityForSquash = 2f;
    [SerializeField] private float groundSquashAmount = 0.6f;
    [SerializeField] private float groundSquashDuration = 0.35f;
    
    [Header("=== VISUAL EFFECTS ===")]
    [SerializeField] private bool enableTrail = true;
    [SerializeField] private bool enableColorChange = true;
    
    [Header("=== ÂM THANH VUỐT ===")]
    [SerializeField] private AudioClip swipeSound;
    [SerializeField] private float swipeSoundVolume = 0.7f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    
    [Header("=== ÂM THANH VA CHẠM ===")]
    [SerializeField] private AudioClip fireHitSound;
    [SerializeField] private AudioClip lightningHitSound;
    [SerializeField] private AudioClip circleHitSound;
    [SerializeField] private AudioClip defaultHitSound;
    [Range(0f, 1f)]
    [SerializeField] private float hitSoundVolume = 0.8f;
    
    private Rigidbody2D rb;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trail;
    private Color originalColor;
    private AudioSource audioSource;
    private ScoreCycle scoreCycle; // THÊM: cache reference
    
    private bool isDragging = false;
    private Vector2 dragStartPos;
    private Vector2 lastMousePos;
    private bool hasAppliedSwipe = false;
    private bool isFalling = false;
    private bool gameStarted = false; // THÊM: chờ swipe đầu tiên
    
    private bool hitObstacle = false;
    private bool isGameOver = false;
    private bool isWaitingForAds = false;
    private bool adsClosed = false;
    private bool hasUsedContinue = false;
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isGroundSquashing = false;
    private float groundSquashTimer = 0f;
    
    private Quaternion targetRotation;
    
    public bool IsGameOver => isGameOver;
    public bool HitObstacle => hitObstacle;
    public bool IsWaitingForAds => isWaitingForAds;
    public bool HasUsedContinue => hasUsedContinue;
    public bool GameStarted => gameStarted; // THÊM
    public Rigidbody2D Rigidbody => rb;
    public Vector2 Velocity => rb.linearVelocity;
    public bool IsDragging => isDragging;
    public bool IsFalling => isFalling;
   
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = mass;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;
        rb.gravityScale = gravityScale;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        originalScale = transform.localScale;
        targetScale = originalScale;
        targetRotation = Quaternion.Euler(0, 0, 0);
        transform.rotation = targetRotation;
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        
        if (enableTrail)
        {
            trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.4f;
                trail.startWidth = 0.25f;
                trail.endWidth = 0.05f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = new Color(1f, 1f, 0f, 0.5f);
                trail.endColor = new Color(1f, 1f, 0f, 0f);
            }
        }
    }
    
    void Start()
    {
        // Cache ScoreCycle
        scoreCycle = FindAnyObjectByType<ScoreCycle>();
        
        // Đảm bảo score CHƯA chạy khi bắt đầu
        if (scoreCycle != null)
            scoreCycle.StopScore();
        
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.LoadInterstitial();
            AdsManager.Instance.OnAdShowComplete += OnAdsComplete;
            AdsManager.Instance.OnAdShowFailed += OnAdsFailed;
        }
    }
    
    void Update()
    {
        if (!isGameOver)
        {
            HandleSwipeInput();
            UpdateVisuals();
            
            if (enableSquashStretch)
                UpdateSquashStretch();
            
            if (enableRotation)
                UpdateRotation();
        }
    }
    
    void FixedUpdate()
    {
        if (isGameOver) return;
        if (!gameStarted) return; // THÊM: không move trước khi swipe
        ApplyMovement();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag.Contains("Obstacle") && !hitObstacle)
            OnObstacleHit(other.transform, other.tag);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Contains("Obstacle") && !hitObstacle)
            OnObstacleHit(collision.transform, collision.gameObject.tag);
    }
    
    void OnObstacleHit(Transform obstacleTransform, string obstacleTag)
    {
        if (hitObstacle) return;
        
        hitObstacle = true;
        isFalling = true;
        
        // Dừng điểm khi chết
        if (scoreCycle != null)
            scoreCycle.StopScore();
        
        PlayHitSound(obstacleTag);
        
        PlayerPrefs.SetFloat("RespawnX", transform.position.x);
        PlayerPrefs.SetFloat("RespawnY", transform.position.y);
        PlayerPrefs.SetFloat("RespawnVelocityY", rb.linearVelocity.y);
        PlayerPrefs.Save();
        
        Vector2 knockbackDirection = (transform.position - obstacleTransform.position).normalized;
        if (knockbackDirection == Vector2.zero) knockbackDirection = Vector2.up;
        
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection * obstacleKnockbackForce, ForceMode2D.Impulse);
        
        if (enableColorChange && spriteRenderer != null)
            spriteRenderer.color = new Color(0.8f, 0f, 0f, 1f);
        
        if (enableRotation)
            targetRotation = Quaternion.Euler(0, 0, 180);
        
        StartCoroutine(DeathSequence());
    }
    
    void PlayHitSound(string obstacleTag)
    {
        if (audioSource == null) return;
        bool isSoundOn = PlayerPrefs.GetInt("IsSoundOn", 1) == 1;
        if (!isSoundOn) return;
        
        AudioClip soundToPlay = null;
        string tagLower = obstacleTag.ToLower();
        
        if (tagLower.Contains("fire")) soundToPlay = fireHitSound;
        else if (tagLower.Contains("lightning")) soundToPlay = lightningHitSound;
        else if (tagLower.Contains("circle")) soundToPlay = circleHitSound;
        else soundToPlay = defaultHitSound;
        
        if (soundToPlay != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(soundToPlay, hitSoundVolume);
        }
    }
    
    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(delayBeforeAds);
        
        isWaitingForAds = true;
        bool hasAds = ShowAds();
        
        if (!hasAds)
        {
            TriggerGameOver();
            yield break;
        }
        
        float timeWaited = 0f;
        while (!adsClosed && timeWaited < adsTimeout)
        {
            yield return new WaitForSeconds(0.5f);
            timeWaited += 0.5f;
        }
        
        isWaitingForAds = false;
        TriggerGameOver();
    }
    
    bool ShowAds()
    {
        if (AdsManager.Instance == null) return false;
        if (!AdsManager.Instance.CanShowInterstitial()) return false;
        return AdsManager.Instance.ShowInterstitial();
    }
    
    void OnAdsComplete(string unitId)
    {
        if (!adsClosed) adsClosed = true;
    }
    
    void OnAdsFailed(string unitId, string message)
    {
        if (!adsClosed) adsClosed = true;
    }
    
    void TriggerGameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        
        if (spriteRenderer != null)
            spriteRenderer.color = Color.black;
    }
    
    void HandleSwipeInput()
    {
        if (hitObstacle)
        {
            isDragging = false;
            return;
        }
        
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = mouseWorldPos;
            lastMousePos = mouseWorldPos;
            hasAppliedSwipe = false;
            
            if (enableColorChange && spriteRenderer != null)
                spriteRenderer.color = Color.cyan;
        }
        
        if (Input.GetMouseButton(0) && isDragging)
            lastMousePos = mouseWorldPos;
        
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            
            Vector2 totalSwipe = mouseWorldPos - dragStartPos;
            float swipeDistance = totalSwipe.y;
            
            if (swipeDistance >= minSwipeDistance)
            {
                float swipeForce = swipeDistance * swipeForceMultiplier;
                swipeForce = Mathf.Min(swipeForce, maxSwipeForce);
                
                rb.AddForce(Vector2.up * swipeForce, ForceMode2D.Impulse);
                isFalling = false;
                
                if (!gameStarted)
                {
                    gameStarted = true;
                    if (scoreCycle != null)
                        scoreCycle.StartScore();
                }
                
                PlaySwipeSound(swipeForce);
                
                if (enableRotation)
                    targetRotation = Quaternion.Euler(0, 0, 0);
                
                if (enableTrail && trail != null)
                {
                    float intensity = swipeForce / maxSwipeForce;
                    trail.startColor = Color.Lerp(
                        new Color(1f, 1f, 0f, 0.5f),
                        new Color(0f, 1f, 1f, 0.9f),
                        intensity
                    );
                    trail.startWidth = 0.2f + (0.3f * intensity);
                }
                
                if (enableColorChange && spriteRenderer != null)
                    spriteRenderer.color = originalColor;
            }
        }
        
        // ── TAB / KEYBOARD INPUT (đã tắt, chỉ dùng vuốt) ──────────────────
        // if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Tab))
        // {
        //     rb.AddForce(Vector2.up * maxSwipeForce, ForceMode2D.Impulse);
        //     isFalling = false;
        //     if (!gameStarted)
        //     {
        //         gameStarted = true;
        //         if (scoreCycle != null) scoreCycle.StartScore();
        //     }
        // }
        // ───────────────────────────────────────────────────────────────────
    }
    
    void PlaySwipeSound(float swipeForce)
    {
        if (swipeSound == null || audioSource == null) return;
        bool isSoundOn = PlayerPrefs.GetInt("IsSoundOn", 1) == 1;
        if (!isSoundOn) return;
        
        float intensity = swipeForce / maxSwipeForce;
        float volume = swipeSoundVolume * Mathf.Lerp(0.7f, 1f, intensity);
        
        audioSource.pitch = randomizePitch ? Random.Range(minPitch, maxPitch) : 1f;
        audioSource.PlayOneShot(swipeSound, volume);
    }
    
    void ApplyMovement()
    {
        if (isFalling)
        {
            float fallForce = autoRiseForce * fallForceMultiplier;
            rb.AddForce(Vector2.down * fallForce, ForceMode2D.Force);
        }
        else if (!isDragging)
        {
            if (rb.linearVelocity.y < maxAutoRiseSpeed)
                rb.AddForce(Vector2.up * autoRiseForce, ForceMode2D.Force);
        }
        
        if (rb.linearVelocity.y > maxRiseSpeed)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y = maxRiseSpeed;
            rb.linearVelocity = vel;
        }
    }
    
    void UpdateRotation()
    {
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }
    
    void UpdateSquashStretch()
    {
        if (isGroundSquashing)
        {
            groundSquashTimer += Time.deltaTime;
            
            if (groundSquashTimer < groundSquashDuration)
            {
                float progress = groundSquashTimer / groundSquashDuration;
                float squashCurve = Mathf.Pow(1f - progress, 2f);
                
                targetScale = new Vector3(
                    originalScale.x * (1f + groundSquashAmount * squashCurve),
                    originalScale.y * (1f - groundSquashAmount * 0.8f * squashCurve),
                    originalScale.z
                );
                
                transform.localScale = targetScale;
                return;
            }
            else
            {
                isGroundSquashing = false;
                groundSquashTimer = 0f;
            }
        }
        
        float velocityY = rb.linearVelocity.y;
        
        if (velocityY > minVelocityForSquash)
        {
            float squashFactor = Mathf.Clamp(velocityY / 10f, 0f, 1f);
            float squash = squashAmount * squashFactor;
            targetScale = new Vector3(
                originalScale.x * (1f - squash),
                originalScale.y * (1f + squash),
                originalScale.z
            );
        }
        else if (velocityY < -minVelocityForSquash)
        {
            float squashFactor = Mathf.Clamp(-velocityY / 10f, 0f, 1f);
            float squash = squashAmount * squashFactor;
            targetScale = new Vector3(
                originalScale.x * (1f + squash * 0.5f),
                originalScale.y * (1f - squash * 0.5f),
                originalScale.z
            );
        }
        else
        {
            targetScale = originalScale;
        }
        
        transform.localScale = Vector3.Lerp(
            transform.localScale, 
            targetScale, 
            Time.deltaTime * squashSpeed
        );
    }
    
    public void TriggerGroundSquash()
    {
        if (!enableSquashStretch) return;
        isGroundSquashing = true;
        groundSquashTimer = 0f;
    }
    
    void UpdateVisuals()
    {
        if (!enableColorChange || spriteRenderer == null) return;
        
        if (hitObstacle && !isGameOver)
        {
            float blinkSpeed = 5f;
            float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            spriteRenderer.color = Color.Lerp(
                new Color(0.5f, 0f, 0f, 1f),
                new Color(1f, 0f, 0f, 1f),
                blink
            );
            return;
        }
        
        float totalSpeed = rb.linearVelocity.y;
        
        if (isFalling)
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.red, Time.deltaTime * 5f);
        else if (isDragging)
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.cyan, Time.deltaTime * 5f);
        else if (totalSpeed > 3f)
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.green, Time.deltaTime * 3f);
        else
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, originalColor, Time.deltaTime * 3f);
        
        if (enableTrail && trail != null)
        {
            if (isFalling)
            {
                trail.startWidth = 0.15f;
                trail.startColor = new Color(1f, 0.3f, 0f, 0.6f);
            }
            else if (totalSpeed > 4f)
            {
                trail.startWidth = 0.35f;
                trail.startColor = new Color(0f, 1f, 1f, 0.8f);
            }
            else
            {
                trail.startWidth = 0.2f;
                trail.startColor = new Color(1f, 1f, 0f, 0.5f);
            }
        }
    }
    
    public void ResetVelocity()
    {
        rb.linearVelocity = Vector2.zero;
        hasAppliedSwipe = false;
        isFalling = false;
    }
    
    public void SetVelocity(Vector2 velocity) => rb.linearVelocity = velocity;
    public void SetAutoRiseForce(float force) => autoRiseForce = force;
    public void AddImpulse(Vector2 force) => rb.AddForce(force, ForceMode2D.Impulse);
    public void EnablePhysics(bool enable) => rb.simulated = enable;
    
    public void StopFalling()
    {
        isFalling = false;
        if (enableColorChange && spriteRenderer != null)
            spriteRenderer.color = originalColor;
        if (enableRotation)
            targetRotation = Quaternion.Euler(0, 0, 0);
    }
    
    public void MarkContinueUsed()
    {
        hasUsedContinue = true;
    }
    
    public void ResetGame()
    {
        hitObstacle = false;
        isGameOver = false;
        isWaitingForAds = false;
        adsClosed = false;
        isFalling = false;
        isDragging = false;
        
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        
        transform.rotation = Quaternion.Euler(0, 0, 0);
        targetRotation = Quaternion.Euler(0, 0, 0);
    }
    
    public void ResetNewGame()
    {
        ResetGame();
        gameStarted = false; // THÊM: reset về chờ swipe
        hasUsedContinue = false;
        
        // Dừng score, chờ swipe mới
        if (scoreCycle != null)
            scoreCycle.StopScore();
    }
    
    void OnDestroy()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnAdShowComplete -= OnAdsComplete;
            AdsManager.Instance.OnAdShowFailed -= OnAdsFailed;
        }
    }
}