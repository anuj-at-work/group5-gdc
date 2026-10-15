using UnityEngine;

// Small things that are different each time round. Enable from a given loop onwards
// (or only on that loop).
public class LoopVariant : LoopObject
{
    [SerializeField] GameObject target;
    [SerializeField] int fromLoop = 2;
    [SerializeField] int untilLoop = 999;
    [SerializeField] bool hideAfterBoard;

    bool boardDone;

    public override void ApplyState(GameState s) { boardDone = s.BoardComplete; Refresh(s.loop); }
    public override void OnLoopStarted(int loop) => Refresh(loop);
    public override void OnBoardComplete() { boardDone = true; Refresh(GameState.Current.loop); }

    void Refresh(int loop)
    {
        if (target == null) return;
        bool on = loop >= fromLoop && loop <= untilLoop && !(hideAfterBoard && boardDone);
        target.SetActive(on);
    }
}
