using UnityEngine;

public class TapToStartController : MonoBehaviour
{
    public GameObject tapToStartUI;   // UI "Tap to Start"
    public GameObject gameObjects;    // Các đối tượng của game
    public ScoreCycle scoreCycle;     // ĐỔI TỪ ScoreManager SANG ScoreCycle

    private bool gameStarted = false;

    void Start()
{
    if (scoreCycle == null)
        scoreCycle = FindObjectOfType<ScoreCycle>();

    gameObjects.SetActive(false);
    tapToStartUI.SetActive(true);

    if (scoreCycle != null)
        scoreCycle.StopScore();
}


    void Update()
    {
        if (!gameStarted && Input.GetMouseButtonDown(0))
        {
            StartGame();
        }
    }

    void StartGame()
    {
        tapToStartUI.SetActive(false);
        gameObjects.SetActive(true);
        gameStarted = true;
        
        // Bắt đầu tăng điểm
        scoreCycle.StartScore();
    }
}