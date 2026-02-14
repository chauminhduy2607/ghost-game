using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSettingButton : MonoBehaviour
{
    [Header("=== SCENE SETTING ===")]
    [SerializeField] private string settingSceneName = "Scenes/SettingScreen";

    public void OnSetting()
    {
        SceneManager.LoadScene(settingSceneName);
    }
}