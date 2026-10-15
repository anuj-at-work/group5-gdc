using System.Collections;
using UnityEngine;

public class PostBox : Interactable
{
    [SerializeField] string code = "2317";
    [SerializeField] int pieceIndex = 1;
    [SerializeField] CombinationLockUI lockUI;
    [SerializeField] Transform door;
    [SerializeField] Vector3 doorOpenEuler = new Vector3(0f, -105f, 0f);
    [SerializeField] GameObject scrapInside;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip creak;

    Quaternion doorClosed;
    bool open;
    bool emptied;

    protected override void Awake()
    {
        base.Awake();
        if (door != null) doorClosed = door.localRotation;
    }

    public override string Prompt
    {
        get
        {
            if (!open) return "try the lock";
            return emptied ? "" : "reach inside";
        }
    }

    public override bool CanInteract => !open || !emptied;

    public override void ApplyState(GameState s)
    {
        open = s.postboxOpen;
        emptied = s.collected[pieceIndex];
        if (door != null) door.localRotation = open ? doorClosed * Quaternion.Euler(doorOpenEuler) : doorClosed;
        if (scrapInside != null) scrapInside.SetActive(open && !emptied);
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!open)
        {
            lockUI.Open(code, OnUnlocked);
            return;
        }

        if (!emptied)
        {
            emptied = true;
            if (scrapInside != null) scrapInside.SetActive(false);
            NewspaperBoard.Instance.TakePiece(pieceIndex);
        }
    }

    void OnUnlocked()
    {
        open = true;
        GameState.Current.postboxOpen = true;
        if (scrapInside != null) scrapInside.SetActive(true);
        StartCoroutine(SwingOpen());
    }

    IEnumerator SwingOpen()
    {
        if (source != null && creak != null) source.PlayOneShot(creak, 0.7f);
        if (door == null) yield break;
        Quaternion target = doorClosed * Quaternion.Euler(doorOpenEuler);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.1f;
            door.localRotation = Quaternion.Slerp(doorClosed, target, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }
    }
}
