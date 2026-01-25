using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPlayButton : MonoBehaviour
{
    [SerializeField] string gameSceneName = "SampleScene";

    public void OnPlay()
    {
        PlayerPrefs.SetInt("SkipCountdown", 1);   // ✅ chỉ Play mới skip
        PlayerPrefs.SetInt("IsContinue", 0);      // đảm bảo không phải continue
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameSceneName);
    }
}
