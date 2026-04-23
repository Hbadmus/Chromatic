using UnityEngine;

/// <summary>
/// Place one of these in each scene. Sets which level music pair to use
/// and starts ambient sound. BossRoomTrigger handles switching to boss music.
/// </summary>
public class LevelMusicStarter : MonoBehaviour
{
    [Tooltip("0 = Level 1, 1 = Level 2, etc. Must match SoundManager array indices")]
    [SerializeField] private int levelIndex = 0;
    [SerializeField] private AudioClip ambientClip;

    private void Start()
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.SetCurrentLevel(levelIndex);
        SoundManager.Instance.PlayDefaultMusic();

        if (ambientClip != null)
            SoundManager.Instance.PlayAmbient(ambientClip);
    }
}
