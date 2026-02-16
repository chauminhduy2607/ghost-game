using UnityEngine;

public class DynamicBackgroundMusicPlayer : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Nhạc nền gió (luôn chạy)")]
    public AudioClip windClip;
    
    [Tooltip("Nhạc lửa (khi có fire obstacle)")]
    public AudioClip fireClip;
    
    [Tooltip("Nhạc sấm sét (khi có lightning obstacle)")]
    public AudioClip lightningClip;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float windVolume = 0.8f;
    
    [Range(0f, 1f)]
    public float fireVolume = 0.6f;
    
    [Range(0f, 1f)]
    public float lightningVolume = 0.6f;
    
    [Header("Fade Settings")]
    [Tooltip("Thời gian fade in/out âm thanh (giây)")]
    public float fadeSpeed = 0.5f;
    
    [Header("Detection Settings")]
    [Tooltip("Khoảng cách phát hiện thêm phía trên/dưới camera")]
    public float detectionBuffer = 5f;
    
    [Header("References")]
    [Tooltip("Reference đến Camera (tự động tìm nếu để trống)")]
    public Camera mainCamera;
    
    private AudioSource windSource;
    private AudioSource fireSource;
    private AudioSource lightningSource;
    
    private bool isMusicOn = true;
    
    // Target volumes cho fade
    private float fireTargetVolume = 0f;
    private float lightningTargetVolume = 0f;
    
    void Start()
    {
        // Tự động tìm camera nếu chưa assign
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null)
        {
            Debug.LogWarning("DynamicBackgroundMusicPlayer: Không tìm thấy Camera!");
        }
        
        // Tạo 3 AudioSource
        windSource = CreateAudioSource("Wind", windClip, windVolume, true);
        fireSource = CreateAudioSource("Fire", fireClip, 0f, true);
        lightningSource = CreateAudioSource("Lightning", lightningClip, 0f, true);
        
        // Kiểm tra setting
        isMusicOn = PlayerPrefs.GetInt("IsMusicOn", 1) == 1;
        
        if (isMusicOn)
        {
            // Phát tất cả (volume = 0 cho fire và lightning)
            windSource.Play();
            fireSource.Play();
            lightningSource.Play();
        }
    }
    
    AudioSource CreateAudioSource(string name, AudioClip clip, float volume, bool loop)
    {
        GameObject obj = new GameObject($"AudioSource_{name}");
        obj.transform.SetParent(transform);
        
        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D sound
        
        return source;
    }
    
    void Update()
    {
        if (!isMusicOn || mainCamera == null) return;
        
        // Tính vùng nhìn thấy của camera + buffer
        float cameraY = mainCamera.transform.position.y;
        float screenHeight = mainCamera.orthographicSize * 2f;
        
        float minY = cameraY - (screenHeight * 0.5f) - detectionBuffer;
        float maxY = cameraY + (screenHeight * 0.5f) + detectionBuffer;
        
        // Kiểm tra obstacles trong tầm nhìn
        bool hasFireOnScreen = HasObstacleInView("Fire", minY, maxY);
        bool hasLightningOnScreen = HasObstacleInView("Lightning", minY, maxY);
        
        // Set target volumes
        fireTargetVolume = hasFireOnScreen ? fireVolume : 0f;
        lightningTargetVolume = hasLightningOnScreen ? lightningVolume : 0f;
        
        // Fade volumes
        FadeAudioSource(fireSource, fireTargetVolume);
        FadeAudioSource(lightningSource, lightningTargetVolume);
    }
    
    bool HasObstacleInView(string obstacleType, float minY, float maxY)
    {
        // Tìm tất cả obstacles với tag chứa obstacleType
        string[] possibleTags = GetPossibleTags(obstacleType);
        
        foreach (string tag in possibleTags)
        {
            GameObject[] obstacles = FindGameObjectsWithTag(tag);
            
            foreach (GameObject obstacle in obstacles)
            {
                if (obstacle == null || !obstacle.activeInHierarchy) continue;
                
                // Kiểm tra vị trí Y có nằm trong tầm nhìn không
                float obstacleY = obstacle.transform.position.y;
                
                if (obstacleY >= minY && obstacleY <= maxY)
                {
                    return true;
                }
                
                // Kiểm tra cả children (cho các obstacle có spawned objects)
                if (CheckChildrenInView(obstacle.transform, minY, maxY))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    bool CheckChildrenInView(Transform parent, float minY, float maxY)
    {
        foreach (Transform child in parent)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            
            float childY = child.position.y;
            
            if (childY >= minY && childY <= maxY)
            {
                return true;
            }
            
            // Đệ quy kiểm tra children của children
            if (child.childCount > 0)
            {
                if (CheckChildrenInView(child, minY, maxY))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    string[] GetPossibleTags(string obstacleType)
    {
        // Trả về tất cả các tag có thể có cho obstacle type
        if (obstacleType == "Fire")
        {
            return new string[]
            {
                "FireObstacle",
                "FireLineObstacle",
                "FireNLineObstacle",
                "FlyingFireObstacle"
            };
        }
        else if (obstacleType == "Lightning")
        {
            return new string[]
            {
                "LightningObstacle",
                "LightningLineObstacle",
                "LightningNLineObstacle"
            };
        }
        
        return new string[] { };
    }
    
    GameObject[] FindGameObjectsWithTag(string tag)
    {
        try
        {
            return GameObject.FindGameObjectsWithTag(tag);
        }
        catch
        {
            // Tag không tồn tại
            return new GameObject[0];
        }
    }
    
    void FadeAudioSource(AudioSource source, float targetVolume)
    {
        if (source == null) return;
        
        float currentVolume = source.volume;
        
        if (Mathf.Abs(currentVolume - targetVolume) < 0.01f)
        {
            source.volume = targetVolume;
            return;
        }
        
        // Smooth fade
        source.volume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime / fadeSpeed);
    }
    
    public void SetMusicEnabled(bool enabled)
    {
        isMusicOn = enabled;
        
        if (enabled)
        {
            if (windSource != null && !windSource.isPlaying)
                windSource.Play();
            if (fireSource != null && !fireSource.isPlaying)
                fireSource.Play();
            if (lightningSource != null && !lightningSource.isPlaying)
                lightningSource.Play();
        }
        else
        {
            if (windSource != null)
                windSource.Stop();
            if (fireSource != null)
                fireSource.Stop();
            if (lightningSource != null)
                lightningSource.Stop();
        }
    }
    
    public void StopAll()
    {
        if (windSource != null) windSource.Stop();
        if (fireSource != null) fireSource.Stop();
        if (lightningSource != null) lightningSource.Stop();
    }
    
    public void SetMasterVolume(float volume)
    {
        if (windSource != null)
            windSource.volume = windVolume * volume;
    }
    
    void OnDestroy()
    {
        StopAll();
    }
}