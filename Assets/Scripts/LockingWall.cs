using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockingWall : MonoBehaviour
{
    [Tooltip("この壁の識別子。鍵と一致したら解除される")]
    public string wallId = "A";

    [Header("オプション")]
    public AudioClip unlockSE;
    public ParticleSystem unlockFX;
    public float fadeOutSeconds = 0.2f;

    bool unlocked = false;
    SpriteRenderer[] renderers;
    Collider2D[] colliders;
    AudioSource audioSrc;

    // Start is called before the first frame update
    void Start()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
        audioSrc = GetComponent<AudioSource>();
        if (!audioSrc) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Unlock()
    {
        if (unlocked) return;
        unlocked = true;

        // 物理を先に止める
        foreach (var c in colliders) c.enabled = false;

        // 演出
        if (unlockFX) Instantiate(unlockFX, transform.position, Quaternion.identity);
        if (unlockSE && audioSrc) audioSrc.PlayOneShot(unlockSE);

        // フェードして破棄（即消しで良ければ Destroy(gameObject) だけでOK）
        if (fadeOutSeconds <= 0f) { Destroy(gameObject); return; }
        StartCoroutine(FadeAndDie());
    }

    System.Collections.IEnumerator FadeAndDie()
    {
        float t = 0f;
        // もとの色を記録
        Color[] from = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i]) from[i] = renderers[i].color;

        while (t < fadeOutSeconds)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeOutSeconds);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i]) renderers[i].color = new Color(from[i].r, from[i].g, from[i].b, a);
            yield return null;
        }
        Destroy(gameObject);
    }
}
