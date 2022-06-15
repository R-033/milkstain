using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Example : MonoBehaviour
{
    public Milkstain.Milkdrop milkdrop;
    public RawImage TargetGraphic;
    public AudioSource TargetAudio;

    void Start()
    {
        milkdrop.TargetAudio = TargetAudio;
        milkdrop.Resolution = new Vector2Int(Screen.width, Screen.height);
        milkdrop.Initialize();
        TargetGraphic.texture = milkdrop.FinalTexture;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            milkdrop.presetChangeTimer = 0f;
            milkdrop.PlayRandomPreset(2.7f);
        }
    }
}
