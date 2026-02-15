using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private GameObject nicknamePopup;
    [SerializeField] private TMP_InputField nicknameInput; // Hoặc TMP_Text nếu bạn dùng Text
    [SerializeField] private GameObject scoreboardPanel;
    
    [Header("Scoreboard UI - Top 3")]
    [SerializeField] private TMP_Text rank1Name;
    [SerializeField] private TMP_Text rank1Time;
    [SerializeField] private TMP_Text rank1Day;
    
    [SerializeField] private TMP_Text rank2Name;
    [SerializeField] private TMP_Text rank2Time;
    [SerializeField] private TMP_Text rank2Day;
    
    [SerializeField] private TMP_Text rank3Name;
    [SerializeField] private TMP_Text rank3Time;
    [SerializeField] private TMP_Text rank3Day;
    
    private DatabaseReference leaderboardRef;
    private int currentScore;
    private string currentTime;
    private int currentDay;
    private string currentNickname;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        if (FirebaseInitializer.Instance != null && FirebaseInitializer.Instance.IsInitialized)
        {
            leaderboardRef = FirebaseInitializer.Instance.DatabaseRef.Child("leaderboard");
        }
        
        // Ẩn scoreboard ban đầu
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(false);
    }
    
    // Gọi hàm này khi game over
    public void ShowNicknamePopup(int score, string time, int day)
    {
        currentScore = score;
        currentTime = time;
        currentDay = day;
        
        if (nicknamePopup != null)
        {
            nicknamePopup.SetActive(true);
        }
    }
    
    // Gọi khi nhấn nút Save trong popup
    // Gọi khi nhấn nút Save trong popup
    public void OnSaveNickname()
    {
        Debug.Log("🔴 SAVE BUTTON CLICKED!"); // THÊM DÒNG NÀY
        
        string nickname = nicknameInput.text.Trim();
        
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("Nickname is empty!");
            nickname = "Player";
        }
        
        currentNickname = nickname;
        
        Debug.Log("🔴 CLOSING POPUP..."); // THÊM DÒNG NÀY
        
        // TẮT POPUP NGAY LẬP TỨC
        if (nicknamePopup != null)
            nicknamePopup.SetActive(false);
        
        Debug.Log("🔴 POPUP CLOSED!"); // THÊM DÒNG NÀY
        
        // Kiểm tra xem có đủ điểm để lưu không
        CheckAndSaveScore();
    }
    
    private void CheckAndSaveScore()
    {
        if (leaderboardRef == null)
        {
            Debug.LogWarning("⚠️ Firebase not initialized! Showing local score only...");
            LoadTop3Scores(); // Hiện scoreboard ngay
            return;
        }
        
        Debug.Log("🔍 Checking if score is high enough to save...");
        
        // Lấy top 3 hiện tại từ Firebase
        leaderboardRef.OrderByChild("score").LimitToLast(3)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Failed to check scores: " + task.Exception);
                    LoadTop3Scores(); // Vẫn hiện scoreboard
                    return;
                }
                
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    
                    List<ScoreData> topScores = new List<ScoreData>();
                    
                    foreach (DataSnapshot childSnapshot in snapshot.Children)
                    {
                        string json = childSnapshot.GetRawJsonValue();
                        ScoreData data = JsonUtility.FromJson<ScoreData>(json);
                        topScores.Add(data);
                    }
                    
                    // Sắp xếp theo score giảm dần
                    topScores = topScores.OrderByDescending(s => s.score).ToList();
                    
                    // Kiểm tra xem có đủ điểm để vào top 3 không
                    bool shouldSave = false;
                    
                    if (topScores.Count < 3)
                    {
                        // Chưa đủ 3 người, luôn lưu
                        shouldSave = true;
                        Debug.Log($"✅ Leaderboard has only {topScores.Count} entries. Saving score...");
                    }
                    else
                    {
                        // Có đủ 3 người rồi, kiểm tra điểm thấp nhất (rank 3)
                        int lowestScore = topScores[2].score;
                        
                        if (currentScore > lowestScore)
                        {
                            shouldSave = true;
                            Debug.Log($"✅ Score {currentScore} > lowest score {lowestScore}. Saving...");
                        }
                        else
                        {
                            Debug.Log($"⏭️ Score {currentScore} <= lowest score {lowestScore}. Not saving.");
                        }
                    }
                    
                    // Lưu nếu đủ điều kiện
                    if (shouldSave)
                    {
                        SaveScoreToFirebase(currentNickname);
                    }
                    else
                    {
                        // Không đủ điểm, chỉ hiện scoreboard
                        LoadTop3Scores();
                    }
                }
            });
    }
    
    private void SaveScoreToFirebase(string nickname)
    {
        if (leaderboardRef == null)
        {
            Debug.LogWarning("⚠️ Firebase not initialized!");
            LoadTop3Scores();
            return;
        }
        
        // Tạo data
        ScoreData scoreData = new ScoreData(nickname, currentScore, currentTime, currentDay);
        
        // Convert to JSON
        string json = JsonUtility.ToJson(scoreData);
        
        // Lưu với key là timestamp để unique
        string key = leaderboardRef.Push().Key;
        
        Debug.Log($"💾 Saving score to Firebase: {nickname} - {currentScore}");
        
        leaderboardRef.Child(key).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("✅ Score saved to Firebase!");
                    
                    // Reload lại top 3 sau khi lưu thành công
                    LoadTop3Scores();
                }
                else
                {
                    Debug.LogError("❌ Failed to save score: " + task.Exception);
                    LoadTop3Scores(); // Vẫn hiện scoreboard
                }
            });
    }
    
    public void LoadTop3Scores()
    {
        // HIỆN SCOREBOARD NGAY
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(true);
        
        if (leaderboardRef == null)
        {
            Debug.LogWarning("⚠️ Firebase not initialized! Showing empty leaderboard...");
            DisplayTop3(new List<ScoreData>());
            return;
        }
        
        // Query top scores (sắp xếp theo score giảm dần, lấy 3 người)
        leaderboardRef.OrderByChild("score").LimitToLast(3)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Failed to load scores: " + task.Exception);
                    DisplayTop3(new List<ScoreData>());
                    return;
                }
                
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    
                    List<ScoreData> scores = new List<ScoreData>();
                    
                    foreach (DataSnapshot childSnapshot in snapshot.Children)
                    {
                        string json = childSnapshot.GetRawJsonValue();
                        ScoreData data = JsonUtility.FromJson<ScoreData>(json);
                        scores.Add(data);
                    }
                    
                    // Sắp xếp theo score giảm dần
                    scores = scores.OrderByDescending(s => s.score).ToList();
                    
                    // Hiển thị top 3
                    DisplayTop3(scores);
                }
            });
    }
    
    private void DisplayTop3(List<ScoreData> scores)
    {
        // Rank 1
        if (scores.Count > 0)
        {
            if (rank1Name != null) rank1Name.text = scores[0].nickname;
            if (rank1Time != null) rank1Time.text = scores[0].time;
            if (rank1Day != null) rank1Day.text = scores[0].day.ToString();
        }
        else
        {
            ClearRank1();
        }
        
        // Rank 2
        if (scores.Count > 1)
        {
            if (rank2Name != null) rank2Name.text = scores[1].nickname;
            if (rank2Time != null) rank2Time.text = scores[1].time;
            if (rank2Day != null) rank2Day.text = scores[1].day.ToString();
        }
        else
        {
            ClearRank2();
        }
        
        // Rank 3
        if (scores.Count > 2)
        {
            if (rank3Name != null) rank3Name.text = scores[2].nickname;
            if (rank3Time != null) rank3Time.text = scores[2].time;
            if (rank3Day != null) rank3Day.text = scores[2].day.ToString();
        }
        else
        {
            ClearRank3();
        }
        
        Debug.Log($"✅ Displayed top {scores.Count} scores");
    }
    
    private void ClearRank1()
    {
        if (rank1Name != null) rank1Name.text = "-";
        if (rank1Time != null) rank1Time.text = "-";
        if (rank1Day != null) rank1Day.text = "-";
    }
    
    private void ClearRank2()
    {
        if (rank2Name != null) rank2Name.text = "-";
        if (rank2Time != null) rank2Time.text = "-";
        if (rank2Day != null) rank2Day.text = "-";
    }
    
    private void ClearRank3()
    {
        if (rank3Name != null) rank3Name.text = "-";
        if (rank3Time != null) rank3Time.text = "-";
        if (rank3Day != null) rank3Day.text = "-";
    }
}