using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 🎮 GAME OVER SCENE CONTROLLER
/// - Continue (Second chance?): Xem Rewarded Ad để tiếp tục
/// - Try Again (Play Again): Chơi lại từ đầu (không ads)
/// </summary>
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
    
    private bool isWatchingAd = false;
    
    void Start()
    {
        // Lấy điểm từ PlayerPrefs
        int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
        // Hiển thị điểm
        if (currentScoreText != null)
            currentScoreText.text = finalScore.ToString();
        
        if (bestScoreText != null)
            bestScoreText.text = bestScore.ToString();
        
        Debug.Log("💀 Game Over Scene Loaded!");
        Debug.Log($"📊 Score: {finalScore} | Best: {bestScore}");
        
        // ✅ Kiểm tra rewarded ad và update button
        UpdateContinueButton();
    }
    
    /// <summary>
    /// ✅ Update trạng thái nút Continue dựa vào ads
    /// </summary>
    void UpdateContinueButton()
    {
        if (continueButton == null)
        {
            Debug.LogWarning("⚠️ Continue Button chưa được gán trong Inspector!");
            return;
        }
        
        bool hasRewardedAd = AdsManager.Instance != null 
            && AdsManager.Instance.CanShowRewarded();
        
        // Get button component (từ chính GameObject hoặc con)
        Button btn = continueButton.GetComponent<Button>();
        if (btn == null)
            btn = continueButton.GetComponentInChildren<Button>();
        
        if (btn != null)
        {
            btn.interactable = hasRewardedAd;
        }
        
        // ✅ Làm mờ nếu không có ads
        CanvasGroup canvasGroup = continueButton.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = continueButton.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = hasRewardedAd ? 1f : 0.5f;
        
        Debug.Log($"🎁 Continue button: {(hasRewardedAd ? "✅ AVAILABLE" : "❌ NO ADS")}");
    }
    
    /// <summary>
    /// 🎁 CONTINUE - Xem Rewarded Ad để tiếp tục
    /// Gọi từ nút "Second chance?"
    /// </summary>
    public void OnContinueButton()
    {
        if (isWatchingAd)
        {
            Debug.LogWarning("⚠️ Đang xem ad rồi, không spam!");
            return;
        }
        
        if (AdsManager.Instance == null)
        {
            Debug.LogError("❌ AdsManager NULL!");
            return;
        }
        
        Debug.Log("🎁 CONTINUE clicked - Show Rewarded Ad...");
        isWatchingAd = true;
        
        // ✅ Show Rewarded Ad
        bool shown = AdsManager.Instance.ShowRewarded(
            onReward: OnRewardedAdSuccess,
            onClosedOrFailed: OnRewardedAdFailed
        );
        
        if (!shown)
        {
            Debug.LogWarning("❌ ShowRewarded() returned false - no ads available!");
            isWatchingAd = false;
            
            // Optional: Hiện popup báo không có ads
            // ShowNoAdsPopup();
        }
    }
    
    /// <summary>
    /// ✅ User xem xong rewarded ad → Cho phép continue
    /// </summary>
    void OnRewardedAdSuccess()
    {
        Debug.Log("✅ REWARDED AD SUCCESS - User earned second chance!");
        
        isWatchingAd = false;
        
        // ✅ Lưu TOÀN BỘ thông tin để continue
        int currentScore = PlayerPrefs.GetInt("FinalScore", 0);
        PlayerPrefs.SetInt("ContinueScore", currentScore);
        
        // ✅ Đánh dấu Continue mode
        PlayerPrefs.SetInt("IsContinue", 1);
        
        // ✅ Respawn data đã được lưu trong GhostController.OnObstacleHit()
        // Chỉ cần set flag IsContinue = 1 là đủ
        
        PlayerPrefs.Save();
        
        Debug.Log($"💾 Continue enabled | Score={currentScore} | Respawn data ready");
        
        // Load lại game
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// ❌ User đóng ad sớm hoặc ad failed
    /// </summary>
    void OnRewardedAdFailed()
    {
        Debug.Log("❌ REWARDED AD CLOSED/FAILED - No reward");
        
        isWatchingAd = false;
        
        // Không làm gì - user vẫn ở màn Game Over
        // Có thể thử lại hoặc chọn Try Again
    }
    
    /// <summary>
    /// 🔄 TRY AGAIN - Chơi lại từ đầu (KHÔNG ADS)
    /// Gọi từ nút "Play Again"
    /// </summary>
    public void OnTryAgainButton()
    {
        Debug.Log("🔄 TRY AGAIN - Chơi lại từ đầu (no ads)");
        
        // Xóa toàn bộ continue data
        PlayerPrefs.SetInt("IsContinue", 0);
        PlayerPrefs.DeleteKey("ContinueScore");
        PlayerPrefs.DeleteKey("RespawnX");
        PlayerPrefs.DeleteKey("RespawnY");
        PlayerPrefs.DeleteKey("RespawnVelocityY");
        PlayerPrefs.Save();
        
        // Load lại game từ đầu
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// 🏠 Về menu (nếu có nút Home)
    /// </summary>
    public void OnMenuButton()
    {
        Debug.Log("🏠 GO TO MENU");
        
        PlayerPrefs.SetInt("IsContinue", 0);
        PlayerPrefs.DeleteKey("ContinueScore");
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(menuSceneName);
    }
}