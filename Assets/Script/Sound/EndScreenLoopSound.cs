using UnityEngine;

public class EndScreenLoopSound : MonoBehaviour
{
    [Header("=== LOOP SOUND ===")]
    public AudioClip loopSound;
    [Range(0f, 1f)]
    public float volume = 1f;
    
    private AudioSource audioSource;
    
    void Awake()
    {
        // TẠO AUDIO SOURCE
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        
        if (loopSound != null)
        {
            audioSource.clip = loopSound;
        }
    }
    
    public void PlayLoop()
    {
        if (audioSource != null && loopSound != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("Loop Sound Started!");
        }
    }
    
    public void StopLoop()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Loop Sound Stopped!");
        }
    }
}