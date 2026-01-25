using UnityEngine;

public class BossHealth : EnemyHealth
{
    [SerializeField] private GameObject[] redEnvironment;

    protected override void Die()
    {
        // Change all red environment objects to red
        foreach (GameObject obj in redEnvironment)
        {
            ColorTransition transition = obj.GetComponent<ColorTransition>();
            if (transition != null)
            {
                transition.StartTransition();
            }
        }

        base.Die();
    }
}