using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;
    
    [Header("UI References")]
    public GameObject leaderboardPanel;
    public GameObject entryPrefab;
    public Transform entryContainer;
    public Button closeButton;
    public Button showLeaderboardButton;
    
    [Header("Settings")]
    public int maxEntries = 10;
    
    private List<ScoreEntry> scoreList = new List<ScoreEntry>();
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        LoadScores();
        closeButton.onClick.AddListener(HideLeaderboard);
        
        if (showLeaderboardButton != null)
            showLeaderboardButton.onClick.AddListener(ShowLeaderboard);
        
        leaderboardPanel.SetActive(false);
    }
    
    public void AddScore(float score)
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "Player");
        ScoreEntry newEntry = new ScoreEntry(playerName, score);
        scoreList.Add(newEntry);
        
        // Sắp xếp theo điểm cao nhất
        scoreList = scoreList.OrderByDescending(x => x.score).ToList();
        
        // Giữ chỉ top N
        if (scoreList.Count > maxEntries)
            scoreList.RemoveAt(scoreList.Count - 1);
        
        SaveScores();
    }
    
    public void ShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        UpdateLeaderboardUI();
    }
    
    public void HideLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }
    
    void UpdateLeaderboardUI()
    {
        // Xóa các entry cũ
        foreach (Transform child in entryContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Tạo entry mới
        for (int i = 0; i < scoreList.Count; i++)
        {
            GameObject entry = Instantiate(entryPrefab, entryContainer);
            LeaderboardEntry entryScript = entry.GetComponent<LeaderboardEntry>();
            entryScript.SetEntry(i + 1, scoreList[i].name, scoreList[i].score);
        }
    }
    
    void SaveScores()
    {
        for (int i = 0; i < scoreList.Count; i++)
        {
            PlayerPrefs.SetString("Score_Name_" + i, scoreList[i].name);
            PlayerPrefs.SetFloat("Score_Value_" + i, scoreList[i].score);
        }
        PlayerPrefs.SetInt("Score_Count", scoreList.Count);
        PlayerPrefs.Save();
    }
    
    void LoadScores()
    {
        scoreList.Clear();
        int count = PlayerPrefs.GetInt("Score_Count", 0);
        
        for (int i = 0; i < count; i++)
        {
            string name = PlayerPrefs.GetString("Score_Name_" + i, "");
            float score = PlayerPrefs.GetFloat("Score_Value_" + i, 0);
            scoreList.Add(new ScoreEntry(name, score));
        }
    }
    
    public void ClearLeaderboard()
    {
        scoreList.Clear();
        PlayerPrefs.DeleteAll();
        UpdateLeaderboardUI();
    }
}

[System.Serializable]
public class ScoreEntry
{
    public string name;
    public float score;
    
    public ScoreEntry(string name, float score)
    {
        this.name = name;
        this.score = score;
    }
}