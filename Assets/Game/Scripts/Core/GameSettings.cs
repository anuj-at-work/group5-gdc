using UnityEngine;

public static class GameSettings
{
    const string SensKey = "opt_sens";
    const string VolKey = "opt_vol";

    public static float Sensitivity
    {
        get => PlayerPrefs.GetFloat(SensKey, 0.5f);
        set { PlayerPrefs.SetFloat(SensKey, Mathf.Clamp01(value)); }
    }

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(VolKey, 0.85f);
        set
        {
            PlayerPrefs.SetFloat(VolKey, Mathf.Clamp01(value));
            AudioListener.volume = Mathf.Clamp01(value);
        }
    }

    // 0..1 slider -> actual mouse multiplier
    public static float MouseMultiplier => Mathf.Lerp(0.02f, 0.2f, Sensitivity);

    public static void Apply()
    {
        AudioListener.volume = Volume;
    }
}
