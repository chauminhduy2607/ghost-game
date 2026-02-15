using UnityEngine;

public class BackgroundMusicPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Áp dụng setting từ PlayerPrefs
        bool isMusicOn = PlayerPrefs.GetInt("IsMusicOn", 1) == 1;
        
        if (isMusicOn)
        {
            audioSource.volume = 1f;
            audioSource.Play();
        }
        else
        {
            audioSource.volume = 0f;
        }
    }
}