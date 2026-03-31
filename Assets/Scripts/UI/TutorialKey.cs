using UnityEngine;

public class TutorialKey : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private BoxCollider2D triggerCollider;
    [SerializeField] private bool useColliderTrigger = true; // if false, tutorial must be shown manually via ShowTutorial()

    private bool hasShownThisSession = false;

    void Start()
    {
        tutorialPanel.SetActive(false);

        if (!triggerCollider)
        {
            triggerCollider = GetComponent<BoxCollider2D>();
            if (!triggerCollider)
            {
                Debug.LogWarning("TutorialKey: No BoxCollider2D found for trigger. Please assign one or add a BoxCollider2D component.");
            }
        }
        else if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
            Debug.LogWarning("TutorialKey: Assigned BoxCollider2D was not set as Trigger. It has been changed to Trigger automatically.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && useColliderTrigger && !hasShownThisSession)
        {
            tutorialPanel.SetActive(true);
            hasShownThisSession = true;
            Debug.Log("Triggered tutorial for player.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            tutorialPanel.SetActive(false);
        }
    }

    public void ShowTutorial()
    {
        if (!hasShownThisSession)
        {
            tutorialPanel.SetActive(true);
            hasShownThisSession = true;
        }
    }
}
