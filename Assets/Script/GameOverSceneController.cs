using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 🎮 GAME OVER SCENE CONTROLLER
/// Hiển thị điểm và best score
/// </summary>
public class GameOverSceneController : MonoBehaviour
{
    [Header("=== UI ===")]
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string menuSceneName = "MenuScene";
    
    void Start()
    {
        // Lấy điểm từ PlayerPrefs (được lưu từ SampleScene)
        int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
        // Hiển thị điểm
        if (currentScoreText != null)
        {
            currentScoreText.text = finalScore.ToString();
        }
        
        if (bestScoreText != null)
        {
            bestScoreText.text = bestScore.ToString();
        }
        
        Debug.Log("💀 Game Over Scene Loaded!");
        Debug.Log("Score: " + finalScore);
        Debug.Log("Best: " + bestScore);
    }
    
    /// <summary>
    /// Nút Retry - Chơi lại
    /// </summary>
    public void OnRetryButton()
    {
        Debug.Log("🔄 RETRY!");
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// Nút Menu - Về menu
    /// </summary>
    public void OnMenuButton()
    {
        Debug.Log("🏠 GO TO MENU!");
        SceneManager.LoadScene(menuSceneName);
    }
}