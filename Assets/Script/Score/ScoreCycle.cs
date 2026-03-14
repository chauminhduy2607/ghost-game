using UnityEngine;
using TMPro;

public class ScoreCycle : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text scoreText;

    [Header("Score Settings")]
    public float secondsPerPlusPoint = 3f;
    public bool autoStart = true;

    private int score = 0;
    private float timer = 0f;
    private bool running = false;

    void Start()
    {
        score = 0;
        timer = 0f;
        running = false; // LUÔN false lúc đầu, chờ swipe
        UpdateScoreText();
        SaveScore();
    }
    void Update()
    {
        if (!running) return;

        timer += Time.deltaTime;

        if (timer >= secondsPerPlusPoint)
        {
            timer = 0f; // reset sạch, không dùng while để tránh nhảy nhiều lần
            score++;
            UpdateScoreText();
            SaveScore();
        }
    }

    /// <summary>Gọi khi ghost đi qua vật cản</summary>
    public void AddObstacleBonus(int bonus = 2)
    {
        score += bonus;
        UpdateScoreText();
        SaveScore();
    }

    public void SetScore(int newScore)
    {
        score = newScore;
        timer = 0f;
        UpdateScoreText();
        SaveScore();
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    void SaveScore()
    {
        PlayerPrefs.SetInt("FinalScore", score);
    }

    public void StartScore()
    {
        running = true;
        timer = 0f;
    }

    public void StopScore() => running = false;
    public int GetScore() => score;

    public void ResetScore()
    {
        score = 0;
        timer = 0f;
        running = true;
        UpdateScoreText();
        SaveScore();
    }

    void OnDestroy()
    {
        SaveScore();
        PlayerPrefs.Save();
    }
}