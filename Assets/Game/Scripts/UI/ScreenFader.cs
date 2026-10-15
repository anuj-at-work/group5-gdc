using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] Image black;
    [SerializeField] Text line;
    [SerializeField] Text controls;
    [SerializeField] CanvasGroup titleCard;

    void Awake()
    {
        SetA(line, 0f);
        SetA(controls, 0f);
        if (titleCard != null) titleCard.alpha = 0f;
    }

    public float Alpha => black.color.a;

    public void SetBlack() => SetA(black, 1f);

    public IEnumerator FadeTo(float target, float time)
    {
        float from = black.color.a;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            SetA(black, Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / time)));
            yield return null;
        }
        SetA(black, target);
    }

    public IEnumerator ShowLines(string[] lines, bool showControls, bool slow = false)
    {
        SetA(black, 1f);
        if (showControls) StartCoroutine(FadeText(controls, 0.45f, 2f));

        foreach (string l in lines)
        {
            line.text = l;
            yield return FadeText(line, 0.9f, slow ? 1.6f : 1.1f);
            float hold = (slow ? 2.6f : 1.6f) + l.Length * 0.035f;
            float t = 0f;
            while (t < hold)
            {
                t += Time.unscaledDeltaTime;
                if (t > 0.6f && Skip()) break;
                yield return null;
            }
            yield return FadeText(line, 0f, 0.8f);
            yield return new WaitForSecondsRealtime(0.25f);
        }

        if (showControls) yield return FadeText(controls, 0f, 0.8f);
    }

    public IEnumerator ShowTitleCard()
    {
        if (titleCard == null) yield break;
        float t = 0f;
        while (t < 3f) { t += Time.unscaledDeltaTime; titleCard.alpha = t / 3f; yield return null; }
        yield return new WaitForSecondsRealtime(4f);
        t = 0f;
        while (t < 2f) { t += Time.unscaledDeltaTime; titleCard.alpha = 1f - t / 2f; yield return null; }
        titleCard.alpha = 0f;
        yield return new WaitForSecondsRealtime(1f);
    }

    static bool Skip()
    {
        var kb = Keyboard.current;
        var m = Mouse.current;
        return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            || (m != null && m.leftButton.wasPressedThisFrame);
    }

    IEnumerator FadeText(Graphic g, float to, float time)
    {
        float from = g.color.a;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            SetA(g, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        SetA(g, to);
    }

    static void SetA(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color; c.a = a; g.color = c;
    }
}
