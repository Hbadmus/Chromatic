using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [SerializeField] private DialogueLine[] lines;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || !other.CompareTag("Player")) return;
        if (DialogueManager.Instance == null) return;

        hasTriggered = true;
        DialogueManager.Instance.StartDialogue(lines);
    }
}
