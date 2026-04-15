using UnityEngine;

public class BossDeathTutorialBridge : MonoBehaviour
{
    [SerializeField] private BaseBoss boss;
    [SerializeField] private bool triggerOnlyOnce = true;
    
    [Tooltip("The tutorial key to show when the boss dies. If not set, it will try to find one on the same GameObject.")]
    [SerializeField] private TutorialKey tutorialKey;

    private Health bossHealth;
    private bool hasTriggered;

    private void Awake()
    {
        if (boss != null)
        {
            bossHealth = boss.GetComponent<Health>();
        }
        if (tutorialKey == null)
        {
            tutorialKey = GetComponent<TutorialKey>();
        }
    }

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDied += HandleBossDied;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDied -= HandleBossDied;
        }
    }

    private void HandleBossDied()
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (tutorialKey != null)
        {
            tutorialKey.ShowTutorial();
            hasTriggered = true;
        }
    }
}
