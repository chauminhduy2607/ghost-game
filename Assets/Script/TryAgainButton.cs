using UnityEngine;
using UnityEngine.SceneManagement;

public class TryAgainButton : MonoBehaviour
{
    public GameObject tryAgainButton;  // Đối tượng nút "Try Again" 
    public GameObject gameUI;          // UI của game chính
    public GameObject scoreUI;         // UI của điểm số
    public GameObject obstacleManager; // Quản lý các vật cản trong game

    void Start()
    {
        // Ẩn nút "Try Again" khi game bắt đầu
        tryAgainButton.SetActive(false);
        // Ẩn UI điểm số (nếu cần)
        scoreUI.SetActive(true);
        // Đảm bảo các đối tượng UI của game đang hoạt động
        gameUI.SetActive(true);
        obstacleManager.SetActive(true);
    }

    public void GameOver() 
    {
        // Khi game kết thúc, ẩn các đối tượng UI của game chính
        gameUI.SetActive(false);
        scoreUI.SetActive(false);
        obstacleManager.SetActive(false);

        // Hiển thị nút "Try Again"
        tryAgainButton.SetActive(true);

    }

    // Phương thức này sẽ được gọi khi bấm vào nút "Try Again"
    public void RestartGame()
    {
        // Tải lại scene hiện tại để bắt đầu lại trò chơi
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
