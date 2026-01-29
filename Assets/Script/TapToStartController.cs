using UnityEngine;

/// <summary>
/// ⭐ TAP TO START CONTROLLER
/// - Trong lúc countdown 5s: KHÔNG cho bấm
/// - Sau countdown: Cho phép bấm
/// - Bấm "Swipe to start": Mới chạy Ghost + Score + Hiện vật cản
/// </summary>
public class TapToStartController : MonoBehaviour
{
    public GameObject tapToStartUI;   // UI "Tap to Start"
    public GameObject gameObjects;    // Các đối tượng của game (Ghost)
    public GameObject obstacleManager; // ⭐ ObstacleManager (vật cản)
    public ScoreCycle scoreCycle;     // ScoreCycle
    
    private bool gameStarted = false;
    private bool canInput = false; // ⭐ Cho phép bấm hay không
    
    void Start()
{
    if (scoreCycle == null)
        scoreCycle = FindObjectOfType<ScoreCycle>();
    
    // ⭐ TỰ ĐỘNG TÌM OBSTACLEMANAGER
    if (obstacleManager == null)
        obstacleManager = GameObject.Find("ObstacleManager");
    
    // ⭐⭐ KIỂM TRA XEM CÓ PHẢI SAU COUNTDOWN KHÔNG
    bool afterCountdown = PlayerPrefs.GetInt("AfterCountdown", 0) == 1;
    
    if (afterCountdown)
    {
        // ⭐ SAU COUNTDOWN - Hiện Ghost, ẨN vật cản, đợi Swipe
        gameObjects.SetActive(true);
        
        // ⭐⭐ ẨN VẬT CẢN - CHỜ BẤM SWIPE
        if (obstacleManager != null)
            obstacleManager.SetActive(false);
        
        tapToStartUI.SetActive(true);
        canInput = true; // ⭐ CHO PHÉP BẤM
        // Reset flag
        PlayerPrefs.SetInt("AfterCountdown", 0);
        PlayerPrefs.Save();
        
        Debug.Log("✅ Sau countdown - Đợi Swipe to start");
    }
    else
    {
        // ⭐ LẦN ĐẦU - Ẩn tất cả
        gameObjects.SetActive(false);
        
        // ⭐⭐ ẨN VẬT CẢN
        if (obstacleManager != null)
            obstacleManager.SetActive(false);
        
        tapToStartUI.SetActive(true);
        canInput = false; // ⭐ CHƯA CHO BẤM (đợi countdown)
        
        if (scoreCycle != null)
            scoreCycle.StopScore();
        
        Debug.Log("⏳ Đợi countdown...");
    }
}

    
    void Update()
    {
        // ⭐⭐ CHỈ CHO BẤM KHI canInput = true (sau countdown 5s)
        if (!gameStarted && canInput && Input.GetMouseButtonDown(0))
        {
            StartGame();
        }
    }
    
    void StartGame()
    {
        Debug.Log("🎮 SWIPE TO START - Bắt đầu game!");
        
        tapToStartUI.SetActive(false);
        gameObjects.SetActive(true);
        
        // ⭐⭐ HIỆN VẬT CẢN KHI BẤM SWIPE
        if (obstacleManager != null)
            obstacleManager.SetActive(true);
        
        gameStarted = true;
        
        // ⭐⭐ BẬT GHOST
        GhostController ghost = FindObjectOfType<GhostController>();
        if (ghost != null)
        {
            ghost.EnablePhysics(true);
        }
        
        // ⭐⭐ BẬT SCORE
        if (scoreCycle != null)
        {
            scoreCycle.StartScore();
        }
    }
    
    /// <summary>
    /// ⭐ Public method - Cho phép Count5s gọi sau countdown
    /// </summary>
    public void EnableInput()
    {
        canInput = true;
        Debug.Log("✅ Đã cho phép bấm Swipe to start");
    }
}