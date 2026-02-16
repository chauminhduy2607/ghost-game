using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImageEndScreenChanger : MonoBehaviour
{
    [Header("=== IMAGES ===")]
    public Sprite[] images;
    
    [Header("=== TIMING ===")]
    public float changeInterval = 1f;
    public float delayBeforeNotContinue = 1f;
    
    [Header("=== AUDIO ===")]
    public AudioClip clockSound;
    
    [Header("=== REFERENCES ===")]
    public GameObject notContinue;
    public GameObject bgRevive;
    public GameObject bgEndNotContinue;
    public GameObject timeObject;
    public GameObject endGameText;
    public EndScreenLoopSound loopSoundController;
    
    private Image uiImage;
    private int currentIndex = 0;
    private GameObject clockSoundObject;
    
    void Start()
    {
        uiImage = GetComponent<Image>();
        
        if (uiImage == null || images.Length == 0)
        {
            enabled = false;
            return;
        }
        
        uiImage.sprite = images[0];
        
        if (notContinue != null)
        {
            notContinue.SetActive(false);
        }
        
        if (bgEndNotContinue != null)
        {
            bgEndNotContinue.SetActive(false);
        }
        
        StartCoroutine(ChangeImageRoutine());
    }
    
    IEnumerator ChangeImageRoutine()
    {
        // PHÁT ÂM THANH CLOCK NGAY TỪ ĐẦU
        PlayClockSound();
        
        while (currentIndex < images.Length - 1)
        {
            yield return new WaitForSeconds(changeInterval);
            
            currentIndex++;
            uiImage.sprite = images[currentIndex];
        }
        
        // DỪNG ÂM THANH CLOCK SAU KHI ĐẾM XONG
        StopClockSound();
        
        // BẬT LOOP SOUND - VÀ ĐỂ NÓ PHÁT SUỐT
        if (loopSoundController != null)
        {
            loopSoundController.PlayLoop();
        }
        
        yield return new WaitForSeconds(delayBeforeNotContinue);
        
        if (bgRevive != null)
        {
            bgRevive.SetActive(false);
        }
        
        if (bgEndNotContinue != null)
        {
            bgEndNotContinue.SetActive(true);
        }
        
        if (timeObject != null)
        {
            timeObject.SetActive(false);
        }
        
        if (endGameText != null)
        {
            endGameText.SetActive(false);
        }
        
        if (notContinue != null)
        {
            notContinue.SetActive(true);
        }
        
        // KHÔNG TẮT LOOP SOUND Ở ĐÂY NỮA - ĐỂ NÓ PHÁT TIẾP
        // Loop sound sẽ tắt khi người chơi thoát màn hình hoặc restart game
    }
    
    void PlayClockSound()
    {
        if (clockSound != null)
        {
            clockSoundObject = new GameObject("ClockSound");
            AudioSource audio = clockSoundObject.AddComponent<AudioSource>();
            audio.clip = clockSound;
            audio.loop = false;
            audio.Play();
        }
    }
    
    void StopClockSound()
    {
        if (clockSoundObject != null)
        {
            Destroy(clockSoundObject);
        }
    }
    
    // HÀM NÀY CÓ THỂ GỌI TỪ NÚT "PLAY AGAIN" HOẶC KHI THOÁT SCENE
    public void StopLoopSound()
    {
        if (loopSoundController != null)
        {
            loopSoundController.StopLoop();
        }
    }
    
    // TẮT LOOP SOUND KHI OBJECT BỊ DESTROY
    void OnDestroy()
    {
        StopLoopSound();
    }
}