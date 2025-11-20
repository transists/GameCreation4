using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlashController : MonoBehaviour
{
    public Image flashImage;               // 红色Image
    public float flashSpeed = 2f;          // 闪烁速度
    public float maxAlpha = 0.5f;          // 最亮的透明度

    private Coroutine flashRoutine;

    public void StartFlashLoop()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashLoop());
    }

    public void StopFlashLoop()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        StartCoroutine(FadeOut());
    }

    private IEnumerator FlashLoop()
    {
        while (true)
        {
            // Alpha 从 0 → maxAlpha
            yield return FadeTo(maxAlpha);

            // Alpha 从 maxAlpha → 0
            yield return FadeTo(0f);
        }
    }

    private IEnumerator FadeTo(float target)
    {
        Color c = flashImage.color;

        while (!Mathf.Approximately(c.a, target))
        {
            c.a = Mathf.MoveTowards(c.a, target, Time.deltaTime * flashSpeed);
            flashImage.color = c;
            yield return null;
        }
    }

    private IEnumerator FadeOut()
    {
        Color c = flashImage.color;

        while (c.a > 0f)
        {
            c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * flashSpeed);
            flashImage.color = c;
            yield return null;
        }
    }
}
