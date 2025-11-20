using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeIn : MonoBehaviour
{
    [Header("Fade Overlay (必須)")]
    [Tooltip("画面全体を覆う黒Imageに CanvasGroup を付けて割り当て")]
    public CanvasGroup fadeOverlay;

    [Header("設定")]
    public float fadeInSeconds = 0.6f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public bool useUnscaledTime = true;
    [Tooltip("フェード中は入力を止めるならON")]
    public bool blockRaycastsDuringFade = true;

    void Awake()
    {
        if (!fadeOverlay)
        {
            // 名前でゆるく自動取得（任意）
            var cg = GameObject.Find("FadeIn");
            if (cg) fadeOverlay = cg.GetComponent<CanvasGroup>();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!fadeOverlay)
        {
            Debug.LogWarning("[SceneFadeInOnStart] fadeOverlay が未割り当てです。");
            return;
        }

        // 開始時は真っ黒＆必要なら入力遮断
        fadeOverlay.alpha = 1f;
        fadeOverlay.blocksRaycasts = blockRaycastsDuringFade;
        fadeOverlay.interactable = false;

        StartCoroutine(FadeInCo());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator FadeInCo()
    {
        float t = 0f;
        float dur = Mathf.Max(0.0001f, fadeInSeconds);

        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float a = fadeCurve != null ? fadeCurve.Evaluate(u) : (1f - u); // 1→0
            fadeOverlay.alpha = a;
            yield return null;
        }

        // 完了：透明＆入力許可
        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;
    }
}
