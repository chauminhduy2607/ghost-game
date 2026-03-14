using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    
    public System.Action OnGameStarted;
    
    void Start()
    {
        if (swipePanel != null)
            swipePanel.SetActive(true);
        
        if (swipeText != null)
        {
            int isContinue = PlayerPrefs.GetInt("IsContinue", 0);
            swipeText.text = isContinue == 1 
                ? " Swipe to Continue\n(Vuốt để tiếp tục)" 
                : " Swipe to Start\n(Vuốt để bắt đầu)";
        }
        
        Time.timeScale = 0f;
    }
    
    void Update()
    {
        if (!isWaitingForSwipe || hasStarted) return;
        
        if (enableTextAnimation && swipeText != null)
        {
            float alpha = 0.5f + Mathf.Sin(Time.unscaledTime * textPulseSpeed) * 0.5f;
            Color color = swipeText.color;
            color.a = alpha;
            swipeText.color = color;
        }
        
        DetectSwipe();
    }
    
    void DetectSwipe()
    {
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
            
            if (swipeDelta.magnitude >= minSwipeDistance)
            {
                StartGame();
            }
        }
    }
    
    void StartGame()
    {
        if (hasStarted) return;
        
        hasStarted = true;
        isWaitingForSwipe = false;
        
        StartCoroutine(FadeOutAndStart());
    }
    
    System.Collections.IEnumerator FadeOutAndStart()
    {
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
        
        if (swipePanel != null)
            swipePanel.SetActive(false);
        
        Time.timeScale = 1f;
        
        OnGameStarted?.Invoke();
    }
    
    public void ForceStart()
    {
        if (!hasStarted)
            StartGame();
    }
}