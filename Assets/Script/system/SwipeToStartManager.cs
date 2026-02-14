using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 👆 SWIPE TO START MANAGER
/// Hiển thị màn hình "Swipe to Start" và đợi user vuốt để bắt đầu game
/// </summary>
public class SwipeToStartManager : MonoBehaviour
{
    [Header("=== UI ELEMENTS ===")]
    [SerializeField] private GameObject swipePanel;
    [SerializeField] private TMP_Text swipeText;
    [SerializeField] private Image swipePanelBackground;
    
    [Header("=== SWIPE SETTINGS ===")]
    [Tooltip("Khoảng cách vuốt tối thiểu để start (pixels)")]
    [SerializeField] private float minSwipeDistance = 100f;
    
    [Header("=== ANIMATION ===")]
    [SerializeField] private bool enableTextAnimation = true;
    [SerializeField] private float textPulseSpeed = 2f;
    
    [Header("=== FADE OUT ===")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    private bool isWaitingForSwipe = true;
    private bool hasStarted = false;
    private Vector2 swipeStartPos;
    private bool isDragging = false;
    
    // Events
    public System.Action OnGameStarted;
    
    void Start()
    {
        // Hiển thị swipe panel
        if (swipePanel != null)
            swipePanel.SetActive(true);
        
        // Set text mặc định
        if (swipeText != null)
        {
            int isContinue = PlayerPrefs.GetInt("IsContinue", 0);
            swipeText.text = isContinue == 1 
                ? " Swipe to Continue\n(Vuốt để tiếp tục)" 
                : " Swipe to Start\n(Vuốt để bắt đầu)";
        }
        
        // Pause game
        Time.timeScale = 0f;
        
        Debug.Log("⏸️ Game paused - Waiting for swipe...");
    }
    
    void Update()
    {
        if (!isWaitingForSwipe || hasStarted) return;
        
        // Text animation
        if (enableTextAnimation && swipeText != null)
        {
            float alpha = 0.5f + Mathf.Sin(Time.unscaledTime * textPulseSpeed) * 0.5f;
            Color color = swipeText.color;
            color.a = alpha;
            swipeText.color = color;
        }
        
        // Detect swipe
        DetectSwipe();
    }
    
    void DetectSwipe()
    {
        // Mouse/Touch input
        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPos = Input.mousePosition;
            isDragging = true;
        }
        
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            Vector2 swipeEndPos = Input.mousePosition;
            Vector2 swipeDelta = swipeEndPos - swipeStartPos;
            
            // Check swipe distance
            if (swipeDelta.magnitude >= minSwipeDistance)
            {
                Debug.Log($"✅ Swipe detected! Distance: {swipeDelta.magnitude:F0}px");
                StartGame();
            }
            else
            {
                Debug.Log($"⚠️ Swipe too short: {swipeDelta.magnitude:F0}px (min: {minSwipeDistance}px)");
            }
        }
    }
    
    void StartGame()
    {
        if (hasStarted) return;
        
        hasStarted = true;
        isWaitingForSwipe = false;
        
        Debug.Log("🎮 GAME STARTED!");
        
        // Fade out panel
        StartCoroutine(FadeOutAndStart());
    }
    
    System.Collections.IEnumerator FadeOutAndStart()
    {
        // Fade out animation
        if (swipePanelBackground != null)
        {
            Color startColor = swipePanelBackground.color;
            float elapsed = 0f;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                
                Color color = swipePanelBackground.color;
                color.a = alpha;
                swipePanelBackground.color = color;
                
                if (swipeText != null)
                {
                    Color textColor = swipeText.color;
                    textColor.a = alpha;
                    swipeText.color = textColor;
                }
                
                yield return null;
            }
        }
        
        // Hide panel
        if (swipePanel != null)
            swipePanel.SetActive(false);
        
        // Resume game
        Time.timeScale = 1f;
        
        // Trigger event
        OnGameStarted?.Invoke();
        
        Debug.Log("▶️ Game resumed - Time.timeScale = 1");
    }
    
    /// <summary>
    /// Public method để force start (dùng cho testing)
    /// </summary>
    public void ForceStart()
    {
        if (!hasStarted)
            StartGame();
    }
}