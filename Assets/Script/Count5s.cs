using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// ⏱️ ĐẾM NGƯỢC 5 GIÂY TRƯỚC KHI CHƠI
/// ⭐ KHÔNG ĐỘNG ĐẾN VẬT CẢN - ĐỂ CHÚNG LOOP BÌNH THƯỜNG
/// </summary>
public class Count5s : MonoBehaviour
{
    [Header("=== UI ===")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text countdownText;
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private int countdownFrom = 5;
    
    [Header("=== REFERENCE ===")]
    [SerializeField] private GhostController ghostController;
    [SerializeField] private ScoreCycle scoreCycle;
    [SerializeField] private GhostAI ghostAI;
    
    private bool isCountingDown = false;
    
    void Start()
    {
        // ✅ Nếu bấm PLAY từ menu thì skip countdown
        if (PlayerPrefs.GetInt("SkipCountdown", 0) == 1)
        {
            PlayerPrefs.SetInt("SkipCountdown", 0);
            PlayerPrefs.Save();

            if (countdownPanel != null) 
                countdownPanel.SetActive(false);
            
            ResumeGame();
            return;
        }
        
        // Tự động tìm components
        if (ghostController == null)
            ghostController = FindObjectOfType<GhostController>();
        
        if (scoreCycle == null)
            scoreCycle = FindObjectOfType<ScoreCycle>();
        
        if (ghostAI == null)
            ghostAI = FindObjectOfType<GhostAI>();
        
        // ⭐⭐ KIỂM TRA CONTINUE
        bool isContinue = PlayerPrefs.GetInt("IsContinue", 0) == 1;
        
        if (isContinue)
        {
            // CONTINUE: Hồi sinh Ghost về vị trí chạm vật cản
            float respawnX = PlayerPrefs.GetFloat("RespawnX", 0f);
            float respawnY = PlayerPrefs.GetFloat("RespawnY", 0f);
            float respawnVelY = PlayerPrefs.GetFloat("RespawnVelocityY", 0f);
            
            if (ghostController != null)
            {
                // Teleport về vị trí chạm vật cản
                ghostController.transform.position = new Vector3(respawnX, respawnY, 0);
                
                // Khôi phục vận tốc
                if (ghostController.Rigidbody != null)
                {
                    ghostController.Rigidbody.linearVelocity = new Vector2(0, respawnVelY);
                }
                
                // Reset trạng thái Ghost
                ghostController.ResetGame();
                
                Debug.Log("🔄 RESPAWN at: (" + respawnX + ", " + respawnY + ") | Vel: " + respawnVelY);
            }
            
            // Khôi phục điểm cũ
            int savedScore = PlayerPrefs.GetInt("ContinueScore", 100);
            if (scoreCycle != null)
            {
                scoreCycle.SetScore(savedScore);
            }
            
            PlayerPrefs.SetInt("IsContinue", 0); // Reset flag
            PlayerPrefs.Save();
            
            Debug.Log("▶️ CONTINUE: Score = " + savedScore);
        }
        
        // Bắt đầu đếm ngược
        StartCountdown();
    }
    
    /// <summary>
    /// Bắt đầu đếm ngược
    /// </summary>
    public void StartCountdown()
    {
        if (isCountingDown) return;
        
        // Hiện panel đếm ngược
        if (countdownPanel != null)
            countdownPanel.SetActive(true);
        
        // Dừng mọi thứ (KHÔNG TẮT VẬT CẢN!)
        PauseGame();
        
        // Bắt đầu coroutine đếm ngược
        StartCoroutine(CountdownCoroutine());
    }
    
    /// <summary>
    /// Coroutine đếm ngược
    /// </summary>
    IEnumerator CountdownCoroutine()
    {
        isCountingDown = true;
        
        for (int i = countdownFrom; i > 0; i--)
        {
            // Hiển thị số
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                countdownText.fontSize = 120;
            }
            
            Debug.Log("⏱️ Countdown: " + i);
            
            // Đợi 1 giây (realtime - không bị ảnh hưởng bởi Time.timeScale)
            yield return new WaitForSecondsRealtime(1f);
        }
        
        // Hiển thị "GO!"
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            countdownText.fontSize = 100;
        }
        
        yield return new WaitForSecondsRealtime(0.5f);
        
        // Ẩn panel
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
        
        // Bắt đầu game
        ResumeGame();
        
        isCountingDown = false;
        
        Debug.Log("🎮 START GAME!");
    }
    
    /// <summary>
    /// Tạm dừng game
    /// ⭐ CHỈ TẮT GHOST VÀ SCORE - KHÔNG TẮT VẬT CẢN!
    /// </summary>
    void PauseGame()
    {
        // Dừng Ghost (không cho điều khiển)
        if (ghostController != null)
        {
            ghostController.EnablePhysics(false);
            Debug.Log("⏸️ Tắt Ghost physics");
        }
        
        // Dừng Score (không tăng điểm)
        if (scoreCycle != null)
        {
            scoreCycle.StopScore();
            Debug.Log("⏸️ Dừng Score");
        }
        
        // ⭐⭐ KHÔNG TẮT OBSTACLEMANAGER - ĐỂ VẬT CẢN VẪN LOOP!
        // Vật cản vẫn spawn và di chuyển bình thường trong lúc countdown
        
        // Dừng AI (nếu có)
        if (ghostAI != null)
        {
            ghostAI.SetAIEnabled(false);
            Debug.Log("⏸️ Tắt AI");
        }
    }
    
    /// <summary>
    /// Tiếp tục game
    /// </summary>
    void ResumeGame()
    {
        // Bật Ghost
        if (ghostController != null)
        {
            ghostController.EnablePhysics(true);
            Debug.Log("▶️ Bật Ghost physics");
        }
        
        // Bật Score
        if (scoreCycle != null)
        {
            scoreCycle.StartScore();
            Debug.Log("▶️ Bắt đầu Score");
        }
        
        // ⭐⭐ KHÔNG CẦN BẬT GÌ - VẬT CẢN ĐÃ ĐANG CHẠY!
        
        // Bật AI nếu có
        if (ghostAI != null)
        {
            bool aiEnabled = PlayerPrefs.GetInt("AIEnabled", 0) == 1;
            ghostAI.SetAIEnabled(aiEnabled);
            
            if (aiEnabled)
                Debug.Log("▶️ Bật AI");
        }
    }
}