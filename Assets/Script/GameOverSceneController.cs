using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameOverSceneController : MonoBehaviour
{
    [Header("=== UI SCORE ===")]
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    
    [Header("=== BUTTONS ===")]
    [Tooltip("Nút 'Second chance?' - xem rewarded ad")]
    [SerializeField] private GameObject continueButton;
    
    [Tooltip("Nút 'Play Again' - try again không ads")]
    [SerializeField] private GameObject tryAgainButton;
    
    [Header("=== SCENES ===")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string menuSceneName = "MenuScene";
    
    [Header("=== SCOREBOARD TRANSITION ===")]
    [Tooltip("Panel hiển thị CurrentScore")]
    [SerializeField] private GameObject currentScorePanel;
    
    [Tooltip("Panel hiển thị bảng điểm")]
    [SerializeField] private GameObject scoreboardPanel;
    
    [Tooltip("Thời gian chờ trước khi chuyển sang bảng điểm (giây)")]
    [SerializeField] private float transitionDelay = 2f;
    
    private bool isWatchingAd = false;
    
    void Start()
    {
        int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
        if (currentScoreText != null)
            currentScoreText.text = finalScore.ToString();
        
        if (bestScoreText != null)
            bestScoreText.text = bestScore.ToString();
        
        UpdateContinueButton();
        
        // Bắt đầu chuyển đổi sang bảng điểm
        StartScoreboardTransition();
    }
    
    void StartScoreboardTransition()
    {
        // Đảm bảo ban đầu CurrentScore hiển thị, Scoreboard ẩn
        if (currentScorePanel != null)
            currentScorePanel.SetActive(true);
            
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(false);
        
        // Bắt đầu đếm ngược
        StartCoroutine(TransitionToScoreboard());
    }
    
    IEnumerator TransitionToScoreboard()
    {
        // Chờ X giây
        yield return new WaitForSeconds(transitionDelay);
        
        // Tắt CurrentScore
        if (currentScorePanel != null)
            currentScorePanel.SetActive(false);
        
        // Bật Scoreboard
        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(true);
            
            // TODO: Load data cho bảng điểm
            LoadScoreboardData();
        }
    }
    
    void LoadScoreboardData()
    {
        // Tạm thời để trống, sẽ implement sau khi có data source
        Debug.Log("Loading scoreboard data...");
        
        // Example: Bạn có thể load từ PlayerPrefs, file, hoặc server
    }
    
    void UpdateContinueButton()
    {
        if (continueButton == null)
        {
            return;
        }
        
        bool hasRewardedAd = AdsManager.Instance != null 
            && AdsManager.Instance.CanShowRewarded();
        
        Button btn = continueButton.GetComponent<Button>();
        if (btn == null)
            btn = continueButton.GetComponentInChildren<Button>();
        
        if (btn != null)
        {
            btn.interactable = hasRewardedAd;
        }
        
        CanvasGroup canvasGroup = continueButton.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = continueButton.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = hasRewardedAd ? 1f : 0.5f;
    }
    
    public void OnContinueButton()
    {
        if (isWatchingAd)
        {
            return;
        }
        
        if (AdsManager.Instance == null)
        {
            return;
        }
        
        isWatchingAd = true;
        
        bool shown = AdsManager.Instance.ShowRewarded(
            onReward: OnRewardedAdSuccess,
            onClosedOrFailed: OnRewardedAdFailed
        );
        
        if (!shown)
        {
            isWatchingAd = false;
        }
    }
    
    void OnRewardedAdSuccess()
    {
        isWatchingAd = false;
        
        int currentScore = PlayerPrefs.GetInt("FinalScore", 0);
        PlayerPrefs.SetInt("ContinueScore", currentScore);
        
        PlayerPrefs.SetInt("IsContinue", 1);
        
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(gameSceneName);
    }
    
    void OnRewardedAdFailed()
    {
        isWatchingAd = false;
    }
    
    public void OnTryAgainButton()
    {
        PlayerPrefs.SetInt("IsContinue", 0);
        PlayerPrefs.DeleteKey("ContinueScore");
        PlayerPrefs.DeleteKey("RespawnX");
        PlayerPrefs.DeleteKey("RespawnY");
        PlayerPrefs.DeleteKey("RespawnVelocityY");
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void OnMenuButton()
    {
        PlayerPrefs.SetInt("IsContinue", 0);
        PlayerPrefs.DeleteKey("ContinueScore");
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(menuSceneName);
    }
}