using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] GameObject menuCamera;
    [SerializeField] Transform camFrom;
    [SerializeField] Transform camTo;
    [SerializeField] float dollyTime = 70f;
    [SerializeField] MenuItem continueItem;
    [SerializeField] MenuItem newItem;
    [SerializeField] MenuItem settingsItem;
    [SerializeField] MenuItem quitItem;
    [SerializeField] SettingsPanel settings;
    [SerializeField] RectTransform list;

    bool open;
    bool leaving;
    float t;

    void Awake()
    {
        continueItem.onClick.AddListener(() => Go(false));
        newItem.onClick.AddListener(() => Go(true));
        settingsItem.onClick.AddListener(() => settings.Toggle());
        quitItem.onClick.AddListener(Quit);
    }

    public void Open()
    {
        open = true;
        leaving = false;
        menuCamera.SetActive(true);
        group.alpha = 0f;
        group.blocksRaycasts = true;
        group.interactable = true;
        continueItem.gameObject.SetActive(GameState.HasSave);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        t = 0f;
    }

    public void Close()
    {
        open = false;
        menuCamera.SetActive(false);
        group.alpha = 0f;
        group.blocksRaycasts = false;
        settings.Hide();
    }

    void Go(bool fresh)
    {
        if (leaving) return;
        leaving = true;
        group.interactable = false;
        Cursor.visible = false;
        if (fresh) LoopDirector.Instance.NewGame();
        else LoopDirector.Instance.Continue();
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        if (!open) return;
        t += Time.unscaledDeltaTime;
        group.alpha = Mathf.MoveTowards(group.alpha, leaving ? 0f : (t > 1.2f ? 1f : 0f), Time.unscaledDeltaTime * 0.8f);

        // very slow push down the street
        float k = Mathf.PingPong(t / dollyTime, 1f);
        k = Mathf.SmoothStep(0f, 1f, k);
        Transform cam = menuCamera.transform;
        cam.SetPositionAndRotation(
            Vector3.Lerp(camFrom.position, camTo.position, k) + new Vector3(0f, Mathf.Sin(t * 0.4f) * 0.03f, 0f),
            Quaternion.Slerp(camFrom.rotation, camTo.rotation, k) * Quaternion.Euler(0f, Mathf.Sin(t * 0.13f) * 1.5f, Mathf.Sin(t * 0.21f) * 0.6f));
    }
}
