using UnityEngine;

public class StartGameAudioController : MonoBehaviour
{
    void Start()
    {
        // Phát nhạc menu khi vào StartGameScreen
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }
    }
}