using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManagerSceneTransition : MonoBehaviour
{
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghostController;
    [SerializeField] private ScoreCycle scoreCycle;
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";
    
    [Tooltip("Delay ngắn trước khi chuyển scene (cho mượt)")]
    [SerializeField] private float delayBeforeTransition = 0.5f;
    
    private bool isTransitioning = false;
    
    void Start()
    {
        if (ghostController == null)
            ghostController = FindObjectOfType<GhostController>();
        
        if (scoreCycle == null)
            scoreCycle = FindObjectOfType<ScoreCycle>();
    }
    
    void Update()
    {
        if (!isTransitioning && ghostController != null && ghostController.IsGameOver)
        {
            isTransitioning = true;
            StartCoroutine(TransitionToGameOver());
        }
    }
    
    IEnumerator TransitionToGameOver()
    {
        yield return new WaitForSeconds(delayBeforeTransition);
        
        SaveScoreAndTransition();
    }
    
    void SaveScoreAndTransition()
    {
        int finalScore = 0;
        if (scoreCycle != null)
        {
            finalScore = scoreCycle.GetScore();
        }
        
        PlayerPrefs.SetInt("FinalScore", finalScore);
        PlayerPrefs.SetInt("ContinueScore", finalScore);
        
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        if (finalScore > bestScore)
        {
            bestScore = finalScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
        }
        
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(gameOverSceneName);
    }
}