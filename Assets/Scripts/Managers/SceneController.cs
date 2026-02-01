using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneController : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null) return;
        
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ReloadCurrentScene();
        }
    }

    public void ReloadCurrentScene()
    {
        if (PlayerHealth.Instance != null)
        {
            Destroy(PlayerHealth.Instance.gameObject);
        }
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
