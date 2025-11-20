using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Titlescreen : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;   // VideoPlayer（RenderTexture出力）
    public RawImage bgVideo;          // 背景に貼る RawImage（任意）

    [Header("UI")]
    public CanvasGroup pressGroup;    // Press Space の CanvasGroup
    public Graphic titleLogo;         // ロゴ（任意）

    [Header("Blink")]
    public float blinkMinAlpha = 0.2f;
    public float blinkMaxAlpha = 1.0f;
    public float blinkSpeed = 1.2f;   // 1.2 くらいでゆるく点滅
    public bool useUnscaledTime = true;

    

    private bool _locked = false;
    private float _t;


    // Start is called before the first frame update
    void Start()
    {
        // 安全セット
        if (videoPlayer)
        {
            videoPlayer.isLooping = true;
            if (!videoPlayer.isPlaying) videoPlayer.Play();
        }
        if (pressGroup) pressGroup.alpha = blinkMaxAlpha;
        _t = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        // 点滅
        if (pressGroup && !_locked)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _t += dt * blinkSpeed;
            float a = (Mathf.Sin(_t) * 0.5f + 0.5f); // 0..1
            pressGroup.alpha = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, a);
        }

    }
}
