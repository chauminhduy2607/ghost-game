using UnityEngine;

public class FloatingAnimation : MonoBehaviour
{
    [Header("=== BẬT/TẮT ANIMATION ===")]
    [SerializeField] private bool enableAnimation = true;
    
    [Header("=== LẮC NGANG (Sway) ===")]
    [SerializeField] private bool enableSway = true;
    [SerializeField] private float swayAmount = 0.7f;
    [SerializeField] private float swaySpeed = 2.2f;
    
    [Header("=== NHẤP NHÔ (Bob) ===")]
    [SerializeField] private bool enableBob = true;
    [SerializeField] private float bobAmount = 0.5f;
    [SerializeField] private float bobSpeed = 2.8f;
    
    [Header("=== NGHIÊNG (Tilt) ===")]
    [SerializeField] private bool enableTilt = true;
    [SerializeField] private float tiltAmount = 18f;
    [SerializeField] private float tiltSpeed = 1.8f;
    
    [Header("=== PHÓNG TO/THU NHỎ (Scale Pulse) ===")]
    [SerializeField] private bool enableScalePulse = true;
    [SerializeField] private float scaleAmount = 0.15f;
    [SerializeField] private float scaleSpeed = 3.5f;
    
    private Vector3 startPosition;
    private Vector3 startScale;
    private Quaternion startRotation;
    
    private float swayTimer = 0f;
    private float bobTimer = 0f;
    private float tiltTimer = 0f;
    private float scaleTimer = 0f;
    
    void Start()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
        startRotation = transform.localRotation;
        
        swayTimer = Random.Range(0f, 2f * Mathf.PI);
        bobTimer = Random.Range(0f, 2f * Mathf.PI);
        tiltTimer = Random.Range(0f, 2f * Mathf.PI);
        scaleTimer = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        if (!enableAnimation) return;
        
        swayTimer += Time.deltaTime * swaySpeed;
        bobTimer += Time.deltaTime * bobSpeed;
        tiltTimer += Time.deltaTime * tiltSpeed;
        scaleTimer += Time.deltaTime * scaleSpeed;
        
        Vector3 offset = Vector3.zero;
        
        if (enableSway)
        {
            offset.x = Mathf.Sin(swayTimer) * swayAmount;
        }
        
        if (enableBob)
        {
            offset.y = Mathf.Sin(bobTimer) * bobAmount;
        }
        
        transform.localPosition = startPosition + offset;
        
        if (enableTilt)
        {
            float tiltAngle = Mathf.Sin(tiltTimer) * tiltAmount;
            transform.localRotation = startRotation * Quaternion.Euler(0, 0, tiltAngle);
        }
        
        if (enableScalePulse)
        {
            float scaleFactor = 1f + Mathf.Sin(scaleTimer) * scaleAmount;
            transform.localScale = startScale * scaleFactor;
        }
    }
    
    public void SetAnimationEnabled(bool enabled)
    {
        enableAnimation = enabled;
        
        if (!enabled)
        {
            transform.localPosition = startPosition;
            transform.localRotation = startRotation;
            transform.localScale = startScale;
        }
    }
    
    public void ResetStartPosition()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
        startRotation = transform.localRotation;
    }
    
    public void SetIntensity(float intensity)
    {
        swayAmount = 0.2f * intensity;
        bobAmount = 0.15f * intensity;
        tiltAmount = 5f * intensity;
    }
}