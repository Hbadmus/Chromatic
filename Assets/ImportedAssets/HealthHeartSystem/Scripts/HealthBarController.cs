/*
 *  Author: ariel oliveira [o.arielg@gmail.com]
 *  Modified by: Binxuan Yan
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarController : MonoBehaviour
{
    private GameObject[] heartContainers;
    private Image[] heartFills;

    public Transform heartsParent;
    public GameObject heartContainerPrefab;
    public GameObject newHeartContainerPrefab;
    private float transitionDuration = 0.5f;

    private void Start()
    {
        // Should I use lists? Maybe :)
        heartContainers = new GameObject[(int)PlayerHealth.Instance.MaxHealth];
        heartFills = new Image[(int)PlayerHealth.Instance.MaxHealth];

        PlayerHealth.Instance.OnHealthChanged += UpdateHeartsHUD;
        InstantiateHeartContainers();
        UpdateHeartsHUD();
    }

    public void UpdateHeartsHUD()
    {
        SetHeartContainers();
        SetFilledHearts();
    }

    void SetHeartContainers()
    {
        for (int i = 0; i < heartContainers.Length; i++)
        {
            if (i < PlayerHealth.Instance.MaxHealth)
            {
                heartContainers[i].SetActive(true);
            }
            else
            {
                heartContainers[i].SetActive(false);
            }
        }
    }

    void SetFilledHearts()
    {
        for (int i = 0; i < heartFills.Length; i++)
        {
            if (i < PlayerHealth.Instance.CurrentHealth)
            {
                heartFills[i].fillAmount = 1;
            }
            else
            {
                heartFills[i].fillAmount = 0;
            }
        }

        if (PlayerHealth.Instance.CurrentHealth % 1 != 0)
        {
            int lastPos = Mathf.FloorToInt(PlayerHealth.Instance.CurrentHealth);
            heartFills[lastPos].fillAmount = PlayerHealth.Instance.CurrentHealth % 1;
        }
    }

    void InstantiateHeartContainers()
    {
        for (int i = 0; i < PlayerHealth.Instance.MaxHealth; i++)
        {
            GameObject temp = Instantiate(heartContainerPrefab);
            temp.transform.SetParent(heartsParent, false);
            heartContainers[i] = temp;
            heartFills[i] = temp.transform.Find("HeartFill").GetComponent<Image>();
        }
    }

    /// <summary>
    /// Dynamically replace the heart container prefab and regenerate all hearts with smooth transition
    /// </summary>
    public void RegenerateHearts()
    {
        if (newHeartContainerPrefab == null)
        {
            Debug.LogWarning("HealthBarController: newHeartContainerPrefab is null!");
            return;
        }

        StartCoroutine(RegenerateHeartsWithTransition());
    }

    private IEnumerator RegenerateHeartsWithTransition()
    {
        // Fade out old hearts
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            foreach (GameObject heart in heartContainers)
            {
                if (heart != null)
                {
                    CanvasGroup canvasGroup = heart.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                        canvasGroup = heart.AddComponent<CanvasGroup>();

                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                }
            }
            yield return null;
        }

        // Destroy all old hearts
        foreach (GameObject heart in heartContainers)
        {
            if (heart != null)
                Destroy(heart);
        }

        // Update prefab and recreate
        heartContainerPrefab = newHeartContainerPrefab;
        heartContainers = new GameObject[(int)PlayerHealth.Instance.MaxHealth];
        heartFills = new Image[(int)PlayerHealth.Instance.MaxHealth];
        InstantiateHeartContainers();
        UpdateHeartsHUD();

        // Fade in new hearts
        elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            foreach (GameObject heart in heartContainers)
            {
                if (heart != null)
                {
                    CanvasGroup canvasGroup = heart.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                        canvasGroup = heart.AddComponent<CanvasGroup>();

                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                }
            }
            yield return null;
        }

        // Ensure final alpha is 1
        foreach (GameObject heart in heartContainers)
        {
            if (heart != null)
            {
                CanvasGroup canvasGroup = heart.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f;
            }
        }
    }
}
