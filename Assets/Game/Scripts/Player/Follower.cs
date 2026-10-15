using System;
using System.Collections;
using UnityEngine;

// The thing behind you. It's never rendered - it's footsteps, breathing and
// the rule: walk back too far or turn around too far and it has you.
public class Follower : MonoBehaviour
{
    [SerializeField] StreetPath path;
    [SerializeField] FirstPersonController player;

    [Header("Rule")]
    [Tooltip("How many metres you can drift back from the furthest point you reached")]
    [SerializeField] float backAllowance = 1.4f;
    [SerializeField] float warnAngle = 65f;
    [SerializeField] float killAngle = 112f;
    [Tooltip("Seconds you can stay over the line before it catches you")]
    [SerializeField] float grace = 0.3f;

    [Header("Presence")]
    [SerializeField] AudioSource echoSource;
    [SerializeField] AudioClip[] echoSteps;
    [SerializeField] float echoDelay = 0.34f;
    [SerializeField] float followDistance = 7.5f;
    [SerializeField] AudioSource droneSource;
    [SerializeField] AudioSource heartSource;
    [SerializeField] AudioSource breathSource;
    [SerializeField] AudioClip[] breaths;

    public event Action Caught;

    public bool Active { get; set; }
    public float Tension { get; private set; }
    public float CatchDirectionYaw { get; private set; }

    float furthest;
    float overTimer;
    float idle;
    float closeness;
    float nextBreath;
    bool wasMoving;
    float stoppedAt;
    bool extraStepDone = true;

    void OnEnable() { if (player != null) player.Stepped += OnPlayerStepped; }
    void OnDisable() { if (player != null) player.Stepped -= OnPlayerStepped; }

    public void ResetProgress()
    {
        furthest = path.Project(player.transform.position);
        overTimer = 0f;
        idle = 0f;
        closeness = 0f;
        Tension = 0f;
    }

    void Update()
    {
        if (!Active)
        {
            Tension = Mathf.MoveTowards(Tension, 0f, Time.deltaTime * 0.8f);
            UpdateAudio();
            return;
        }

        Vector3 pos = player.transform.position;
        float d = path.Project(pos, out Vector3 tangent);
        if (d > furthest) furthest = d;

        float back = furthest - d;
        Vector3 fwd = player.transform.forward; fwd.y = 0f;
        float angle = Vector3.Angle(fwd, tangent);

        float fromAngle = Mathf.InverseLerp(warnAngle, killAngle, angle);
        float fromBack = Mathf.InverseLerp(backAllowance * 0.3f, backAllowance, back);

        bool moving = player.Speed01 > 0.05f;
        idle = moving ? Mathf.Max(0f, idle - Time.deltaTime * 3f) : idle + Time.deltaTime;
        closeness = Mathf.Clamp01((idle - 6f) / 30f);

        float target = Mathf.Max(fromAngle, fromBack, closeness * 0.4f);
        float rate = target > Tension ? 4f : 0.9f;
        Tension = Mathf.MoveTowards(Tension, target, rate * Time.deltaTime);

        bool over = angle > killAngle || back > backAllowance;
        if (over)
        {
            overTimer += Time.deltaTime;
            if (overTimer >= grace)
            {
                Active = false;
                CatchDirectionYaw = Quaternion.LookRotation(-tangent).eulerAngles.y;
                Caught?.Invoke();
                return;
            }
        }
        else overTimer = Mathf.Max(0f, overTimer - Time.deltaTime);

        // keep the "him" audio somewhere behind on the street
        float behind = Mathf.Lerp(followDistance, 2.4f, Mathf.Max(Tension, closeness));
        Vector3 p = path.Evaluate(Mathf.Max(0f, d - behind));
        if (echoSource != null) echoSource.transform.position = p + Vector3.up * 1.2f;
        if (breathSource != null) breathSource.transform.position = pos - tangent * 0.9f + Vector3.up * 1.7f;

        // he takes one more step after you stop. every time.
        if (wasMoving && !moving) { stoppedAt = Time.time; extraStepDone = false; }
        if (!extraStepDone && !moving && Time.time - stoppedAt > 0.75f)
        {
            extraStepDone = true;
            if (UnityEngine.Random.value < 0.6f) PlayEcho(0.55f);
        }
        wasMoving = moving;

        if ((closeness > 0.55f || Tension > 0.65f) && Time.time > nextBreath && breathSource != null && breaths.Length > 0)
        {
            nextBreath = Time.time + UnityEngine.Random.Range(4f, 7f);
            breathSource.PlayOneShot(breaths[UnityEngine.Random.Range(0, breaths.Length)], 0.5f + Tension * 0.4f);
        }

        UpdateAudio();
    }

    void UpdateAudio()
    {
        if (droneSource != null) droneSource.volume = Mathf.Lerp(0f, 0.75f, Tension);
        if (heartSource != null)
        {
            heartSource.volume = Mathf.Lerp(0f, 0.9f, Mathf.InverseLerp(0.15f, 0.8f, Tension));
            heartSource.pitch = Mathf.Lerp(0.85f, 1.45f, Tension);
        }
    }

    void OnPlayerStepped(Vector3 pos, float loud)
    {
        if (!Active) return;
        // skipping one now and then is worse than always matching
        if (UnityEngine.Random.value < 0.1f) return;
        StartCoroutine(EchoLater(loud));
    }

    IEnumerator EchoLater(float loud)
    {
        yield return new WaitForSeconds(echoDelay * UnityEngine.Random.Range(0.85f, 1.2f));
        if (Active) PlayEcho(loud);
    }

    void PlayEcho(float loud)
    {
        if (echoSource == null || echoSteps == null || echoSteps.Length == 0) return;
        echoSource.pitch = UnityEngine.Random.Range(0.78f, 0.9f);
        float vol = Mathf.Lerp(0.35f, 0.9f, Mathf.Max(Tension, closeness)) * Mathf.Lerp(0.6f, 1f, loud);
        echoSource.PlayOneShot(echoSteps[UnityEngine.Random.Range(0, echoSteps.Length)], vol);
    }

    // where the hands come from when it catches you
    public Vector3 PointBehindPlayer(float metres)
    {
        float d = path.Project(player.transform.position, out Vector3 t);
        return player.transform.position - t * metres;
    }
}
