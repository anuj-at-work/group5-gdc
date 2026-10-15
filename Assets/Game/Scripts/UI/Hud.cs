using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour
{
    [SerializeField] Image dot;
    [SerializeField] Text prompt;
    [SerializeField] Text toast;
    [SerializeField] float idleDotAlpha = 0.18f;
    [SerializeField] float focusDotAlpha = 0.75f;

    string promptText;
    float promptAlpha;
    float dotAlpha;
    float toastUntil;
    float toastAlpha;
    bool hidden;

    void Awake()
    {
        SetAlpha(prompt, 0f);
        SetAlpha(toast, 0f);
    }

    public void SetFocus(string p)
    {
        promptText = string.IsNullOrEmpty(p) ? null : p;
        if (promptText != null) prompt.text = promptText;
    }

    public void Toast(string message, float seconds = 4f)
    {
        toast.text = message;
        toastUntil = Time.time + seconds;
    }

    public void HideAll()
    {
        hidden = true;
        promptText = null;
        toastUntil = 0f;
    }

    public void ShowAgain() => hidden = false;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool focused = promptText != null && !hidden;

        dotAlpha = Mathf.MoveTowards(dotAlpha, hidden ? 0f : (focused ? focusDotAlpha : idleDotAlpha), dt * 3f);
        promptAlpha = Mathf.MoveTowards(promptAlpha, focused ? 0.85f : 0f, dt * (focused ? 4f : 6f));
        toastAlpha = Mathf.MoveTowards(toastAlpha, Time.time < toastUntil && !hidden ? 0.9f : 0f, dt * 1.5f);

        SetAlpha(dot, dotAlpha);
        SetAlpha(prompt, promptAlpha);
        SetAlpha(toast, toastAlpha);

        float s = focused ? 1.6f : 1f;
        dot.rectTransform.localScale = Vector3.Lerp(dot.rectTransform.localScale, Vector3.one * s, dt * 10f);
    }

    static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color; c.a = a; g.color = c;
    }
}
