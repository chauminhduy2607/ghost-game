using UnityEngine;
using UnityEngine.UI;

public class SoundToggle : MonoBehaviour
{
    [Header("=== CHECKBOX REFERENCES ===")]
    [SerializeField] private Button onSoundButton;
    [SerializeField] private Button offSoundButton;
    
    [Header("=== TICK MARK IMAGES ===")]
    [SerializeField] private GameObject onSoundTick;   // Dấu tick của nút ON
    [SerializeField] private GameObject offSoundTick;  // Dấu tick của nút OFF
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private bool isSoundOn = true;  // Mặc định sound bật

    private void Start()
    {
        // Gắn sự kiện click
        onSoundButton.onClick.AddListener(OnClickSoundOn);
        offSoundButton.onClick.AddListener(OnClickSoundOff);
        
        // Set trạng thái ban đầu
        UpdateUI();
    }

    private void OnClickSoundOn()
    {
        Debug.Log("🔊 Sound ON selected");
        isSoundOn = true;
        UpdateUI();
        
        // TODO: Bật sound thật ở đây
        // AudioListener.volume = 1f;
    }

    private void OnClickSoundOff()
    {
        Debug.Log("🔇 Sound OFF selected");
        isSoundOn = false;
        UpdateUI();
        
        // TODO: Tắt sound thật ở đây
        // AudioListener.volume = 0f;
    }

    private void UpdateUI()
    {
        // Hiện/ẩn dấu tick dựa theo trạng thái
        onSoundTick.SetActive(isSoundOn);
        offSoundTick.SetActive(!isSoundOn);
    }
}