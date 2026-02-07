using UnityEngine;
using System.Collections;

public class LeaderboardAnimator : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform panelTransform;
    
    public float animationDuration = 0.3f;
    
    void OnEnable()
    {
        StartCoroutine(ShowAnimation());
    }
    
    IEnumerator ShowAnimation()
    {
        // Fade in
        canvasGroup.alpha = 0;
        panelTransform.localScale = Vector3.zero;
        
        float elapsed = 0;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            
            // Fade
            canvasGroup.alpha = progress;
            
            // Scale với hiệu ứng bounce
            float scale = EaseOutBack(progress);
            panelTransform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        canvasGroup.alpha = 1;
        panelTransform.localScale = Vector3.one;
    }
    
    public void Close()
    {
        StartCoroutine(HideAnimation());
    }
    
    IEnumerator HideAnimation()
    {
        float elapsed = 0;
        
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.2f;
            
            canvasGroup.alpha = 1 - progress;
            panelTransform.localScale = Vector3.one * (1 - progress * 0.2f);
            
            yield return null;
        }
        
        gameObject.SetActive(false);
    }
    
    // Hiệu ứng bounce đơn giản
    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }
}