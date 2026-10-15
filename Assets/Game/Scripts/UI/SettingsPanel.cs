using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] Slider sensitivity;
    [SerializeField] Slider volume;

    bool shown;

    void Awake()
    {
        sensitivity.SetValueWithoutNotify(GameSettings.Sensitivity);
        volume.SetValueWithoutNotify(GameSettings.Volume);
        sensitivity.onValueChanged.AddListener(v => GameSettings.Sensitivity = v);
        volume.onValueChanged.AddListener(v => GameSettings.Volume = v);
        SetShown(false, true);
    }

    public void Toggle() => SetShown(!shown, false);
    public void Hide() => SetShown(false, true);

    void SetShown(bool on, bool instant)
    {
        shown = on;
        group.blocksRaycasts = on;
        group.interactable = on;
        if (instant) group.alpha = on ? 1f : 0f;
    }

    void Update()
    {
        group.alpha = Mathf.MoveTowards(group.alpha, shown ? 1f : 0f, Time.unscaledDeltaTime * 5f);
    }
}
