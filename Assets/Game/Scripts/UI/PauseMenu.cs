using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] MenuItem resumeItem;
    [SerializeField] MenuItem settingsItem;
    [SerializeField] MenuItem giveUpItem;
    [SerializeField] SettingsPanel settings;

    bool paused;

    void Awake()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
        resumeItem.onClick.AddListener(Resume);
        settingsItem.onClick.AddListener(() => settings.Toggle());
        giveUpItem.onClick.AddListener(GiveUp);
    }

    void Update()
    {
        group.alpha = Mathf.MoveTowards(group.alpha, paused ? 1f : 0f, Time.unscaledDeltaTime * 6f);

        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        if (paused) Resume();
        else if (LoopDirector.Instance != null && LoopDirector.Instance.CanPause) Pause();
    }

    void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        group.blocksRaycasts = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        group.blocksRaycasts = false;
        settings.Hide();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void GiveUp()
    {
        paused = false;
        LoopDirector.Instance.BackToMenu();
    }

#if !UNITY_EDITOR
    void OnApplicationFocus(bool focus)
    {
        // alt-tabbing out mid-walk shouldn't get you killed
        if (!focus && LoopDirector.Instance != null && LoopDirector.Instance.CanPause && !paused) Pause();
    }
#endif
}
