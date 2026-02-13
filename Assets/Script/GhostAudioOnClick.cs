using UnityEngine;

public class GhostAudioOnClick : MonoBehaviour
{
    void OnMouseDown()
    {
        Debug.Log("Ghost clicked!");
        
        var audioTrigger = GameObject.Find("AudioTrigger");
        if (audioTrigger != null)
        {
            audioTrigger.GetComponent<AudioTrigger>()?.PlayGhostTapSound();
        }
    }
}