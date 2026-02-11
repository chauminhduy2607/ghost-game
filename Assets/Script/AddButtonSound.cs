#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class AddButtonSounds : EditorWindow
{
    [MenuItem("Tools/Add Button Sounds")]
    static void AddSoundsToAllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>();
        int count = 0;
        
        foreach (Button button in buttons)
        {
            if (button.GetComponent<ButtonSound>() == null)
            {
                button.gameObject.AddComponent<ButtonSound>();
                count++;
            }
        }
        
        Debug.Log($"✅ Added ButtonSound to {count} buttons!");
    }
}
#endif