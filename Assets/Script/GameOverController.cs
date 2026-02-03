using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Xử lý va chạm và game over
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Header("=== UI GAME OVER ===")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private TMP_Text finalScoreText;
    
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghostController;
    [SerializeField] private ScoreCycle scoreCycle;
    
    private bool isGameOver = false;
    
    void Start()
    {
        if (ghostController == null)
            ghostController = GetComponent<GhostController>();
        
        if (scoreCycle == null)
            scoreCycle = Object.FindFirstObjectByType<ScoreCycle>();

        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Obstacle") && !isGameOver)
        {
            TriggerGameOver();
        }
    }
    
    void TriggerGameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        if (ghostController != null)
        {
            ghostController.EnablePhysics(false);
            ghostController.ResetVelocity();
        }
        
        if (scoreCycle != null)
        {
            scoreCycle.StopScore();
        }
        
        Animator[] allAnimators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (Animator anim in allAnimators)
        {
            anim.enabled = false;
        }
        
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            
            if (finalScoreText != null && scoreCycle != null)
            {
                finalScoreText.text = "Final Score: " + scoreCycle.GetScore();
            }
        }
        
        Time.timeScale = 0f;
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
}