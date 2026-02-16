using UnityEngine;

public class MultiBackgroundMusicPlayer : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Danh sách các file nhạc muốn phát cùng lúc")]
    public AudioClip[] musicClips;
    
    [Header("Settings")]
    [Tooltip("Âm lượng cho từng track (0-1)")]
    public float[] volumes;
    
    [Tooltip("Bật loop cho từng track")]
    public bool[] loops;
    
    private AudioSource[] audioSources;
    
    void Start()
    {
        if (musicClips == null || musicClips.Length == 0)
        {
            Debug.LogWarning("Chưa có nhạc nào được thêm vào!");
            return;
        }
        
        // Tạo AudioSource cho mỗi nhạc
        audioSources = new AudioSource[musicClips.Length];
        
        for (int i = 0; i < musicClips.Length; i++)
        {
            // Thêm AudioSource component
            audioSources[i] = gameObject.AddComponent<AudioSource>();
            audioSources[i].clip = musicClips[i];
            audioSources[i].playOnAwake = false;
            
            // Set volume (nếu có)
            if (volumes != null && i < volumes.Length)
            {
                audioSources[i].volume = volumes[i];
            }
            else
            {
                audioSources[i].volume = 1f;
            }
            
            // Set loop (nếu có)
            if (loops != null && i < loops.Length)
            {
                audioSources[i].loop = loops[i];
            }
            else
            {
                audioSources[i].loop = true;
            }
        }
        
        // Kiểm tra setting từ PlayerPrefs
        bool isMusicOn = PlayerPrefs.GetInt("IsMusicOn", 1) == 1;
        
        if (isMusicOn)
        {
            PlayAll();
        }
    }
    
    public void PlayAll()
    {
        if (audioSources == null) return;
        
        foreach (AudioSource source in audioSources)
        {
            if (source != null && !source.isPlaying)
            {
                source.Play();
            }
        }
    }
    
    public void StopAll()
    {
        if (audioSources == null) return;
        
        foreach (AudioSource source in audioSources)
        {
            if (source != null)
            {
                source.Stop();
            }
        }
    }
    
    public void PauseAll()
    {
        if (audioSources == null) return;
        
        foreach (AudioSource source in audioSources)
        {
            if (source != null)
            {
                source.Pause();
            }
        }
    }
    
    public void SetMasterVolume(float volume)
    {
        if (audioSources == null) return;
        
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                float originalVolume = (volumes != null && i < volumes.Length) ? volumes[i] : 1f;
                audioSources[i].volume = originalVolume * volume;
            }
        }
    }
}