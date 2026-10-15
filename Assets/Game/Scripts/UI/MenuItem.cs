using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Plain text menu entry. Hover pushes it right a touch and brightens it.
public class MenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] Text label;
    [SerializeField] Color idle = new Color(0.72f, 0.74f, 0.72f, 0.55f);
    [SerializeField] Color hover = new Color(0.93f, 0.9f, 0.82f, 1f);
    [SerializeField] float hoverShift = 14f;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip hoverClip;
    [SerializeField] AudioClip clickClip;

    public UnityEvent onClick = new UnityEvent();

    bool over;
    float baseX;
    RectTransform rt;

    void Awake()
    {
        rt = (RectTransform)transform;
        baseX = rt.anchoredPosition.x;
        if (label == null) label = GetComponentInChildren<Text>();
    }

    void OnDisable() { over = false; }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        Color c = Color.Lerp(label.color, over ? hover : idle, dt * 10f);
        // the odd flicker on the highlighted one, like a dying sign
        if (over && Random.value < 0.015f) c.a *= 0.4f;
        label.color = c;
        Vector2 p = rt.anchoredPosition;
        p.x = Mathf.Lerp(p.x, baseX + (over ? hoverShift : 0f), dt * 12f);
        rt.anchoredPosition = p;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        over = true;
        if (source != null && hoverClip != null) source.PlayOneShot(hoverClip, 0.35f);
    }

    public void OnPointerExit(PointerEventData e) { over = false; }

    public void OnPointerClick(PointerEventData e)
    {
        if (source != null && clickClip != null) source.PlayOneShot(clickClip, 0.6f);
        onClick.Invoke();
    }
}
