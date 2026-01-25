using UnityEngine;
using TMPro;

public class ScoreCycle : MonoBehaviour
{

    
    [Header("UI")]
    public TMP_Text scoreText;

    [Header("Score Settings")]
    public int startScore = 100;   // Điểm bắt đầu từ 100
    [Min(0.01f)]
    public float secondsPerMinusPoint = 2f;  // Mỗi giây trừ 1 điểm

    [Header("Multiplier Settings")]
    public int multiplier = 10;   // Mức độ nhân (100 -> 1000 -> 10000...)
    
    public bool autoStart = true;

    private int score;
    private float timer = 0f;
    private bool running = false;
    [SerializeField] private ScoreCycle scoreCycle;


    //// <summary>
/// ⭐ Set điểm trực tiếp (dùng cho Continue)
/// </summary>
public void SetScore(int newScore)
{
    score = newScore;
    startScore = newScore;
    UpdateScoreText();
    Debug.Log("📊 Set Score to: " + newScore);
}
    void Start()
    {
        score = startScore;
        if (autoStart) running = true;
        UpdateScoreText();
    }

    void Update()
    {
        if (!running) return;
        if (score <= 0) return;

        timer += Time.deltaTime;

        // Trừ điểm mỗi giây
        while (timer >= secondsPerMinusPoint && score > 0)
        {
            timer -= secondsPerMinusPoint;
            score--;
            UpdateScoreText();

            // Khi điểm giảm về 0
            if (score <= 0)
            {
                score = 0;
                running = false;
                UpdateScoreText();
                StartNextRound();  // Ngay lập tức qua vòng mới
            }
        }
    }

    void StartNextRound()
    {
        // Nhân điểm lên 10 lần ngay lập tức (không delay)
        startScore *= multiplier;
        ResetScore();  // Reset lại điểm cho vòng tiếp theo
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    public void StartScore() => running = true;
    public void StopScore() => running = false;

    public int GetScore() => score;

    public void ResetScore()
    {
        score = startScore;  // Dùng startScore mới
        timer = 0f;
        running = true;
        UpdateScoreText();
    }
}
