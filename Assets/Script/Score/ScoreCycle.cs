using UnityEngine;
using TMPro;

public class ScoreCycle : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text scoreText;

    [Header("Score Settings")]
    [Min(0.01f)]
    public float secondsPerPlusPoint = 1f;
    
    public bool autoStart = true;

    private int score = 0;
    private float timer = 0f;
    private bool running = false;

    public void SetScore(int newScore)
    {
        score = newScore;
        UpdateScoreText();
        SaveScore();
    }

    void Start()
    {
        score = 0;
        if (autoStart) running = true;
        UpdateScoreText();
        SaveScore();
    }

    void Update()
    {
        if (!running) return;

        timer += Time.deltaTime;

        while (timer >= secondsPerPlusPoint)
        {
            timer -= secondsPerPlusPoint;
            score++;
            UpdateScoreText();
            SaveScore();
        }
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

    public void StartScore() => running = true;
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