using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPlayButton : MonoBehaviour
{
    [Header("=== SCENE SETTING ===")]
    [SerializeField] private string gameSceneName = "Scenes/GameplayScreen";
    
    public void OnPlay()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}