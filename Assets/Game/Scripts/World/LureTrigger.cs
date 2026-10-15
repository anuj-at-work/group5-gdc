using System.Collections;
using UnityEngine;

// Plays a sound somewhere behind you, the kind you'd want to turn around for.
public class LureTrigger : LoopObject
{
    [SerializeField] StreetPath path;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip[] clips;
    [SerializeField] float metresBehind = 7f;
    [SerializeField] float height = 1.3f;
    [SerializeField] float delay = 0.4f;
    [SerializeField] int firstLoop = 1;
    [Range(0f, 1f)] [SerializeField] float chance = 1f;

    bool firedThisLoop;
    bool silenced;
    int loop = 1;

    public override void ApplyState(GameState s) { silenced = s.BoardComplete; }

    public override void OnLoopStarted(int l)
    {
        loop = l;
        firedThisLoop = false;
    }

    public override void OnBoardComplete() { silenced = true; }

    void OnTriggerEnter(Collider other)
    {
        if (firedThisLoop || silenced || loop < firstLoop) return;
        var player = other.GetComponent<FirstPersonController>();
        if (player == null) return;
        firedThisLoop = true;
        if (Random.value > chance) return;
        StartCoroutine(Play(player.transform));
    }

    IEnumerator Play(Transform player)
    {
        yield return new WaitForSeconds(delay);
        float d = path.Project(player.position);
        Vector3 p = path.Evaluate(Mathf.Max(0f, d - metresBehind));
        source.transform.position = p + Vector3.up * height;
        // cycle through the variations so later loops don't sound identical
        AudioClip clip = clips[(loop - 1) % clips.Length];
        source.PlayOneShot(clip);
    }
}
