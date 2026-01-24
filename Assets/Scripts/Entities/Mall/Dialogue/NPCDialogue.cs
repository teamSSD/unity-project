using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    [Header("UI")]
    public GameObject interactText;

    private bool isPlayerNear = false;
    private bool isTalking = false;

    private DialogueManager dialogueManager;


    void Start()
    {
        dialogueManager = FindFirstObjectByType<DialogueManager>();
        interactText.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNear && !isTalking && Input.GetKeyDown(KeyCode.Space))
        {
            StartDialogue();
        }
    }
    
    void StartDialogue()
    {
        isTalking = true;
        interactText.SetActive(false);
        dialogueManager.StartDialogue("D001"); //@@ 완전 임시
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (!isTalking)
                interactText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            dialogueManager.EndDialogue(); 

            isPlayerNear = false;
            interactText.SetActive(false);
            isTalking = false;
        }
    }
}
