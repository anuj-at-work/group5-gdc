using System.Collections;
using UnityEngine;

public class StreetLamp : LoopObject
{
    public enum Mode { Steady, Flicker, Dead, ComesBackLate }

    [SerializeField] Mode mode = Mode.Steady;
    [SerializeField] Light[] lights;
    [SerializeField] Renderer bulb;
    [SerializeField, ColorUsage(false, true)] Color bulbColor = new Color(0.75f, 0.95f, 1f) * 6f;
    [Range(0f, 1f)] [SerializeField] float badness = 0.5f;   // how often a Flicker lamp acts up

    [Header("Sound")]
    [SerializeField] AudioSource buzz;
    [SerializeField] AudioSource sparks;
    [SerializeField] AudioClip[] zaps;

    float[] baseIntensity;
    float baseBuzz;
    float level = 1f;
    float master = 1f;
    MaterialPropertyBlock mpb;
    Coroutine routine;
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    protected override void Awake()
    {
        base.Awake();
        mpb = new MaterialPropertyBlock();
        baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
        if (buzz != null) baseBuzz = buzz.volume;
    }

    void OnEnable() { Restart(); }

    public override void ApplyState(GameState s) => Restart();
    public override void OnLoopStarted(int loop) => Restart();

    public override void OnBoardComplete()
    {
        // everything gets a bit weaker once you know
        master = 0.55f;
        if (mode == Mode.Steady) { mode = Mode.Flicker; badness = 0.25f; Restart(); }
    }

    void Restart()
    {
        if (!isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        switch (mode)
        {
            case Mode.Dead:
                Set(0f);
                yield break;

            case Mode.ComesBackLate:
                if (!GameState.Current.LateLampLit) { Set(0f); yield break; }
                // first time you see it on it struggles a little
                yield return Stutter(Random.Range(5, 9), 0.04f, 0.2f);
                break;

            case Mode.Flicker:
                while (true)
                {
                    Set(Random.Range(0.92f, 1f));
                    yield return new WaitForSeconds(Mathf.Lerp(7f, 1.2f, badness) * Random.Range(0.5f, 1.4f));
                    yield return Stutter(Random.Range(2, 6 + Mathf.RoundToInt(badness * 8f)), 0.03f, 0.14f);
                    if (Random.value < badness * 0.6f)
                    {
                        Set(0f);
                        yield return new WaitForSeconds(Random.Range(0.4f, 2.2f));
                        yield return Stutter(Random.Range(2, 4), 0.03f, 0.1f);
                    }
                }
        }

        // steady: still a tiny bit of mains wobble
        while (true)
        {
            Set(1f - Mathf.PerlinNoise(Time.time * 3f, transform.position.x) * 0.06f);
            yield return null;
        }
    }

    IEnumerator Stutter(int count, float min, float max)
    {
        for (int i = 0; i < count; i++)
        {
            Set(Random.value < 0.5f ? Random.Range(0f, 0.15f) : Random.Range(0.6f, 1f));
            if (Random.value < 0.35f) Zap();
            yield return new WaitForSeconds(Random.Range(min, max));
        }
        Set(1f);
    }

    void Zap()
    {
        if (sparks == null || zaps == null || zaps.Length == 0) return;
        sparks.pitch = Random.Range(0.85f, 1.2f);
        sparks.PlayOneShot(zaps[Random.Range(0, zaps.Length)], Random.Range(0.3f, 0.6f));
    }

    void Set(float v)
    {
        level = v;
        float k = level * master;
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = baseIntensity[i] * k;

        if (bulb != null)
        {
            bulb.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionId, bulbColor * Mathf.Max(0.02f, k));
            bulb.SetPropertyBlock(mpb);
        }
        if (buzz != null) buzz.volume = baseBuzz * k;
    }
}
