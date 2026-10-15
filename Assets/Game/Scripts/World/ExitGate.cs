using System.Collections;
using UnityEngine;

public class ExitGate : LoopObject
{
    [SerializeField] Renderer[] glowRenderers;
    [SerializeField, ColorUsage(false, true)] Color glowColor = new Color(1f, 0.85f, 0.6f) * 4f;
    [SerializeField] Light[] glowLights;
    [SerializeField] AudioSource hum;
    [SerializeField] ParticleSystem motes;
    [SerializeField] AudioClip dieClip;

    float level = 1f;
    float[] lightBase;
    float humBase;
    bool glowing = true;
    MaterialPropertyBlock mpb;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    protected override void Awake()
    {
        base.Awake();
        mpb = new MaterialPropertyBlock();
        lightBase = new float[glowLights.Length];
        for (int i = 0; i < glowLights.Length; i++) lightBase[i] = glowLights[i].intensity;
        if (hum != null) humBase = hum.volume;
    }

    public override void ApplyState(GameState s)
    {
        glowing = !s.BoardComplete;
        level = glowing ? 1f : 0f;
        if (motes != null)
        {
            if (glowing) motes.Play(); else motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        Apply(level);
    }

    public override void OnBoardComplete()
    {
        if (glowing) StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        glowing = false;
        if (motes != null) motes.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (hum != null && dieClip != null) hum.PlayOneShot(dieClip, 0.9f);

        float t = 0f;
        float start = level;
        while (t < 3.5f)
        {
            t += Time.deltaTime;
            float k = t / 3.5f;
            // it gutters out rather than fading cleanly
            float stutter = Random.value < 0.18f ? Random.Range(0f, 0.4f) : 1f;
            level = Mathf.Lerp(start, 0f, k) * stutter;
            yield return null;
        }
        level = 0f;
    }

    void Update()
    {
        float v = level;
        if (glowing)
        {
            float n = Mathf.PerlinNoise(Time.time * 0.8f, 0.3f);
            v *= 0.82f + n * 0.3f;
        }
        Apply(v);
    }

    void Apply(float v)
    {
        foreach (var r in glowRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, glowColor * v);
            r.SetPropertyBlock(mpb);
        }
        for (int i = 0; i < glowLights.Length; i++)
            if (glowLights[i] != null) glowLights[i].intensity = lightBase[i] * v;
        if (hum != null) hum.volume = humBase * Mathf.Clamp01(v);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<FirstPersonController>() != null && LoopDirector.Instance != null)
            LoopDirector.Instance.OnGateEntered();
    }
}
