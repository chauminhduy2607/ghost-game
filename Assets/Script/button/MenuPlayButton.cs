using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuPlayButton : MonoBehaviour
{
    [Header("=== SCENE SETTING ===")]
    [SerializeField] private string gameSceneName = "Scenes/GameplayScreen";
    
    public void OnPlay()
    {
        StartCoroutine(LoadSceneWithDelay());
    }
    
    IEnumerator LoadSceneWithDelay()
    {
        // Đợi 0.2 giây cho âm thanh phát
        yield return new WaitForSeconds(0.2f);
        
        // Sau đó mới chuyển scene
        SceneManager.LoadScene(gameSceneName);
    }
}