using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Goal : MonoBehaviour
{
    public string nextSceneName = "";

    [Header("ゴールSE")]
    public AudioClip goalSE;
    [Range(0f, 1f)] public float goalSEVolume = 1.0f;
    [Tooltip("SE長ではなく固定秒待ちしたい場合 >0（0=clip長）")]
    public float waitSecondsOverride = 0f;

    [Header("BGM停止（先に止める／フェードアウト）")]
    [Tooltip("Inspectorで明示的に指定（空なら自動検出）")]
    public AudioSource[] bgmSources;
    [Tooltip("メインカメラのAudioSourceを自動検出してBGMとして扱う")]
    public bool includeCameraAudio = true;
    [Tooltip("タグ 'BGM' が付いたAudioSourceを自動検出")]
    public bool includeTagBGM = true;
    [Tooltip("BGMのフェードアウト秒数（0で即停止）")]
    public float bgmFadeSeconds = 0.5f;

    [Header("UI/演出（任意）")]
    //public Animator fadeAnimator;
    //public string fadeTriggerName = "FadeOut";

    private bool entered = false;
    private Collider2D col;

    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<Collider2D>();
        // ゴール判定はトリガー推奨
        if (col && !col.isTrigger) col.isTrigger = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (entered) return;

        // プレイヤー判定（タグ or コンポーネント）
        var pc = other.GetComponent<PlayerController>();
        if (!pc && !other.CompareTag("Player")) return;

        // 検知ロック中はゴール不可
        if (pc && !pc.CanGoal) return;

        StartCoroutine(GoalSequence(pc));
    }

    private System.Collections.IEnumerator GoalSequence(PlayerController pc)
    {
        entered = true;
        if (col) col.enabled = false;

        //if (fadeAnimator && !string.IsNullOrEmpty(fadeTriggerName))
        //    fadeAnimator.SetTrigger(fadeTriggerName);

        // --- BGMを先に止める（フェード可） ---
        var targets = GatherBGMSources();
        if (bgmFadeSeconds > 0f)
            yield return StartCoroutine(FadeOutBGMs(targets, bgmFadeSeconds));
        else
            StopBGMsImmediately(targets);

        // --- ゴールSEを再生して待つ ---
        float wait = 0.1f;
        if (goalSE)
        {
            AudioSource.PlayClipAtPoint(goalSE, Camera.main ? Camera.main.transform.position : transform.position, goalSEVolume);
            wait = (waitSecondsOverride > 0f) ? waitSecondsOverride : goalSE.length;
        }
        else if (waitSecondsOverride > 0f)
        {
            wait = waitSecondsOverride;
        }

        yield return new WaitForSeconds(wait);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogWarning("[GoalGate] nextSceneName が未設定です。");
    }

    // ===== BGM検出/停止 =====

    private AudioSource[] GatherBGMSources()
    {
        // すでにInspectorで指定されていたらそれを使用
        var list = new System.Collections.Generic.List<AudioSource>();
        if (bgmSources != null)
        {
            foreach (var s in bgmSources) if (s) list.Add(s);
        }

        // 自動：メインカメラのAudioSource
        if (includeCameraAudio && Camera.main)
        {
            var camSrc = Camera.main.GetComponent<AudioSource>();
            if (camSrc && !list.Contains(camSrc)) list.Add(camSrc);
        }

        // 自動：タグ "BGM" のAudioSource
        if (includeTagBGM)
        {
            var tagged = GameObject.FindGameObjectsWithTag("BGM");
            foreach (var go in tagged)
            {
                var src = go.GetComponent<AudioSource>();
                if (src && !list.Contains(src)) list.Add(src);
            }
        }

        return list.ToArray();
    }

    private System.Collections.IEnumerator FadeOutBGMs(AudioSource[] sources, float seconds)
    {
        if (sources == null || sources.Length == 0 || seconds <= 0f) yield break;

        // 各ソースの元音量
        var vols = new float[sources.Length];
        for (int i = 0; i < sources.Length; i++)
            vols[i] = sources[i] ? sources[i].volume : 0f;

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime; // フェードはタイムスケール無視が無難
            float k = 1f - Mathf.Clamp01(t / seconds);
            for (int i = 0; i < sources.Length; i++)
                if (sources[i]) sources[i].volume = vols[i] * k;
            yield return null;
        }

        // 停止＆音量復元（次シーンで再利用される場合に備える）
        for (int i = 0; i < sources.Length; i++)
        {
            if (!sources[i]) continue;
            sources[i].Stop();
            sources[i].volume = vols[i];
        }
    }

    private void StopBGMsImmediately(AudioSource[] sources)
    {
        if (sources == null) return;
        foreach (var s in sources)
        {
            if (!s) continue;
            s.Stop();
        }
    }
}
