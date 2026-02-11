using UnityEngine;

public class GameplaySceneController : MonoBehaviour
{
    void Start()
    {
        // Play nhạc nền gameplay
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }
    }
}