using UnityEngine;
using TMPro;

public class ScoreCycle : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text scoreText;

    [Header("Score Settings")]
    [Min(0.01f)]
    public float secondsPerPlusPoint = 1f;  // ✅ Mỗi 1 giây cộng 1 điểm
    
    public bool autoStart = true;

    private int score = 0;  // ✅ Bắt đầu từ 0
    private float timer = 0f;
    private bool running = false;

    /// <summary>
    /// ⭐ Set điểm trực tiếp (dùng cho Continue)
    /// </summary>
    public void SetScore(int newScore)
    {
        score = newScore;
        UpdateScoreText();
        Debug.Log("📊 Set Score to: " + newScore);
    }

    void Start()
    {
        score = 0;  // ✅ Bắt đầu từ 0
        if (autoStart) running = true;
        UpdateScoreText();
    }

    void Update()
    {
        if (!running) return;

        timer += Time.deltaTime;

        // ✅ CỘNG điểm mỗi giây
        while (timer >= secondsPerPlusPoint)
        {
            timer -= secondsPerPlusPoint;
            score++;
            UpdateScoreText();
        }
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;  // ✅ Chỉ hiển thị điểm
    }

    public void StartScore() => running = true;
    public void StopScore() => running = false;

    public int GetScore() => score;

    public void ResetScore()
    {
        score = 0;  // ✅ Reset về 0
        timer = 0f;
        running = true;
        UpdateScoreText();
    }
}