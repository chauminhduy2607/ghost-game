using UnityEngine;
using TMPro;

public class GameOverController : MonoBehaviour
{
    public TextMeshProUGUI tryAgainText;
    public GameObject player;
    public GameObject obstacles;
    public TextMeshProUGUI scoreText; // Nếu bạn muốn hiển thị điểm ở phần game over

    private bool gameOver = false;

    void Start()
    {
        tryAgainText.gameObject.SetActive(false); // Khi bắt đầu, ẩn nút "Try Again"
    }

    void Update()
    {
        if (gameOver && Input.GetMouseButtonDown(0)) // Kiểm tra khi người chơi bấm nút
        {
            RestartGame();
        }
    }

    // Gọi khi game over
    public void GameOver()
    {
        gameOver = true;
        tryAgainText.gameObject.SetActive(true); // Hiển thị nút "Try Again"
        obstacles.SetActive(false); // Ẩn vật cản khi game over
        player.SetActive(false); // Ẩn người chơi khi game over
        scoreText.text = "Game Over!"; // Hiển thị thông báo game over
    }

    // Chức năng restart game khi bấm vào "Try Again"
    void RestartGame()
    {
        gameOver = false;
        tryAgainText.gameObject.SetActive(false); // Ẩn nút "Try Again"
        player.SetActive(true); // Kích hoạt người chơi lại
        obstacles.SetActive(true); // Kích hoạt vật cản lại
        scoreText.text = "Score: 0"; // Reset điểm
        // Thêm reset lại những giá trị khác nếu cần thiết, ví dụ như timer, score...
    }
}
