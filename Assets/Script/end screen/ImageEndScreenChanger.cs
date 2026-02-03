using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đổi hình ảnh end screen theo trình tự
/// </summary>
public class ImageEndScreenChanger : MonoBehaviour
{
    [Header("=== IMAGES ===")]
    public Sprite[] images;
    
    [Header("=== TIMING ===")]
    public float changeInterval = 1f;
    public float delayBeforeNotContinue = 1f;
    
    [Header("=== REFERENCES ===")]
    public GameObject notContinue;
    public GameObject bgRevive;
    public GameObject bgEndNotContinue;
    public GameObject timeObject;
    public GameObject endGameText;
    
    private Image uiImage;
    private int currentIndex = 0;
    
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
        while (currentIndex < images.Length - 1)
        {
            yield return new WaitForSeconds(changeInterval);
            
            currentIndex++;
            uiImage.sprite = images[currentIndex];
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
    }
}