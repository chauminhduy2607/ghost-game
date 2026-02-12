using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 🎮 GAME OVER SCENE CONTROLLER
/// Hiển thị điểm và best score + Leaderboard
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
    [SerializeField] private Button leaderboardButton; // ⭐ MỚI
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private string gameSceneName = "GameplayScreen";
    [SerializeField] private string menuSceneName = "StartGameScreen";
    
    void Start()
    {
        // Lấy điểm từ PlayerPrefs
        int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
        // ⭐ THÊM ĐIỂM VÀO LEADERBOARD
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.AddScore(finalScore);
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
        
        // ⭐ GÁN SỰ KIỆN CHO NÚT LEADERBOARD
        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.AddListener(OnLeaderboardButton);
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
        
        PlayerPrefs.SetInt("IsContinue", 0);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// ⭐ MỚI: Nút Continue - Tiếp tục ở mức thua
    /// </summary>
    public void OnContinueButton()
    {
        Debug.Log("▶️ CONTINUE - Tiếp tục chơi!");
        
        int currentScore = PlayerPrefs.GetInt("FinalScore", 0);
        PlayerPrefs.SetInt("ContinueScore", currentScore);
        PlayerPrefs.SetInt("IsContinue", 1);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// Nút Home - Về menu
    /// </summary>
    public void OnMenuButton()
    {
        Debug.Log("🏠 GO TO MENU!");
        
        PlayerPrefs.SetInt("IsContinue", 0);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(menuSceneName);
    }
    
    /// <summary>
    /// ⭐ MỚI: Nút Leaderboard - Hiện bảng xếp hạng
    /// </summary>
    public void OnLeaderboardButton()
    {
        Debug.Log("🏆 SHOW LEADERBOARD!");
        
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.ShowLeaderboard();
        }
        else
        {
            Debug.LogError("❌ LeaderboardManager not found!");
        }
    }
}