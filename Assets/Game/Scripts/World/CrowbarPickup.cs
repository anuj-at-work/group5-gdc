using UnityEngine;

public class CrowbarPickup : Interactable
{
    [SerializeField] AudioClip pickupClip;

    public override string Prompt => "take the crowbar";

    public override void ApplyState(GameState s)
    {
        gameObject.SetActive(!s.hasCrowbar);
    }

    public override void Interact(PlayerInteractor player)
    {
        GameState.Current.hasCrowbar = true;
        if (pickupClip != null)
            AudioSource.PlayClipAtPoint(pickupClip, transform.position, 0.8f);
        player.Hud.Toast("A crowbar. Still warm, like someone just put it down.");
        gameObject.SetActive(false);
    }
}
