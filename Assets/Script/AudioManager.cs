using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    
    [Header("Music Clips")]
    public AudioClip gameplayMusic;
    public AudioClip gameOverMusic;
    
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
        // Singleton pattern
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
        
        // Load saved volumes
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        if (musicSource != null)
            musicSource.volume = musicVolume;
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }
    
    #region Music Methods
    
    /// <summary>
    /// Play nhạc nền gameplay
    /// </summary>
    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }
    
    /// <summary>
    /// Play nhạc nền game over
    /// </summary>
    public void PlayGameOverMusic()
    {
        PlayMusic(gameOverMusic);
    }
    
    /// <summary>
    /// Play nhạc bất kỳ
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        
        // Nếu đang play nhạc khác, stop trước
        if (musicSource.isPlaying)
            musicSource.Stop();
        
        musicSource.clip = clip;
        musicSource.Play();
    }
    
    /// <summary>
    /// Dừng nhạc
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
    
    /// <summary>
    /// Pause nhạc
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource != null)
            musicSource.Pause();
    }
    
    /// <summary>
    /// Resume nhạc
    /// </summary>
    public void ResumeMusic()
    {
        if (musicSource != null)
            musicSource.UnPause();
    }
    
    #endregion
    
    #region SFX Methods
    
    /// <summary>
    /// Play SFX click button
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSFX);
    }
    
    /// <summary>
    /// Play SFX tap ghost
    /// </summary>
    public void PlayGhostTap()
    {
        PlaySFX(ghostTapSFX);
    }
    
    /// <summary>
    /// Play SFX đụng obstacle
    /// </summary>
    public void PlayObstacleHit()
    {
        PlaySFX(obstacleHitSFX);
    }
    
    /// <summary>
    /// Play SFX bất kỳ
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        
        sfxSource.PlayOneShot(clip);
    }
    
    /// <summary>
    /// Play SFX với volume tùy chỉnh
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;
        
        sfxSource.PlayOneShot(clip, volume);
    }
    
    #endregion
    
    #region Volume Control
    
    /// <summary>
    /// Set volume nhạc
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
        
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Set volume SFX
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
        
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Toggle music on/off
    /// </summary>
    public void ToggleMusic()
    {
        if (musicSource != null)
        {
            musicSource.mute = !musicSource.mute;
            PlayerPrefs.SetInt("MusicMuted", musicSource.mute ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// Toggle SFX on/off
    /// </summary>
    public void ToggleSFX()
    {
        if (sfxSource != null)
        {
            sfxSource.mute = !sfxSource.mute;
            PlayerPrefs.SetInt("SFXMuted", sfxSource.mute ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
    
    
    #endregion
}