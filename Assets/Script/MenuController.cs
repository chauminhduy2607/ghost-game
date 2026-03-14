using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu Controller - Kết hợp đầy đủ chức năng
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("=== BUTTONS ===")]
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject settingButton;
    [SerializeField] private GameObject adsButton;
    
    [Header("=== SCENE NAMES ===")]
    [SerializeField] private string gameplaySceneName = "GameplayScreen";
    [SerializeField] private string settingSceneName = "SettingScreen";
    [SerializeField] private string adsSceneName = "StartGameScreen";
    
    // ==================== NÚT PLAY ====================
    public void OnPlayButton()
    {
        Debug.Log("🎮 Play Game!");
        
        // Set PlayerPrefs như MenuPlayButton cũ
        PlayerPrefs.SetInt("SkipCountdown", 1);
        PlayerPrefs.SetInt("IsContinue", 0);
        PlayerPrefs.Save();
        
        // Load scene
        LoadScene(gameplaySceneName);
    }
    
    // ==================== NÚT SETTING ====================
    public void OnSettingButton()
    {
        Debug.Log("⚙️ Loading Setting!");
        LoadScene(settingSceneName);
    }
    
    // ==================== NÚT ADS ====================
    public void OnAdsButton()
    {
        Debug.Log("📺 Loading Ads!");
        LoadScene(adsSceneName);
    }
    
    // ==================== LOAD SCENE AN TOÀN ====================
    void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("❌ Scene name is empty!");
            return;
        }
        
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
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