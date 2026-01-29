using UnityEngine;

public class ImageSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class ImageData
    {
        public Sprite sprite;
        public Vector3 scale = new Vector3(0.06741104f, 0.06156023f, 1f);
    }
    
    [Header("Image Settings")]
    public ImageData[] imageList;
    
    [Header("Auto Switch Settings")]
    public float switchTime = 2f; // Thời gian tự động chuyển (giây)
    public bool autoSwitch = true; // Bật/tắt tự động chuyển
    
    private SpriteRenderer spriteRenderer;
    private int currentIndex = 0;
    private float timer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (imageList.Length > 0)
        {
            ShowImage(0);
        }
        else
        {
            Debug.LogWarning("Image list is empty! Please add images in Inspector.");
        }
    }

    void Update()
    {
        // Tự động chuyển hình theo thời gian
        if (autoSwitch && imageList.Length > 0)
        {
            timer += Time.deltaTime;
            
            if (timer >= switchTime)
            {
                NextImage();
                timer = 0f; // Reset timer
            }
        }
        
        // Manual controls (optional - có thể xóa nếu không cần)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextImage();
            timer = 0f; // Reset timer khi bấm phím
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousImage();
            timer = 0f;
        }
        
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextImage();
            timer = 0f;
        }
    }

    public void NextImage()
    {
        if (imageList.Length == 0) return;
        
        currentIndex = (currentIndex + 1) % imageList.Length;
        ShowImage(currentIndex);
    }

    public void PreviousImage()
    {
        if (imageList.Length == 0) return;
        
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = imageList.Length - 1;
        }
        ShowImage(currentIndex);
    }

    public void ShowImage(int index)
    {
        if (index >= 0 && index < imageList.Length)
        {
            spriteRenderer.sprite = imageList[index].sprite;
            transform.localScale = imageList[index].scale;
        }
    }

    // Switch to specific image by number (0, 1, 2, ...)
    public void SwitchToImage(int index)
    {
        if (index >= 0 && index < imageList.Length)
        {
            currentIndex = index;
            ShowImage(currentIndex);
            timer = 0f; // Reset timer
        }
    }
    
    // Bật/tắt chế độ tự động
    public void SetAutoSwitch(bool enable)
    {
        autoSwitch = enable;
        timer = 0f;
    }
    
    // Đổi tốc độ chuyển hình
    public void SetSwitchTime(float newTime)
    {
        switchTime = Mathf.Max(0.1f, newTime); // Tối thiểu 0.1 giây
        timer = 0f;
    }
}