using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Slow, slightly drunk first person walk. No jumping - he can barely stand.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] Transform pitchPivot;   // rotates up/down
    [SerializeField] Transform headCam;      // camera, gets bob + sway on top

    [Header("Walking")]
    [SerializeField] float walkSpeed = 1.9f;
    [SerializeField] float hurrySpeed = 3.1f;
    [SerializeField] float backMultiplier = 0.55f;
    [SerializeField] float accel = 6f;
    [SerializeField] float gravity = -18f;

    [Header("Look")]
    [SerializeField] float pitchLimit = 78f;
    [SerializeField] float lookSmoothing = 18f;

    [Header("Drunk")]
    [SerializeField] float swayRoll = 1.6f;
    [SerializeField] float swayYaw = 0.9f;
    [SerializeField] float swaySpeed = 0.23f;
    [SerializeField] float stumbleDrift = 0.18f;

    [Header("Head bob / steps")]
    [SerializeField] float stepLength = 0.78f;
    [SerializeField] float bobHeight = 0.04f;
    [SerializeField] float bobSide = 0.025f;
    [SerializeField] AudioSource stepSource;
    [SerializeField] AudioClip[] stepClips;

    public event Action<Vector3, float> Stepped;   // position, loudness 0..1

    public bool MoveLocked { get; set; }
    public bool LookLocked { get; set; }
    public float Speed01 { get; private set; }
    public float Yaw => transform.eulerAngles.y;

    CharacterController cc;
    Vector3 planarVel;
    float vertVel;
    float pitch;
    Vector2 lookTarget, lookCurrent;
    float stepCycle;
    int lastStep = -1;
    float seed;
    float driftPhase;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        seed = UnityEngine.Random.value * 100f;
        if (headCam == null) headCam = GetComponentInChildren<Camera>().transform;
        if (pitchPivot == null) pitchPivot = headCam.parent;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        if (!LookLocked && mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 d = mouse.delta.ReadValue() * GameSettings.MouseMultiplier;
            lookTarget += d;
        }

        // a bit of smoothing makes it feel heavier, like he's not quite with it
        Vector2 prev = lookCurrent;
        lookCurrent = Vector2.Lerp(lookCurrent, lookTarget, 1f - Mathf.Exp(-lookSmoothing * Time.deltaTime));
        Vector2 step = lookCurrent - prev;
        transform.Rotate(0f, step.x, 0f, Space.Self);
        pitch = Mathf.Clamp(pitch - step.y, -pitchLimit, pitchLimit);
        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector2 input = Vector2.zero;
        bool hurry = false;
        if (!MoveLocked && kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            hurry = kb.leftShiftKey.isPressed;
        }
        input = Vector2.ClampMagnitude(input, 1f);

        float speed = hurry ? hurrySpeed : walkSpeed;
        if (input.y < 0f) speed *= backMultiplier;

        Vector3 wish = (transform.forward * input.y + transform.right * input.x) * speed;

        // drunk drift - he never quite walks straight
        if (input.sqrMagnitude > 0.01f)
        {
            driftPhase += Time.deltaTime * 0.6f;
            float drift = (Mathf.PerlinNoise(seed + 3f, driftPhase) - 0.5f) * 2f * stumbleDrift;
            wish += transform.right * drift * speed;
        }

        planarVel = Vector3.MoveTowards(planarVel, wish, accel * Time.deltaTime);

        if (cc.isGrounded && vertVel < 0f) vertVel = -2f;
        vertVel += gravity * Time.deltaTime;

        Vector3 motion = planarVel + Vector3.up * vertVel;
        cc.Move(motion * Time.deltaTime);

        Vector3 flatVel = cc.velocity; flatVel.y = 0f;
        Speed01 = Mathf.Clamp01(flatVel.magnitude / hurrySpeed);

        UpdateHead(flatVel.magnitude);
    }

    void UpdateHead(float moveSpeed)
    {
        float t = Time.time * swaySpeed;
        float roll = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * swayRoll;
        float yaw = (Mathf.PerlinNoise(seed + 10f, t * 0.8f) - 0.5f) * 2f * swayYaw;

        if (moveSpeed > 0.15f && cc.isGrounded)
            stepCycle += moveSpeed * Time.deltaTime / stepLength;

        float phase = stepCycle * Mathf.PI;
        float amount = Mathf.Clamp01(moveSpeed / walkSpeed);
        float bobY = Mathf.Abs(Mathf.Sin(phase)) * bobHeight * amount;
        float bobX = Mathf.Cos(phase) * bobSide * amount;

        headCam.localPosition = Vector3.Lerp(headCam.localPosition, new Vector3(bobX, bobY, 0f), 12f * Time.deltaTime);
        headCam.localRotation = Quaternion.Euler(0f, yaw, roll + bobX * 20f);

        int stepIndex = Mathf.FloorToInt(stepCycle);
        if (stepIndex != lastStep)
        {
            lastStep = stepIndex;
            if (moveSpeed > 0.15f) PlayStep(moveSpeed);
        }
    }

    void PlayStep(float moveSpeed)
    {
        float loud = Mathf.Clamp01(moveSpeed / hurrySpeed);
        if (stepSource != null && stepClips != null && stepClips.Length > 0)
        {
            stepSource.pitch = UnityEngine.Random.Range(0.92f, 1.06f);
            stepSource.PlayOneShot(stepClips[UnityEngine.Random.Range(0, stepClips.Length)], Mathf.Lerp(0.45f, 0.8f, loud));
        }
        Stepped?.Invoke(transform.position, loud);
    }

    public void Teleport(Vector3 pos, Quaternion rot)
    {
        cc.enabled = false;
        transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rot.eulerAngles.y, 0f));
        cc.enabled = true;
        planarVel = Vector3.zero;
        vertVel = 0f;
        pitch = 0f;
        lookTarget = lookCurrent = Vector2.zero;
        pitchPivot.localRotation = Quaternion.identity;
    }

    // used by cutscene-ish bits (getting caught, ending) to steer the view
    public void SetView(float yawDegrees, float pitchDegrees)
    {
        transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        pitch = Mathf.Clamp(pitchDegrees, -pitchLimit, pitchLimit);
        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        lookTarget = lookCurrent = Vector2.zero;
    }

    public float Pitch => pitch;

    public void Halt()
    {
        planarVel = Vector3.zero;
    }

    public Transform HeadCamera => headCam;
}
