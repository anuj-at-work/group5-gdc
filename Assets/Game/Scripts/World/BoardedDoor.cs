using System.Collections;
using UnityEngine;

// Toilet block doors. Nailed shut with a couple of planks, crowbar gets them off.
public class BoardedDoor : Interactable
{
    public enum Which { Entry, Exit }

    [SerializeField] Which which;
    [SerializeField] Transform hinge;
    [SerializeField] float openAngle = -100f;
    [SerializeField] Rigidbody[] planks;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip pry;
    [SerializeField] AudioClip creak;
    [SerializeField] AudioClip rattle;

    bool open;
    bool busy;
    Quaternion closedRot;
    Vector3[] plankPos;
    Quaternion[] plankRot;

    protected override void Awake()
    {
        base.Awake();
        closedRot = hinge.localRotation;
        plankPos = new Vector3[planks.Length];
        plankRot = new Quaternion[planks.Length];
        for (int i = 0; i < planks.Length; i++)
        {
            plankPos[i] = planks[i].transform.localPosition;
            plankRot[i] = planks[i].transform.localRotation;
            planks[i].isKinematic = true;
        }
    }

    bool StateOpen
    {
        get => which == Which.Entry ? GameState.Current.entryDoorOpen : GameState.Current.exitDoorOpen;
        set
        {
            if (which == Which.Entry) GameState.Current.entryDoorOpen = value;
            else GameState.Current.exitDoorOpen = value;
        }
    }

    public override string Prompt
    {
        get
        {
            if (open || busy) return "";
            return GameState.Current.hasCrowbar ? "pry the boards off" : "boarded up";
        }
    }

    public override bool CanInteract => !open && !busy;

    public override void ApplyState(GameState s)
    {
        open = StateOpen;
        hinge.localRotation = open ? closedRot * Quaternion.Euler(0f, openAngle, 0f) : closedRot;
        for (int i = 0; i < planks.Length; i++)
        {
            planks[i].isKinematic = true;
            planks[i].transform.localPosition = plankPos[i];
            planks[i].transform.localRotation = plankRot[i];
            planks[i].gameObject.SetActive(!open);
        }
    }

    public override void Interact(PlayerInteractor player)
    {
        if (!GameState.Current.hasCrowbar)
        {
            if (source != null && rattle != null) source.PlayOneShot(rattle, 0.8f);
            player.Hud.Toast(which == Which.Entry
                ? "Nailed shut. You can't get your fingers under the boards."
                : "Boarded from this side too.");
            return;
        }
        StartCoroutine(PryOpen());
    }

    IEnumerator PryOpen()
    {
        busy = true;
        if (source != null && pry != null) source.PlayOneShot(pry, 1f);
        yield return new WaitForSeconds(0.55f);

        foreach (var p in planks)
        {
            p.isKinematic = false;
            p.AddForce((-transform.forward * 1.5f + Vector3.up * 0.5f + Random.insideUnitSphere * 0.4f), ForceMode.VelocityChange);
            p.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
            yield return new WaitForSeconds(0.25f);
        }

        open = true;
        StateOpen = true;

        if (source != null && creak != null) source.PlayOneShot(creak, 0.8f);
        Quaternion target = closedRot * Quaternion.Euler(0f, openAngle, 0f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.8f;
            hinge.localRotation = Quaternion.Slerp(closedRot, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        busy = false;
    }
}
