using UnityEngine;

// A scrap lying somewhere in the street.
public class NewspaperScrap : Interactable
{
    [SerializeField] int pieceIndex;
    [Tooltip("Only shows up once the dead lamp above it comes back on")]
    [SerializeField] bool needsLateLamp;

    Renderer[] renderers;
    Collider[] colliders;
    bool visible;

    protected override void Awake()
    {
        base.Awake();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
    }

    public override string Prompt => "pick up the newspaper";
    public override bool CanInteract => visible;

    public override void ApplyState(GameState s) => Refresh();
    public override void OnLoopStarted(int loop) => Refresh();

    void Refresh()
    {
        var s = GameState.Current;
        visible = !s.collected[pieceIndex] && (!needsLateLamp || s.LateLampLit);
        foreach (var r in renderers) r.enabled = visible;
        foreach (var c in colliders) c.enabled = visible;
    }

    public override void Interact(PlayerInteractor player)
    {
        visible = false;
        foreach (var r in renderers) r.enabled = false;
        foreach (var c in colliders) c.enabled = false;
        NewspaperBoard.Instance.TakePiece(pieceIndex);
    }
}
