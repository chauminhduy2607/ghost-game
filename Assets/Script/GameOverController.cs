using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ⭐ XỬ LÝ VA CHẠM VÀ GAME OVER
/// - Gắn vào Ghost
/// - Khi chạm vật cản có tag "Obstacle" → Game Over
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Header("=== UI GAME OVER ===")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private TMP_Text finalScoreText;
    
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghostController;
    [SerializeField] private ScoreCycle scoreCycle;
    [SerializeField] private FireLineManager fireLineManager; // ⭐ THÊM
    
    private bool isGameOver = false;
    
    void Start()
    {
        // Tự động tìm nếu chưa gắn
        if (ghostController == null)
            ghostController = GetComponent<GhostController>();
        
        if (scoreCycle == null)
            scoreCycle = FindObjectOfType<ScoreCycle>();
        
        // ⭐ THÊM: Tự động tìm FireLineManager
        if (fireLineManager == null)
            fireLineManager = FindObjectOfType<FireLineManager>();
        
        // Ẩn UI Game Over
        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }
    
    // ==================== VA CHẠM ====================
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra tag "Obstacle"
        if (collision.CompareTag("Obstacle") && !isGameOver)
        {
            TriggerGameOver();
        }
    }
    
    // ==================== GAME OVER ====================
    void TriggerGameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        Debug.Log("💀 GAME OVER!");
        
        // Dừng Ghost
        if (ghostController != null)
        {
            ghostController.EnablePhysics(false); // Dừng vật lý
            ghostController.ResetVelocity();
        }
        
        // Dừng điểm
        if (scoreCycle != null)
        {
            scoreCycle.StopScore();
        }
        
        // ⭐ THÊM: Dừng vật cản di chuyển
        if (fireLineManager != null)
        {
            fireLineManager.enabled = false; // Tắt script FireLineManager
        }
        
        // ⭐ THÊM: Dừng tất cả animation
        Animator[] allAnimators = FindObjectsOfType<Animator>();
        foreach (Animator anim in allAnimators)
        {
            anim.enabled = false;
        }
        
        // Hiển thị UI Game Over
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            
            // Hiển thị điểm cuối cùng
            if (finalScoreText != null && scoreCycle != null)
            {
                finalScoreText.text = "Final Score: " + scoreCycle.GetScore();
            }
        }
        
        // Dừng thời gian (tùy chọn)
        Time.timeScale = 0f; // ⭐ BẬT LÊN: Đóng băng mọi thứ!
    }
    
    // ==================== NÚT CHƠI LẠI ====================
    public void RestartGame()
    {
        Time.timeScale = 1f; // Reset time scale
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // ==================== NÚT THOÁT ====================
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game!");
    }
}