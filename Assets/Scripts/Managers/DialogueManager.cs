using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite portrait;
    [TextArea(2, 4)]
    public string text;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsActive { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueBodyText;
    [SerializeField] private GameObject continuePrompt;

    private DialogueLine[] lines;
    private int lineIndex;
    private Action onEnd;
    private bool canAdvance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!IsActive || !canAdvance) return;

        if (Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            AdvanceLine();
        }
    }

    public void StartDialogue(DialogueLine[] dialogueLines, Action callback = null)
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        lines = dialogueLines;
        lineIndex = 0;
        onEnd = callback;
        IsActive = true;

        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        canAdvance = false;

        DialogueLine line = lines[lineIndex];
        speakerNameText.text = line.speakerName;
        dialogueBodyText.text = line.text;

        if (portraitImage != null)
        {
            portraitImage.enabled = line.portrait != null;
            portraitImage.sprite = line.portrait;
        }

        bool isLast = lineIndex >= lines.Length - 1;
        if (continuePrompt != null)
            continuePrompt.SetActive(!isLast);

        StartCoroutine(AllowAdvanceNextFrame());
    }

    private IEnumerator AllowAdvanceNextFrame()
    {
        yield return null;
        canAdvance = true;
    }

    private void AdvanceLine()
    {
        lineIndex++;
        if (lineIndex >= lines.Length)
            EndDialogue();
        else
            ShowCurrentLine();
    }

    public void EndDialogue()
    {
        IsActive = false;
        dialoguePanel.SetActive(false);
        lines = null;
        Action callback = onEnd;
        onEnd = null;
        callback?.Invoke();
    }
}
