using System.Collections.Generic;
using UnityEngine;

// Base for anything in the street that cares about the loop.
// Registers itself in Awake so it also gets callbacks while disabled.
public abstract class LoopObject : MonoBehaviour
{
    static readonly List<LoopObject> all = new List<LoopObject>();

    protected virtual void Awake() { all.Add(this); }
    protected virtual void OnDestroy() { all.Remove(this); }

    // called once when the scene starts (after a load) so objects can match the save
    public virtual void ApplyState(GameState s) { }

    // called every time the player wakes up at the start of the street
    public virtual void OnLoopStarted(int loop) { }

    // board finished - the street goes quiet
    public virtual void OnBoardComplete() { }

    public static void BroadcastApply(GameState s)
    {
        for (int i = all.Count - 1; i >= 0; i--) all[i].ApplyState(s);
    }

    public static void BroadcastLoop(int loop)
    {
        for (int i = all.Count - 1; i >= 0; i--) all[i].OnLoopStarted(loop);
    }

    public static void BroadcastBoardComplete()
    {
        for (int i = all.Count - 1; i >= 0; i--) all[i].OnBoardComplete();
    }
}
