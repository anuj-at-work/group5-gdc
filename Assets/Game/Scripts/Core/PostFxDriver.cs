using UnityEngine;
using UnityEngine.Rendering;

// Blends a few extra volumes on top of the street grade.
public class PostFxDriver : MonoBehaviour
{
    [SerializeField] Volume tension;   // vignette closes in, colour drains
    [SerializeField] Volume flash;     // getting caught
    [SerializeField] Volume warp;      // walking into the gate
    [SerializeField] Volume drunk;     // always slightly on, breathes in and out

    public float Tension { get; set; }

    float flashLevel;
    float warpLevel;
    float warpTarget;
    float warpSpeed = 1f;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (tension != null)
        {
            // pulse with the heartbeat once it gets bad
            float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * Mathf.Lerp(3.2f, 6f, Tension))), 8f);
            tension.weight = Mathf.Clamp01(Tension + beat * Tension * 0.25f);
        }

        flashLevel = Mathf.MoveTowards(flashLevel, 0f, dt * 1.8f);
        if (flash != null) flash.weight = flashLevel;

        warpLevel = Mathf.MoveTowards(warpLevel, warpTarget, dt * warpSpeed);
        if (warp != null) warp.weight = warpLevel;
        if (warpLevel >= 1f) warpTarget = 0f;

        if (drunk != null)
            drunk.weight = 0.55f + Mathf.Sin(Time.time * 0.37f) * 0.25f + Mathf.Sin(Time.time * 0.91f) * 0.1f;
    }

    public void Flash() => flashLevel = 1f;

    public void Warp(float time)
    {
        warpTarget = 1f;
        warpSpeed = 1f / Mathf.Max(0.1f, time);
    }
}
