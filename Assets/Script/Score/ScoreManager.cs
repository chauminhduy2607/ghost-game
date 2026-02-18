using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// ScoreManager chỉ là wrapper delegate sang ScoreCycle.
/// Không tự chạy logic điểm, tránh trùng lặp.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public TMP_Text scoreText;
    public float scoreInterval = 2f;

    private ScoreCycle scoreCycle;

    void Awake()
    {
        scoreCycle = FindAnyObjectByType<ScoreCycle>();
    }

    void Start()
    {
        // Không tự chạy gì - ScoreCycle lo hết
    }

    public void StopScore()
    {
        if (scoreCycle != null) scoreCycle.StopScore();
    }

    public int GetScore()
    {
        if (scoreCycle != null) return scoreCycle.GetScore();
        return 0;
    }

    public void ResetScore()
    {
        if (scoreCycle != null) scoreCycle.ResetScore();
    }

    public void SetScore(int newScore)
    {
        if (scoreCycle != null) scoreCycle.SetScore(newScore);
    }

    public void ResumeScore()
    {
        if (scoreCycle != null) scoreCycle.StartScore();
    }
}