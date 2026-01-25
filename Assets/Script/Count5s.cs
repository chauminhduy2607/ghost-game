using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// ⏱️ ĐẾM NGƯỢC 5 GIÂY TRƯỚC KHI CHƠI
/// </summary>
public class Count5s : MonoBehaviour
{
    [Header("=== UI ===")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text countdownText;
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private int countdownFrom = 5;
    
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghostController;
    [SerializeField] private ScoreCycle scoreCycle;
    [SerializeField] private FireLineManager fireLineManager;
    [SerializeField] private GhostAI ghostAI;
    
    private bool isCountingDown = false;
    
    void Start()
    {

        // ✅ Nếu bấm PLAY từ menu thì skip countdown
    if (PlayerPrefs.GetInt("SkipCountdown", 0) == 1)
    {
        PlayerPrefs.SetInt("SkipCountdown", 0);
        PlayerPrefs.Save();

        if (countdownPanel != null) countdownPanel.SetActive(false);
        ResumeGame();
        return;
    }
        // Tự động tìm
        if (ghostController == null)
            ghostController = FindObjectOfType<GhostController>();
        
        if (scoreCycle == null)
            scoreCycle = FindObjectOfType<ScoreCycle>();
        
        if (fireLineManager == null)
            fireLineManager = FindObjectOfType<FireLineManager>();
        
        if (ghostAI != null)
            ghostAI = FindObjectOfType<GhostAI>();
        
        // Kiểm tra xem có phải Continue không
        bool isContinue = PlayerPrefs.GetInt("IsContinue", 0) == 1;
        
        if (isContinue)
        {
            // CONTINUE: Khôi phục điểm cũ
            int savedScore = PlayerPrefs.GetInt("ContinueScore", 100);
            if (scoreCycle != null)
            {
                scoreCycle.StopScore();
                // Set lại score (cần thêm method SetScore trong ScoreCycle)
            }
            PlayerPrefs.SetInt("IsContinue", 0); // Reset flag
        }
        
        // Bắt đầu đếm ngược
        StartCountdown();
    }
    
    /// <summary>
    /// Bắt đầu đếm ngược
    /// </summary>
    public void StartCountdown()
    {
        if (isCountingDown) return;
        
        // Hiện panel đếm ngược
        if (countdownPanel != null)
            countdownPanel.SetActive(true);
        
        // Dừng mọi thứ
        PauseGame();
        
        // Bắt đầu coroutine đếm ngược
        StartCoroutine(CountdownCoroutine());
    }
    
    /// <summary>
    /// Coroutine đếm ngược
    /// </summary>
    IEnumerator CountdownCoroutine()
    {
        isCountingDown = true;
        
        for (int i = countdownFrom; i > 0; i--)
        {
            // Hiển thị số
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                countdownText.fontSize = 120;
            }
            
            Debug.Log("⏱️ Countdown: " + i);
            
            // Đợi 1 giây
            yield return new WaitForSecondsRealtime(1f);
        }
        
        // Hiển thị "GO!"
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            countdownText.fontSize = 100;
        }
        
        yield return new WaitForSecondsRealtime(0.5f);
        
        // Ẩn panel
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
        
        // Bắt đầu game
        ResumeGame();
        
        
        isCountingDown = false;
        
        Debug.Log("🎮 START GAME!");
    }
    
    /// <summary>
    /// Tạm dừng game
    /// </summary>
    void PauseGame()
    {
        // Dừng Ghost
        if (ghostController != null)
        {
            ghostController.EnablePhysics(false);
        }
        
        // Dừng Score
        if (scoreCycle != null)
        {
            scoreCycle.StopScore();
        }
        
        // Dừng FireLine
        if (fireLineManager != null)
        {
            fireLineManager.enabled = false;
        }
        
        // Dừng AI
        if (ghostAI != null)
        {
            ghostAI.SetAIEnabled(false);
        }
    }
    
    /// <summary>
    /// Tiếp tục game
    /// </summary>
    void ResumeGame()
    {
        // Bật Ghost
        if (ghostController != null)
        {
            ghostController.EnablePhysics(true);
        }
        
        // Bật Score
        if (scoreCycle != null)
        {
            scoreCycle.StartScore();
        }
        
        // Bật FireLine
        if (fireLineManager != null)
        {
            fireLineManager.enabled = true;
        }
        
        // Bật AI nếu có
        if (ghostAI != null)
        {
            bool aiEnabled = PlayerPrefs.GetInt("AIEnabled", 0) == 1;
            ghostAI.SetAIEnabled(aiEnabled);
        }
    }
}