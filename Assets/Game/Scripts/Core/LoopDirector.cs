using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs the night: menu -> wake up at the start -> walk -> gate -> wake up again...
public class LoopDirector : MonoBehaviour
{
    public enum Phase { Menu, Waking, Playing, Looping, Caught, Ending }

    public static LoopDirector Instance { get; private set; }

    [Header("Player")]
    [SerializeField] FirstPersonController player;
    [SerializeField] PlayerInteractor interactor;
    [SerializeField] Transform spawnPoint;
    [SerializeField] Follower follower;

    [Header("World")]
    [SerializeField] ExitGate gate;
    [SerializeField] GameObject him;            // the silhouette, shown for a few frames when you get caught
    [SerializeField] PostFxDriver postFx;

    [Header("UI")]
    [SerializeField] ScreenFader fader;
    [SerializeField] MainMenu menu;
    [SerializeField] Hud hud;

    [Header("Audio")]
    [SerializeField] AudioSource sfx2D;
    [SerializeField] AudioClip loopWhoosh;
    [SerializeField] AudioClip wakeGasp;
    [SerializeField] AudioClip caughtSting;
    [SerializeField] AudioClip endingTone;
    [SerializeField] AudioSource[] ambience;     // faded down for transitions and the ending

    public Phase Current { get; private set; } = Phase.Menu;
    public bool CanPause => Current == Phase.Playing && !InFocus && Time.frameCount > focusFrame + 1;
    int focusFrame;

    float[] ambienceVolumes;

    void Awake()
    {
        Instance = this;
        ambienceVolumes = new float[ambience.Length];
        for (int i = 0; i < ambience.Length; i++) ambienceVolumes[i] = ambience[i] != null ? ambience[i].volume : 0f;
        if (him != null) him.SetActive(false);
    }

    void Start()
    {
        GameSettings.Apply();
        follower.Active = false;
        follower.Caught += OnCaught;
        SetPlayerControl(false);
        player.gameObject.SetActive(false);

        if (menu != null)
        {
            menu.Open();
            fader.SetBlack();
            StartCoroutine(fader.FadeTo(0f, 3f));
        }
        else NewGame();
    }

    void OnDestroy()
    {
        if (follower != null) follower.Caught -= OnCaught;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (postFx != null) postFx.Tension = follower.Tension;
    }

    // ---- entry points from the menu ----

    public void NewGame()
    {
        GameState.NewGame();
        StartCoroutine(Begin(true));
    }

    public void Continue()
    {
        GameState.Load();
        StartCoroutine(Begin(false));
    }

    IEnumerator Begin(bool fresh)
    {
        Current = Phase.Waking;
        yield return fader.FadeTo(1f, 1.2f);
        if (menu != null) menu.Close();

        player.gameObject.SetActive(true);
        LoopObject.BroadcastApply(GameState.Current);
        GameState.Save();

        if (fresh)
        {
            yield return fader.ShowLines(new[]
            {
                "11:52 p.m.",
                "You missed the last train again.",
                "Somewhere between the station and home,\nthe streets stopped looking familiar.",
                "Someone is walking behind you.\nWhatever happens, don't turn around."
            }, true);
        }
        else
        {
            yield return fader.ShowLines(new[] { NightLine(GameState.Current.loop) }, false);
        }

        yield return WakeUp();
    }

    static string NightLine(int loop)
    {
        // not really a counter. just enough to tell you it's the same night.
        int minute = 52 + Mathf.Min(loop - 1, 7);
        return "11:" + minute.ToString("00") + " p.m.";
    }

    IEnumerator WakeUp()
    {
        player.Teleport(spawnPoint.position, spawnPoint.rotation);
        player.SetView(spawnPoint.eulerAngles.y, 35f);
        LoopObject.BroadcastLoop(GameState.Current.loop);
        follower.ResetProgress();
        SetAmbience(1f, 0f);
        if (wakeGasp != null) sfx2D.PlayOneShot(wakeGasp, 0.7f);

        StartCoroutine(fader.FadeTo(0f, 2.4f));
        float t = 0f;
        float yaw = spawnPoint.eulerAngles.y;
        while (t < 2.2f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 2.2f);
            player.SetView(yaw + Mathf.Sin(t * 2.1f) * 4f * (1f - k), Mathf.Lerp(35f, 0f, k));
            yield return null;
        }

        follower.ResetProgress();
        follower.Active = true;
        SetPlayerControl(true);
        Current = Phase.Playing;
    }

    // ---- the gate ----

    public void OnGateEntered()
    {
        if (Current != Phase.Playing) return;
        if (GameState.Current.BoardComplete) StartCoroutine(Ending());
        else StartCoroutine(LoopAround());
    }

    IEnumerator LoopAround()
    {
        Current = Phase.Looping;
        follower.Active = false;
        SetPlayerControl(false);
        if (loopWhoosh != null) sfx2D.PlayOneShot(loopWhoosh, 0.9f);
        if (postFx != null) postFx.Warp(1.6f);
        StartCoroutine(FadeAmbience(0f, 1.2f));

        // keep walking forward into it for a moment
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            player.transform.position += player.transform.forward * 1.2f * Time.deltaTime;
            yield return null;
        }

        yield return fader.FadeTo(1f, 0.5f);
        yield return new WaitForSeconds(1.2f);

        GameState.Current.loop++;
        GameState.Save();
        yield return WakeUp();
    }

    // ---- getting caught ----

    void OnCaught()
    {
        if (Current != Phase.Playing) return;
        StartCoroutine(CaughtRoutine());
    }

    IEnumerator CaughtRoutine()
    {
        Current = Phase.Caught;
        SetPlayerControl(false);
        if (hud != null) hud.HideAll();
        if (caughtSting != null) sfx2D.PlayOneShot(caughtSting, 1f);
        SetAmbience(0f, 0f);

        // you get dragged round to face it
        float fromYaw = player.Yaw;
        float toYaw = follower.CatchDirectionYaw;
        float fromPitch = player.Pitch;
        if (him != null)
        {
            Vector3 p = follower.PointBehindPlayer(1.15f);
            p.y = player.transform.position.y;
            him.transform.position = p;
            Vector3 look = player.transform.position - p; look.y = 0f;
            if (look.sqrMagnitude > 0.001f) him.transform.rotation = Quaternion.LookRotation(look);
            him.SetActive(true);
        }
        if (postFx != null) postFx.Flash();

        float t = 0f;
        while (t < 0.16f)
        {
            t += Time.deltaTime;
            float k = t / 0.16f;
            player.SetView(Mathf.LerpAngle(fromYaw, toYaw, k), Mathf.Lerp(fromPitch, -12f, k));
            yield return null;
        }
        yield return new WaitForSeconds(0.55f);

        fader.SetBlack();
        if (him != null) him.SetActive(false);
        yield return new WaitForSeconds(1.6f);

        yield return fader.ShowLines(new[] { "You turned around.", "He was there the whole time." }, false, true);
        yield return new WaitForSeconds(0.8f);
        BackToMenu();
    }

    // ---- the true ending ----

    IEnumerator Ending()
    {
        Current = Phase.Ending;
        follower.Active = false;
        SetPlayerControl(false);
        StartCoroutine(FadeAmbience(0f, 3f));
        if (endingTone != null) sfx2D.PlayOneShot(endingTone, 0.8f);

        // he just stands in the doorway. nothing happens. that's the point.
        yield return new WaitForSeconds(2.5f);
        yield return fader.FadeTo(1f, 4f);
        yield return new WaitForSeconds(1.5f);

        GameState.ClearSave();
        yield return fader.ShowLines(new[]
        {
            "The lane was boarded up in the winter of 1987.",
            "People who live nearby say that some nights\nyou can still hear a girl asking for help.",
            "Nobody ever answers.",
            "Nobody ever looks back."
        }, false, true);
        yield return new WaitForSeconds(1f);
        yield return fader.ShowTitleCard();
        BackToMenu();
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---- helpers ----

    public void SetPlayerControl(bool on)
    {
        player.MoveLocked = !on;
        player.LookLocked = !on;
        if (interactor != null) interactor.Blocked = !on;
        if (!on) player.Halt();
        Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !on && (Current == Phase.Menu);
    }

    // used by the lock / reading so the rule doesn't fire while you're busy
    public void SetFocusMode(bool focused)
    {
        if (Current != Phase.Playing) return;
        InFocus = focused;
        focusFrame = Time.frameCount;
        follower.Active = !focused;
        if (!focused) follower.ResetProgress();
        player.MoveLocked = focused;
        player.LookLocked = focused;
        if (interactor != null)
        {
            interactor.Blocked = focused;
            interactor.Cooldown(0.3f);
        }
        if (focused) player.Halt();
    }

    public bool InFocus { get; private set; }

    void SetAmbience(float mul, float _)
    {
        for (int i = 0; i < ambience.Length; i++)
            if (ambience[i] != null) ambience[i].volume = ambienceVolumes[i] * mul;
    }

    IEnumerator FadeAmbience(float to, float time)
    {
        float[] from = new float[ambience.Length];
        for (int i = 0; i < ambience.Length; i++) from[i] = ambience[i] != null ? ambience[i].volume : 0f;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            for (int i = 0; i < ambience.Length; i++)
                if (ambience[i] != null) ambience[i].volume = Mathf.Lerp(from[i], ambienceVolumes[i] * to, t / time);
            yield return null;
        }
    }

    // the board finishing lowers the street's ambience for good
    public void QuietStreet()
    {
        for (int i = 0; i < ambienceVolumes.Length; i++) ambienceVolumes[i] *= 0.35f;
        StartCoroutine(FadeAmbience(1f, 4f));
    }
}
