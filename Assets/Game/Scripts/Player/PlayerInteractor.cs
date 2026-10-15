using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] float reach = 2.3f;
    [SerializeField] LayerMask mask = ~0;
    [SerializeField] Hud hud;

    public FirstPersonController Controller { get; private set; }
    public bool Blocked { get; set; }

    Interactable current;
    float cooldownUntil;

    // stops the key that closed a menu from also using whatever you're looking at
    public void Cooldown(float seconds) => cooldownUntil = Time.unscaledTime + seconds;

    void Awake()
    {
        Controller = GetComponent<FirstPersonController>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (Blocked || Controller.LookLocked || Time.unscaledTime < cooldownUntil)
        {
            SetTarget(null);
            return;
        }

        Interactable hit = null;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.SphereCast(ray, 0.06f, out RaycastHit info, reach, mask, QueryTriggerInteraction.Ignore))
        {
            hit = info.collider.GetComponentInParent<Interactable>();
            if (hit != null && (!hit.isActiveAndEnabled || !hit.CanInteract)) hit = null;
        }
        SetTarget(hit);

        if (current != null && PressedUse())
            current.Interact(this);
    }

    static bool PressedUse()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        return (kb != null && kb.eKey.wasPressedThisFrame) ||
               (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked);
    }

    void SetTarget(Interactable i)
    {
        current = i;
        if (hud != null) hud.SetFocus(i != null ? i.Prompt : null);
    }

    public Hud Hud => hud;
}
