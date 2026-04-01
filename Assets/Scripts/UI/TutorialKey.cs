using UnityEngine;
using System.Collections.Generic;

public class TutorialKey : MonoBehaviour
{
    [SerializeField] private List<GameObject> tutorialPanels = new List<GameObject>();
    [SerializeField] private BoxCollider2D triggerCollider;
    [SerializeField] private bool useColliderTrigger = true; // if false, tutorial must be shown manually via ShowTutorial()

    private bool hasShownThisSession = false;

    void Start()
    {
        SetTutorialPanelsActive(false);

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
            SetTutorialPanelsActive(true);
            hasShownThisSession = true;
            Debug.Log("Triggered tutorial for player.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SetTutorialPanelsActive(false);
        }
    }

    public void ShowTutorial()
    {
        if (!hasShownThisSession)
        {
            SetTutorialPanelsActive(true);
            hasShownThisSession = true;
        }
    }

    private void SetTutorialPanelsActive(bool isActive)
    {
        if (tutorialPanels == null || tutorialPanels.Count == 0)
        {
            return;
        }

        for (int i = 0; i < tutorialPanels.Count; i++)
        {
            if (tutorialPanels[i] != null)
            {
                tutorialPanels[i].SetActive(isActive);
            }
        }
    }
}
