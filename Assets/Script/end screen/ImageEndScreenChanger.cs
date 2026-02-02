using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImageEndScreenChanger : MonoBehaviour
{
    // Mảng chứa các hình ảnh
    public Sprite[] images;
    
    // Thời gian giữa mỗi lần đổi hình (giây)
    public float changeInterval = 1f;
    
    // Thời gian đợi sau hình cuối cùng trước khi hiện NotContinue (giây)
    public float delayBeforeNotContinue = 1f;
    
    // GameObject NotContinue để bật
    public GameObject notContinue;
    
    // GameObject BG-revive để tắt
    public GameObject bgRevive;
    
    // GameObject BG-end-not-continue để bật
    public GameObject bgEndNotContinue;
    
    // GameObject time (chứa 2 số 00) để tắt
    public GameObject timeObject;
    
    // GameObject endgametext để tắt
    public GameObject endGameText;
    
    // Component UI Image
    private Image uiImage;
    
    // Chỉ số hình hiện tại
    private int currentIndex = 0;
    
    void Start()
    {
        // Lấy component UI Image
        uiImage = GetComponent<Image>();
        
        if (uiImage == null)
        {
            Debug.LogError("Không tìm thấy UI Image component!");
            return;
        }
        
        // Hiển thị hình đầu tiên
        if (images.Length > 0)
        {
            uiImage.sprite = images[0];
            Debug.Log("Bắt đầu với hình: " + images[0].name);
        }
        else
        {
            Debug.LogError("Chưa có hình ảnh nào trong mảng!");
            return;
        }
        
        // Đảm bảo NotContinue và BG-end-not-continue bị tắt lúc đầu
        if (notContinue != null)
        {
            notContinue.SetActive(false);
        }
        
        if (bgEndNotContinue != null)
        {
            bgEndNotContinue.SetActive(false);
        }
        
        // Bắt đầu đổi hình
        StartCoroutine(ChangeImageRoutine());
    }
    
    IEnumerator ChangeImageRoutine()
    {
        while (currentIndex < images.Length - 1)
        {
            // Đợi 1 giây
            yield return new WaitForSeconds(changeInterval);
            
            // Chuyển sang hình tiếp theo
            currentIndex++;
            uiImage.sprite = images[currentIndex];
            
            Debug.Log("Đổi sang hình " + currentIndex + ": " + images[currentIndex].name);
        }
        
        // Đã đến hình cuối cùng (00)
        Debug.Log("Đã hiển thị hình cuối cùng! Đợi " + delayBeforeNotContinue + " giây...");
        
        // Đợi thêm 1 giây (hoặc thời gian bạn muốn)
        yield return new WaitForSeconds(delayBeforeNotContinue);
        
        // Sau khi đợi xong, tắt và bật
        Debug.Log("Bắt đầu chuyển sang NotContinue");
        
        // Tắt BG-revive
        if (bgRevive != null)
        {
            bgRevive.SetActive(false);
            Debug.Log("Đã tắt BG-revive");
        }
        
        // Bật BG-end-not-continue
        if (bgEndNotContinue != null)
        {
            bgEndNotContinue.SetActive(true);
            Debug.Log("Đã bật BG-end-not-continue");
        }
        
        // Tắt time (chứa 2 số 00)
        if (timeObject != null)
        {
            timeObject.SetActive(false);
            Debug.Log("Đã tắt time");
        }
        
        // Tắt endgametext
        if (endGameText != null)
        {
            endGameText.SetActive(false);
            Debug.Log("Đã tắt endGameText");
        }
        
        // Bật NotContinue
        if (notContinue != null)
        {
            notContinue.SetActive(true);
            Debug.Log("Đã bật NotContinue");
        }
    }
}