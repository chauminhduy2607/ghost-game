using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public TMP_Text scoreText;
    public float scoreInterval = 2f; // 2 giây +1 điểm

    private int score = 0;
    private bool isRunning = true;

    void Start()
    {
        UpdateScoreUI();
        StartCoroutine(AddScoreRoutine());
    }

    IEnumerator AddScoreRoutine()
    {
        while (isRunning)
        {
            yield return new WaitForSeconds(scoreInterval);
            score += 1;
            UpdateScoreUI();
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    // gọi khi game over
    public void StopScore()
    {
        isRunning = false;
    }

    public int GetScore()
    {
        return score;
    }

    public void ResetScore()
    {
        score = 0;
        isRunning = true;
        UpdateScoreUI();
        StopAllCoroutines();
        StartCoroutine(AddScoreRoutine());
    }
}
