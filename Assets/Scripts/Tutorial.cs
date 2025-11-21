using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 押下後に SE 再生 → 黒フェードアウト → シーン遷移 を行う汎用スクリプト。
/// ・フェード用 CanvasGroup を α=0 から 1 に上げる
/// ・SE は AudioSource.PlayOneShot で再生（任意）
/// ・開始は KeyCode 監視 or 外部から Trigger() 呼び出し
/// </summary>
public class Tutorial : MonoBehaviour
{

    [Header("Start / Input")]
    public bool handleKeyInput = true;      // ここでキー入力を拾うか
    public KeyCode triggerKey = KeyCode.Space;

    [Header("Next Scene")]
    public string nextSceneName = "";       // ここが空だと遷移しません

    [Header("Fade Overlay (必須)")]
    [Tooltip("画面全体の黒パネルに CanvasGroup を付けて割り当て（初期Alphaは0推奨）")]
    public CanvasGroup fadeOverlay;
    public float fadeOutSeconds = 0.6f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool useUnscaledTime = true;
    public bool blockRaycastsDuringFade = true;   // 入力遮断

    [Header("SE (任意)")]
    public AudioSource seSource;             // 2D推奨（BGMと別AudioSource）
    public AudioClip pressSE;
    public float extraDelayAfterSE = 0.1f;   // SE再生開始→フェード開始までの余白

    private bool _running = false;// Start is called before the first frame update
    
    void Start()
    {
        if (fadeOverlay)
        {
            // 開始時は透明にしておく（誤って1になっていると真っ黒のまま）
            fadeOverlay.alpha = Mathf.Min(fadeOverlay.alpha, 0f);
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_running && handleKeyInput && Input.GetKeyDown(triggerKey))
        {
            Trigger();
        }
    }

    /// <summary>
    /// 外部から開始したい場合に呼び出す（ボタンUIや独自入力で）
    /// </summary>
    public void Trigger()
    {
        if (_running) return;
        StartCoroutine(Co_Run());
    }

    private IEnumerator Co_Run()
    {
        _running = true;

        // SE（任意）
        float waitBeforeFade = 0f;
        if (seSource && pressSE)
        {
            seSource.PlayOneShot(pressSE);
            waitBeforeFade = Mathf.Max(pressSE.length, extraDelayAfterSE);
        }
        else
        {
            waitBeforeFade = extraDelayAfterSE;
        }

        if (waitBeforeFade > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(waitBeforeFade);
            else yield return new WaitForSeconds(waitBeforeFade);
        }

        // フェード
        if (fadeOverlay)
        {
            fadeOverlay.blocksRaycasts = blockRaycastsDuringFade;
            fadeOverlay.interactable = false;

            float t = 0f;
            float dur = Mathf.Max(0.0001f, fadeOutSeconds);
            while (t < dur)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float a = fadeCurve != null ? fadeCurve.Evaluate(u) : u; // 0→1
                fadeOverlay.alpha = a;
                yield return null;
            }
            fadeOverlay.alpha = 1f;
        }

        // 遷移
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[PressFadeOutToScene] nextSceneName が未設定のため遷移しません。");
        }
    }
}
