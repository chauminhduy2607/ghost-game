using UnityEngine;
using UnityEngine.UI;

public class RoundedCorners : MonoBehaviour
{
    public Image targetImage;
    public float cornerRadius = 30f;
    
    void Start()
    {
        if (targetImage != null)
        {
            // Tạo sprite bo góc
            Sprite roundedSprite = CreateRoundedSprite(512, 512, cornerRadius);
            targetImage.sprite = roundedSprite;
        }
    }
    
    Sprite CreateRoundedSprite(int width, int height, float radius)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isInside = true;
                
                // Kiểm tra 4 góc
                float dx = 0, dy = 0;
                
                // Góc trên trái
                if (x < radius && y > height - radius)
                {
                    dx = radius - x;
                    dy = (height - radius) - y;
                    isInside = (dx * dx + dy * dy) < (radius * radius);
                }
                // Góc trên phải
                else if (x > width - radius && y > height - radius)
                {
                    dx = x - (width - radius);
                    dy = (height - radius) - y;
                    isInside = (dx * dx + dy * dy) < (radius * radius);
                }
                // Góc dưới trái
                else if (x < radius && y < radius)
                {
                    dx = radius - x;
                    dy = radius - y;
                    isInside = (dx * dx + dy * dy) < (radius * radius);
                }
                // Góc dưới phải
                else if (x > width - radius && y < radius)
                {
                    dx = x - (width - radius);
                    dy = radius - y;
                    isInside = (dx * dx + dy * dy) < (radius * radius);
                }
                
                pixels[y * width + x] = isInside ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}