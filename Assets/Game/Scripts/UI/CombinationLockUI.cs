using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Four-wheel padlock on the postbox. Wheels remember where you left them.
public class CombinationLockUI : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] RectTransform body;
    [SerializeField] RectTransform[] wheels;        // masked windows, left to right
    [SerializeField] RectTransform[] wheelStrips;   // inside each window: prev/current/next stacked
    [SerializeField] Text[] prevDigits;
    [SerializeField] Text[] digits;
    [SerializeField] Text[] nextDigits;
    [SerializeField] RectTransform marker;          // little notch under the selected wheel
    [SerializeField] Text hint;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip tick;
    [SerializeField] AudioClip wrong;
    [SerializeField] AudioClip open;

    readonly int[] values = new int[4];
    float[] roll = new float[4];
    int selected;
    string code;
    Action onOpened;
    bool isOpen;
    bool solving;
    float openedAt;
    float shake;

    void Awake()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
        Redraw();
    }

    public void Open(string combination, Action opened)
    {
        code = combination;
        onOpened = opened;
        isOpen = true;
        solving = false;
        openedAt = Time.unscaledTime;
        group.blocksRaycasts = true;
        Redraw();
        LoopDirector.Instance.SetFocusMode(true);
    }

    void Close()
    {
        isOpen = false;
        group.blocksRaycasts = false;
        LoopDirector.Instance.SetFocusMode(false);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        group.alpha = Mathf.MoveTowards(group.alpha, isOpen ? 1f : 0f, dt * 4f);

        for (int i = 0; i < 4; i++)
        {
            roll[i] = Mathf.Lerp(roll[i], 0f, dt * 16f);
            wheelStrips[i].anchoredPosition = new Vector2(wheelStrips[i].anchoredPosition.x, roll[i]);
        }

        shake = Mathf.MoveTowards(shake, 0f, dt * 3f);
        body.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 70f) * 12f * shake, 0f);

        Vector2 mp = marker.anchoredPosition;
        mp.x = Mathf.Lerp(mp.x, wheels[selected].anchoredPosition.x, dt * 18f);
        marker.anchoredPosition = mp;

        if (!isOpen || solving || Time.unscaledTime - openedAt < 0.2f) return;

        var kb = Keyboard.current;
        var m = Mouse.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame || (m != null && m.rightButton.wasPressedThisFrame))
        {
            Close();
            return;
        }

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Select(selected - 1);
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Select(selected + 1);

        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Roll(-1);
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Roll(1);

        if (m != null)
        {
            float scroll = m.scroll.ReadValue().y;
            if (scroll > 0.1f) Roll(-1);
            else if (scroll < -0.1f) Roll(1);
        }

        if (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            StartCoroutine(Try());
    }

    // hooked to the invisible buttons over each wheel
    public void ClickWheel(int index)
    {
        if (!isOpen || solving) return;
        if (index == selected) Roll(1);
        else Select(index);
    }

    void Select(int i)
    {
        selected = (i + 4) % 4;
    }

    void Roll(int dir)
    {
        values[selected] = (values[selected] + dir + 10) % 10;
        roll[selected] = dir * -38f;
        if (source != null && tick != null)
        {
            source.pitch = UnityEngine.Random.Range(0.95f, 1.08f);
            source.PlayOneShot(tick, 0.7f);
        }
        Redraw();
    }

    void Redraw()
    {
        for (int i = 0; i < 4; i++)
        {
            digits[i].text = values[i].ToString();
            prevDigits[i].text = ((values[i] + 9) % 10).ToString();
            nextDigits[i].text = ((values[i] + 1) % 10).ToString();
        }
    }

    IEnumerator Try()
    {
        string entered = "" + values[0] + values[1] + values[2] + values[3];
        if (entered != code)
        {
            shake = 1f;
            if (source != null && wrong != null) { source.pitch = 1f; source.PlayOneShot(wrong, 0.9f); }
            yield break;
        }

        solving = true;
        if (source != null && open != null) { source.pitch = 1f; source.PlayOneShot(open, 1f); }
        yield return new WaitForSecondsRealtime(0.7f);
        Close();
        onOpened?.Invoke();
    }
}
