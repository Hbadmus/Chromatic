using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialKey : MonoBehaviour
{
    [SerializeField] private List<GameObject> tutorialPanels = new List<GameObject>();
    [SerializeField] private BoxCollider2D triggerCollider;
    [SerializeField] private bool useColliderTrigger = true;
    [SerializeField] private float minDisplayTime = 20f;

    private bool hasShownThisSession = false;
    private bool playerInside = false;
    private bool minTimeElapsed = false;

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
        if (!other.CompareTag("Player") || !useColliderTrigger || hasShownThisSession) return;

        playerInside = true;
        hasShownThisSession = true;
        SetTutorialPanelsActive(true);
        StartCoroutine(MinDisplayTimer());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        if (minTimeElapsed)
            SetTutorialPanelsActive(false);
    }

    private IEnumerator MinDisplayTimer()
    {
        minTimeElapsed = false;
        yield return new WaitForSeconds(minDisplayTime);
        minTimeElapsed = true;

        if (!playerInside)
            SetTutorialPanelsActive(false);
    }

    public void ShowTutorial()
    {
        if (!hasShownThisSession)
        {
            hasShownThisSession = true;
            playerInside = true;
            SetTutorialPanelsActive(true);
            StartCoroutine(MinDisplayTimer());
        }
    }

    private void SetTutorialPanelsActive(bool isActive)
    {
        if (tutorialPanels == null || tutorialPanels.Count == 0) return;

        for (int i = 0; i < tutorialPanels.Count; i++)
        {
            if (tutorialPanels[i] != null)
                tutorialPanels[i].SetActive(isActive);
        }
    }
}
