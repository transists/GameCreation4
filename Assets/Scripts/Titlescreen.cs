using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Titlescreen : MonoBehaviour
{
    [Header("Next Scene")]
    public string nextSceneName = "";

    [Header("Video (任意)")]
    public VideoPlayer videoPlayer;   // 背景動画（なくてもOK）
    public RawImage bgVideo;          // RenderTexture を表示するRawImage（任意）

    [Header("UI")]
    public CanvasGroup pressGroup;    // 「Press Space」の CanvasGroup
    public Graphic titleLogo;         // ロゴ（任意）

    [Header("Idle Blink (未押下のふわっと)")]
    public float idleMinAlpha = 0.2f;
    public float idleMaxAlpha = 1.0f;
    public float idleBlinkSpeed = 1.2f;      // ゆるい点滅速度
    public bool useUnscaledTime = true;

    [Header("Strobe (押下後のチカチカ)")]
    public float strobeHz = 12f;            // 点滅周波数（回/秒） 10〜16くらいが“チカチカ”
    public float strobeDuration = 0.6f;     // 何秒チカチカさせるか
    public float afterStrobeDelay = 0.1f;   // チカチカ後にワンテンポ置く

    [Header("SE")]
    public AudioSource seSource;            // 効果音用
    public AudioClip pressSE;               // 押下SE（任意）

    [Header("Fade Out (チカチカ後)")]
    [Tooltip("画面全体を覆う黒のCanvasGroup（Alphaを0→1にフェード）")]
    public CanvasGroup fadeOverlay;         // 全画面パネルに CanvasGroup を付けて割り当て
    public float fadeOutSeconds = 0.6f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool fadeBlocksRaycasts = true;  // フェード中は入力遮断するならON
    public float afterFadeDelay = 0.1f;     // フェード完了後にワンテンポ置く

    private enum Mode { IdleBlink, Strobe, Fading, Lock }
    private Mode _mode = Mode.IdleBlink;
    private float _t;                 // Idle用タイマー
    private float _strobeStartTime;   // Strobe開始時刻（unscaled/normal切替）


    // Start is called before the first frame update
    void Start()
    {
        if (videoPlayer)
        {
            videoPlayer.isLooping = true;
            if (!videoPlayer.isPlaying) videoPlayer.Play();
        }
        if (pressGroup) pressGroup.alpha = idleMaxAlpha;

        // フェード初期化
        if (fadeOverlay)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }

        _t = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;

        switch (_mode)
        {
            case Mode.IdleBlink:
                if (pressGroup)
                {
                    _t += dt * idleBlinkSpeed;
                    float a = (Mathf.Sin(_t) * 0.5f + 0.5f); // 0..1
                    pressGroup.alpha = Mathf.Lerp(idleMinAlpha, idleMaxAlpha, a);
                }
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // 任意SE
                    if (seSource && pressSE) seSource.PlayOneShot(pressSE);

                    // Strobe開始
                    _strobeStartTime = now;
                    _mode = Mode.Strobe;
                }
                break;

            case Mode.Strobe:
                if (pressGroup)
                {
                    // 矩形波チカチカ
                    float phase = (now - _strobeStartTime) * strobeHz;
                    bool on = (Mathf.FloorToInt(phase) % 2) == 0;
                    pressGroup.alpha = on ? 1f : 0f;
                }
                if (now - _strobeStartTime >= strobeDuration)
                {
                    // フェードへ
                    if (fadeOverlay)
                    {
                        StartCoroutine(FadeOutThenLoad());
                        _mode = Mode.Fading;
                    }
                    else
                    {
                        // フェードなしで即遷移
                        _mode = Mode.Lock;
                        LoadNext();
                    }
                }
                break;

            case Mode.Fading:
            case Mode.Lock:
                // 入力無効
                break;
        }

    }

    private IEnumerator FadeOutThenLoad()
    {
        // 入力遮断
        if (fadeOverlay)
        {
            fadeOverlay.blocksRaycasts = fadeBlocksRaycasts;
            fadeOverlay.interactable = false;
        }

        float t = 0f;
        float dur = Mathf.Max(0.0001f, fadeOutSeconds);
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float a = fadeCurve != null ? fadeCurve.Evaluate(u) : u;
            if (fadeOverlay) fadeOverlay.alpha = a;
            yield return null;
        }
        if (fadeOverlay) fadeOverlay.alpha = 1f;

        if (afterFadeDelay > 0f)
            yield return useUnscaledTime ? new WaitForSecondsRealtime(afterFadeDelay)
                                         : new WaitForSeconds(afterFadeDelay);

        _mode = Mode.Lock;
        LoadNext();
    }

    private void LoadNext()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
