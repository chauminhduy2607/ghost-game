using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ⭐ MENU CHÍNH - Giống Swing Copters
/// - Nút Start: Chơi game
/// - Nút Score: Xem bảng điểm
/// - Background tự động
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("=== BUTTONS ===")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject scoreButton;
    
    [Header("=== PANELS ===")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject scorePanel;
    [SerializeField] private TMP_Text highScoreText;
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private string gameSceneName = "SampleScene";
    
    void Start()
    {
        // Hiển thị menu, ẩn score panel
        ShowMenu();
        
        // Load high score
        UpdateHighScore();
    }
    
    // ==================== NÚT START ====================
    public void OnStartButton()
    {
        Debug.Log("🎮 Start Game!");
        SceneManager.LoadScene(gameSceneName);
    }
    
    // ==================== NÚT SCORE ====================
    public void OnScoreButton()
    {
        Debug.Log("🏆 Show Score!");
        menuPanel.SetActive(false);
        scorePanel.SetActive(true);
        UpdateHighScore();
    }
    
    // ==================== NÚT BACK ====================
    public void OnBackButton()
    {
        Debug.Log("⬅️ Back to Menu!");
        ShowMenu();
    }
    
    // ==================== HIỂN THỊ MENU ====================
    void ShowMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(true);
        
        if (scorePanel != null)
            scorePanel.SetActive(false);
    }
    
    // ==================== CẬP NHẬT HIGH SCORE ====================
    void UpdateHighScore()
    {
        if (highScoreText == null) return;
        
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        highScoreText.text = "High Score: " + highScore;
    }
    
    // ==================== NÚT QUIT ====================
    public void OnQuitButton()
    {
        Debug.Log("👋 Quit Game!");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}