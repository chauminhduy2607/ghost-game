using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    
    [Header("Music Clips")]
    public AudioClip menuMusic;           // Nhạc menu (StartGameScreen)
    public AudioClip gameplayMusic;       // Nhạc gameplay
    public AudioClip gameOverMusic;       // Nhạc game over
    
    [Header("SFX Clips")]
    public AudioClip buttonClickSFX;
    public AudioClip ghostTapSFX;
    public AudioClip obstacleHitSFX;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        if (musicSource != null)
            musicSource.volume = musicVolume;
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }
    
    #region Music Methods
    
    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }
    
    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }
    
    public void PlayGameOverMusic()
    {
        PlayMusic(gameOverMusic);
    }
    
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        
        if (musicSource.isPlaying)
            musicSource.Stop();
        
        musicSource.clip = clip;
        musicSource.Play();
    }
    
    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
    
    #endregion
    
    #region SFX Methods
    
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSFX);
    }
    
    public void PlayGhostTap()
    {
        PlaySFX(ghostTapSFX);
    }
    
    public void PlayObstacleHit()
    {
        PlaySFX(obstacleHitSFX);
    }
    
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
    
    #endregion
}