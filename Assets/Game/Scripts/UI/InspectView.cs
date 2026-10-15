using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Holding a scrap (or the whole board) up to your face.
public class InspectView : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] RectTransform paper;          // the thing that sways
    [SerializeField] RawImage single;              // one scrap, cropped
    [SerializeField] RawImage[] layers;            // whole page, one layer per scrap
    [SerializeField] RectTransform pageRoot;
    [SerializeField] Text thought;
    [SerializeField] Text hint;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip rustle;
    [SerializeField] float maxHeight = 820f;
    [SerializeField] float pageAspect = 0.72f;

    public bool IsOpen { get; private set; }

    Action onClosed;
    float openedAt;
    float target;
    float seed;

    void Awake()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
        seed = UnityEngine.Random.value * 10f;
    }

    public void ShowScrap(Texture2D tex, Rect uv, string text, Action closed)
    {
        single.gameObject.SetActive(true);
        pageRoot.gameObject.SetActive(false);
        single.texture = tex;
        single.uvRect = uv;

        float aspect = (uv.width * tex.width) / Mathf.Max(1f, uv.height * tex.height);
        float h = maxHeight * 0.85f;
        float w = h * aspect;
        if (w > 900f) { w = 900f; h = w / aspect; }
        single.rectTransform.sizeDelta = new Vector2(w, h);

        Open(text, closed);
    }

    public void ShowPage(Texture2D[] placed, string text, Action closed)
    {
        single.gameObject.SetActive(false);
        pageRoot.gameObject.SetActive(true);
        pageRoot.sizeDelta = new Vector2(maxHeight * pageAspect, maxHeight);
        for (int i = 0; i < layers.Length; i++)
        {
            bool has = placed != null && i < placed.Length && placed[i] != null;
            layers[i].gameObject.SetActive(has);
            if (has) layers[i].texture = placed[i];
        }
        Open(text, closed);
    }

    void Open(string text, Action closed)
    {
        onClosed = closed;
        thought.text = text ?? "";
        IsOpen = true;
        openedAt = Time.unscaledTime;
        target = 1f;
        paper.localRotation = Quaternion.Euler(0f, 0f, -7f);
        paper.anchoredPosition = new Vector2(0f, -140f);
        if (source != null && rustle != null) source.PlayOneShot(rustle, 0.8f);
        if (LoopDirector.Instance != null) LoopDirector.Instance.SetFocusMode(true);
    }

    void Close()
    {
        IsOpen = false;
        target = 0f;
        if (source != null && rustle != null) source.PlayOneShot(rustle, 0.5f);
        if (LoopDirector.Instance != null) LoopDirector.Instance.SetFocusMode(false);
        var cb = onClosed;
        onClosed = null;
        cb?.Invoke();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        group.alpha = Mathf.MoveTowards(group.alpha, target, dt * (target > 0f ? 2.5f : 3.5f));

        if (group.alpha <= 0f) return;

        // hands aren't steady
        float t = Time.unscaledTime;
        float rz = (Mathf.PerlinNoise(seed, t * 0.3f) - 0.5f) * 3f;
        Vector2 sway = new Vector2(Mathf.PerlinNoise(seed + 2f, t * 0.25f) - 0.5f, Mathf.PerlinNoise(seed + 4f, t * 0.25f) - 0.5f) * 14f;
        paper.localRotation = Quaternion.Slerp(paper.localRotation, Quaternion.Euler(0f, 0f, rz), dt * 4f);
        paper.anchoredPosition = Vector2.Lerp(paper.anchoredPosition, sway + new Vector2(0f, 30f), dt * 4f);

        if (hint != null)
        {
            var c = hint.color;
            c.a = IsOpen && Time.unscaledTime - openedAt > 1.2f ? Mathf.MoveTowards(c.a, 0.5f, dt) : 0f;
            hint.color = c;
        }

        if (IsOpen && Time.unscaledTime - openedAt > 0.6f && Dismissed()) Close();
    }

    static bool Dismissed()
    {
        var kb = Keyboard.current;
        var m = Mouse.current;
        return (kb != null && (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            || (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame));
    }
}
