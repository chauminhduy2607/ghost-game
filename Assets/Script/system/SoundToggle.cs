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
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private bool isSoundOn = true;

    private void Start()
    {
        onSoundButton.onClick.AddListener(OnClickSoundOn);
        offSoundButton.onClick.AddListener(OnClickSoundOff);
        
        UpdateUI();
    }

    private void OnClickSoundOn()
    {
        isSoundOn = true;
        UpdateUI();
    }

    private void OnClickSoundOff()
    {
        isSoundOn = false;
        UpdateUI();
    }

    private void UpdateUI()
    {
        onSoundTick.SetActive(isSoundOn);
        offSoundTick.SetActive(!isSoundOn);
    }
}