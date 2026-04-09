using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueSO dialogue;

    [Header("Portrait")]
    [SerializeField] private Sprite npcPortrait;
    [SerializeField] private string npcSpeakerName;

    private bool isPlayerNear = false;
    private bool isTalking = false;

    private DialogueManager dialogueManager;

    void Start()
    {
        dialogueManager = FindFirstObjectByType<DialogueManager>();
    }

    void Update()
    {
        if (isPlayerNear && !isTalking && !UILockManager.IsLocked && Input.GetKeyDown(KeyCode.Space))
        {
            StartDialogue();
        }
    }

    void StartDialogue()
    {
        if (dialogue == null)
            return;

        isTalking = true;
        InteractPromptUI.Hide();

        Dictionary<string, Sprite> portraits = null;
        if (npcPortrait != null && !string.IsNullOrEmpty(npcSpeakerName))
            portraits = new Dictionary<string, Sprite> { { npcSpeakerName, npcPortrait } };

        dialogueManager.OnDialogueEnded += OnDialogueEnded;
        dialogueManager.StartDialogue(dialogue, portraits);
    }

    void OnDialogueEnded(string resultTag)
    {
        dialogueManager.OnDialogueEnded -= OnDialogueEnded;
        StartCoroutine(ResetTalkingNextFrame());
    }

    IEnumerator ResetTalkingNextFrame()
    {
        yield return null;
        isTalking = false;
        if (isPlayerNear)
            InteractPromptUI.Show("*press spacebar to talk*");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (!isTalking)
                InteractPromptUI.Show("*press spacebar to talk*");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (isTalking)
                dialogueManager.EndDialogue();

            isPlayerNear = false;
            InteractPromptUI.Hide();
            isTalking = false;
        }
    }
}
