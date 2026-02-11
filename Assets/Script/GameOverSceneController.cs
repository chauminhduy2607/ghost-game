using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 🎮 GAME OVER SCENE CONTROLLER
/// Hiển thị điểm và best score + Phát nhạc nền
/// </summary>
public class GameOverSceneController : MonoBehaviour
{
    [Header("=== UI ===")]
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    
    [Header("=== BUTTONS ===")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button menuButton;
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string menuSceneName = "MenuScene";
    
    [Header("=== AUDIO (Optional) ===")]
    [SerializeField] private bool playGameOverMusic = true;
    
    void Start()
    {
        // ⭐ PHÁT NHẠC NỀN GAME OVER
        if (playGameOverMusic && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOverMusic();
        }
        
        // Lấy điểm từ PlayerPrefs (được lưu từ SampleScene)
        int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
        // Cập nhật best score nếu cần
        if (finalScore > bestScore)
        {
            bestScore = finalScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }
        
        // Hiển thị điểm
        if (currentScoreText != null)
        {
            currentScoreText.text = finalScore.ToString();
        }
        
        if (bestScoreText != null)
        {
            bestScoreText.text = bestScore.ToString();
        }
        
        // ⭐ GÁN SỰ KIỆN CHO CÁC NÚT (với sound effect)
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButton);
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButton);
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuButton);
        }
        
        Debug.Log("💀 Game Over Scene Loaded!");
        Debug.Log("Score: " + finalScore);
        Debug.Log("Best: " + bestScore);
    }
    
    /// <summary>
    /// Nút Retry - Chơi lại từ đầu (reset score)
    /// </summary>
    public void OnRetryButton()
    {
        Debug.Log("🔄 RETRY - Chơi lại từ đầu!");
        
        // ⭐ PHÁT SOUND EFFECT
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
        
        // Xóa flag Continue
        PlayerPrefs.SetInt("IsContinue", 0);
        PlayerPrefs.Save();
        
        // Load lại SampleScene
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// ⭐ MỚI: Nút Continue - Tiếp tục ở mức thua
    /// </summary>
    public void OnContinueButton()
    {
        Debug.Log("▶️ CONTINUE - Tiếp tục chơi!");
        
        // ⭐ PHÁT SOUND EFFECT
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
        
        // Lưu điểm hiện tại để khôi phục
        int currentScore = PlayerPrefs.GetInt("FinalScore", 0);
        PlayerPrefs.SetInt("ContinueScore", currentScore);
        
        // Đánh dấu là Continue
        PlayerPrefs.SetInt("IsContinue", 1);
        PlayerPrefs.Save();
        
        // Load lại SampleScene
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// Nút Home - Về menu
    /// </summary>
    public void OnMenuButton()
    {
        Debug.Log("🏠 GO TO MENU!");
        
        // ⭐ PHÁT SOUND EFFECT
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
        
        // Xóa flag Continue
        PlayerPrefs.SetInt("IsContinue", 0);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(menuSceneName);
    }
    
    /// <summary>
    /// ⭐ MỚI: Dừng nhạc khi thoát scene
    /// </summary>
    void OnDestroy()
    {
        // Xóa listener để tránh memory leak
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryButton);
        
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueButton);
        
        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuButton);
    }
}