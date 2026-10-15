using System;
using UnityEngine;

// Everything that has to survive a loop (and a quit) lives here.
// The street itself never reloads between loops, so most objects just keep
// their state naturally - this is mainly for Continue from the menu.
[Serializable]
public class GameState
{
    public const int PieceCount = 4;
    const string SaveKey = "g5_street_save";

    public int loop = 1;
    public bool hasCrowbar;
    public bool entryDoorOpen;
    public bool exitDoorOpen;
    public bool postboxOpen;
    public bool[] collected = new bool[PieceCount];
    public bool[] placed = new bool[PieceCount];

    static GameState current;
    public static GameState Current
    {
        get
        {
            if (current == null) current = new GameState();
            return current;
        }
    }

    public int PlacedCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < PieceCount; i++) if (placed[i]) n++;
            return n;
        }
    }

    public bool HasUnplacedPieces
    {
        get
        {
            for (int i = 0; i < PieceCount; i++) if (collected[i] && !placed[i]) return true;
            return false;
        }
    }

    public bool BoardComplete => PlacedCount >= PieceCount;

    // the dead lamp near the end only comes back once three scraps are up
    public bool LateLampLit => PlacedCount >= PieceCount - 1;

    public static bool HasSave => PlayerPrefs.HasKey(SaveKey);

    public static void NewGame()
    {
        current = new GameState();
        PlayerPrefs.DeleteKey(SaveKey);
    }

    public static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Current));
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        if (!HasSave) { NewGame(); return; }
        try
        {
            current = JsonUtility.FromJson<GameState>(PlayerPrefs.GetString(SaveKey));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Couldn't read save, starting fresh: " + e.Message);
            current = new GameState();
        }
        if (current.collected == null || current.collected.Length != PieceCount) current.collected = new bool[PieceCount];
        if (current.placed == null || current.placed.Length != PieceCount) current.placed = new bool[PieceCount];
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
