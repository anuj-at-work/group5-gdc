using System.Collections;
using UnityEngine;

// The noticeboard by the gate. Also the one place that knows what each scrap looks like.
public class NewspaperBoard : Interactable
{
    public static NewspaperBoard Instance { get; private set; }

    [Header("Scraps")]
    [Tooltip("Full-page sized textures, transparent everywhere except the scrap itself")]
    [SerializeField] Texture2D[] scraps = new Texture2D[GameState.PieceCount];
    [Tooltip("UV rect of each scrap inside the page, for the close-up view")]
    [SerializeField] Rect[] scrapRects = new Rect[GameState.PieceCount];
    [TextArea] [SerializeField] string[] pickupThoughts = new string[GameState.PieceCount];

    [Header("On the board")]
    [SerializeField] Renderer[] pinned = new Renderer[GameState.PieceCount];
    [SerializeField] InspectView inspect;
    [SerializeField] Hud hud;

    [Header("Sound")]
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip pinClip;
    [SerializeField] AudioClip revealClip;
    [SerializeField] AudioSource herVoice;
    [SerializeField] AudioClip thankYou;

    bool busy;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this) Instance = null;
    }

    public override string Prompt
    {
        get
        {
            if (busy) return "";
            var s = GameState.Current;
            if (s.HasUnplacedPieces) return "pin up what you found";
            if (s.PlacedCount > 0) return "read the board";
            return "";
        }
    }

    public override bool CanInteract
    {
        get
        {
            var s = GameState.Current;
            return !busy && (s.HasUnplacedPieces || s.PlacedCount > 0);
        }
    }

    public override void ApplyState(GameState s)
    {
        for (int i = 0; i < pinned.Length; i++)
            if (pinned[i] != null) pinned[i].enabled = s.placed[i];
    }

    // called by whatever held the scrap (ground, postbox, toilet)
    public void TakePiece(int index)
    {
        GameState.Current.collected[index] = true;
        inspect.ShowScrap(scraps[index], scrapRects[index], pickupThoughts[index], null);
    }

    public override void Interact(PlayerInteractor player)
    {
        var s = GameState.Current;
        if (s.HasUnplacedPieces) StartCoroutine(PinUp());
        else inspect.ShowPage(PlacedTextures(), BoardThought(s.PlacedCount), null);
    }

    IEnumerator PinUp()
    {
        busy = true;
        var s = GameState.Current;
        LoopDirector.Instance.SetFocusMode(true);

        for (int i = 0; i < GameState.PieceCount; i++)
        {
            if (!s.collected[i] || s.placed[i]) continue;
            s.placed[i] = true;
            if (pinned[i] != null) pinned[i].enabled = true;
            if (source != null && pinClip != null) source.PlayOneShot(pinClip, 0.9f);
            yield return new WaitForSeconds(0.6f);
        }

        LoopDirector.Instance.SetFocusMode(false);
        busy = false;

        if (s.BoardComplete)
        {
            yield return new WaitForSeconds(0.4f);
            if (source != null && revealClip != null) source.PlayOneShot(revealClip, 0.9f);
            inspect.ShowPage(PlacedTextures(),
                "It's her. The voice calling for help.\nShe's been asking the whole time.",
                OnRevealClosed);
        }
        else
        {
            hud.Toast(BoardThought(s.PlacedCount));
        }
    }

    void OnRevealClosed()
    {
        GameState.Save();
        LoopObject.BroadcastBoardComplete();
        LoopDirector.Instance.QuietStreet();
        if (herVoice != null && thankYou != null) herVoice.PlayOneShot(thankYou, 0.8f);
        hud.Toast("The light in the gate is gone.", 5f);
    }

    Texture2D[] PlacedTextures()
    {
        var s = GameState.Current;
        var list = new Texture2D[GameState.PieceCount];
        for (int i = 0; i < list.Length; i++) list[i] = s.placed[i] ? scraps[i] : null;
        return list;
    }

    static string BoardThought(int placed)
    {
        switch (placed)
        {
            case 1: return "Part of an old newspaper. Most of it's missing.";
            case 2: return "Half a page. Something happened on this street.";
            case 3: return "One piece left. The lamp back there... was it always out?";
            default: return "";
        }
    }
}
