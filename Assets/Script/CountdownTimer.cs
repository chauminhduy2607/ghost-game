using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text scoreText;

    [Header("WIN UI")]
    public GameObject winGameObject;   // kéo object chữ "You win" vào đây

    [Header("Score Settings")]
    public int startScore = 30;
    [Min(0.01f)]
    public float secondsPerMinusPoint = 2f;

    public bool autoStart = true;

    private int score;
    private float timer = 0f;
    private bool running = false;
    private bool winTriggered = false;

    void Start()
    {
        score = startScore;
        if (autoStart) running = true;
        UpdateScoreText();

        // ẩn You win lúc mới chơi
        if (winGameObject != null)
            winGameObject.SetActive(false);

        Time.timeScale = 1f; // đảm bảo game không bị pause từ lần trước
    }

    void Update()
    {
        if (!running) return;
        if (score <= 0) return;

        timer += Time.deltaTime;

        while (timer >= secondsPerMinusPoint && score > 0)
        {
            timer -= secondsPerMinusPoint;
            score--;
            UpdateScoreText();

            if (score <= 0)
            {
                score = 0;
                running = false;
                UpdateScoreText();
                TriggerWin();   // ✅ gọi Win ở đây
            }
        }
    }

    void TriggerWin()
    {
        if (winTriggered) return;
        winTriggered = true;

        if (winGameObject != null)
            winGameObject.SetActive(true);

        Time.timeScale = 0f; // ✅ dừng game
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
        score = startScore;
        timer = 0f;
        running = true;
        winTriggered = false;

        if (winGameObject != null)
            winGameObject.SetActive(false);

        Time.timeScale = 1f;
        UpdateScoreText();
    }
}
