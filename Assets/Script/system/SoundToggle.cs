using UnityEngine;
using UnityEngine.UI;

public class SoundToggle : MonoBehaviour
{
    [Header("=== CHECKBOX REFERENCES ===")]
    [SerializeField] private Button onSoundButton;
    [SerializeField] private Button offSoundButton;
    
    [Header("=== TICK MARK IMAGES ===")]
    [SerializeField] private GameObject onSoundTick;
    [SerializeField] private GameObject offSoundTick;
    
    [Header("=== AUDIO ===")]
    [SerializeField] private AudioClip clickSound;
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private bool isSoundOn = true;

    private void Start()
    {
        // Load setting đã lưu
        isSoundOn = PlayerPrefs.GetInt("IsMusicOn", 1) == 1;
        
        onSoundButton.onClick.AddListener(OnClickSoundOn);
        offSoundButton.onClick.AddListener(OnClickSoundOff);
        
        UpdateUI();
        ApplyMusicSetting();
    }

    private void OnClickSoundOn()
    {
        PlayClickSound();
        isSoundOn = true;
        PlayerPrefs.SetInt("IsMusicOn", 1);
        PlayerPrefs.Save();
        UpdateUI();
        ApplyMusicSetting();
    }

    private void OnClickSoundOff()
    {
        PlayClickSound();
        isSoundOn = false;
        PlayerPrefs.SetInt("IsMusicOn", 0);
        PlayerPrefs.Save();
        UpdateUI();
        ApplyMusicSetting();
    }

    private void UpdateUI()
    {
        onSoundTick.SetActive(isSoundOn);
        offSoundTick.SetActive(!isSoundOn);
    }
    
    private void ApplyMusicSetting()
    {
        // Tìm tất cả AudioSource trong game
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        
        foreach (AudioSource audio in allAudioSources)
        {
            // Chỉ ảnh hưởng nhạc nền (có loop = true)
            // Không ảnh hưởng âm thanh nút (loop = false)
            if (audio.loop)
            {
                if (isSoundOn)
                {
                    audio.volume = 1f;
                    if (!audio.isPlaying)
                        audio.Play();
                }
                else
                {
                    audio.volume = 0f;
                }
            }
        }
    }
    
    private void PlayClickSound()
    {
        if (clickSound != null)
        {
            // Tạo GameObject tạm để phát âm thanh click
            GameObject tempAudio = new GameObject("TempClickAudio");
            AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
            audioSource.clip = clickSound;
            audioSource.loop = false;
            audioSource.Play();
            Destroy(tempAudio, clickSound.length);
        }
    }
}