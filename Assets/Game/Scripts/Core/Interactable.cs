using UnityEngine;

public abstract class Interactable : LoopObject
{
    // text shown under the dot when you look at it. empty = nothing shown
    public abstract string Prompt { get; }

    public virtual bool CanInteract => true;

    public abstract void Interact(PlayerInteractor player);
}
