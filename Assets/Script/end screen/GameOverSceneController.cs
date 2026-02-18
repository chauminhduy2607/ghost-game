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
    [Tooltip("Màn hình đếm ngược 3s")]
    [SerializeField] private GameObject countdownPanel;
    
    [Tooltip("Panel hiển thị CurrentScore")]
    [SerializeField] private GameObject currentScorePanel;
    
    [Tooltip("Thời gian chờ sau khi hiện điểm trước khi hiện popup (giây)")]
    [SerializeField] private float popupDelay = 2f;
    
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
        StartCoroutine(GameOverFlow());
    }
    
    IEnumerator GameOverFlow()
    {
        if (currentScorePanel != null)
            currentScorePanel.SetActive(false);
        
        yield return new WaitForSeconds(3f);
        
        if (currentScorePanel != null)
            currentScorePanel.SetActive(true);
        
        yield return new WaitForSeconds(popupDelay);
        
        if (currentScorePanel != null)
            currentScorePanel.SetActive(false);
        
        if (LeaderboardManager.Instance != null)
        {
            int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
            string gameTime = "00:45";
            int gameDay = 1;
            
            LeaderboardManager.Instance.ShowNicknamePopup(finalScore, gameTime, gameDay);
        }
    }
    
    void UpdateContinueButton()
    {
        if (continueButton == null)
            return;
        
        bool hasRewardedAd = AdsManager.Instance != null 
            && AdsManager.Instance.CanShowRewarded();
        
        Button btn = continueButton.GetComponent<Button>();
        if (btn == null)
            btn = continueButton.GetComponentInChildren<Button>();
        
        if (btn != null)
            btn.interactable = hasRewardedAd;
        
        CanvasGroup canvasGroup = continueButton.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = continueButton.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = hasRewardedAd ? 1f : 0.5f;
    }
    
    public void OnContinueButton()
    {
        if (isWatchingAd)
            return;
        
        if (AdsManager.Instance == null)
            return;
        
        isWatchingAd = true;
        
        bool shown = AdsManager.Instance.ShowRewarded(
            onReward: OnRewardedAdSuccess,
            onClosedOrFailed: OnRewardedAdFailed
        );
        
        if (!shown)
            isWatchingAd = false;
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