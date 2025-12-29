using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    [Header("UI")]
    public GameObject interactText;
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    [Header("Choices")]
    public Transform choicesParent;
    public Button choiceButtonPrefab;

    [Header("Dialogue (SO)")]
    public DialogueNode startNode;

    private DialogueNode currentNode;
    private bool isPlayerNear = false;
    private bool isTalking = false;

    void Start()
    {
        interactText.SetActive(false);
        dialoguePanel.SetActive(false);
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
        dialoguePanel.SetActive(true);

        ShowNode(startNode); // SO 다이얼로그 시작
    }

    void ShowNode(DialogueNode node)
    {
        StopAllCoroutines();
        //이 스크립트에서 시작한 코루틴만 멈춤
        //즉 WaitForEndInput()를 멈추기 위함임. 그러지 않으면 ShowNode()호출되면서 방금 만든 버튼 destroy해버림 
        //(사실 이해 덜된 것같음)

        currentNode = node;

        nameText.text = node.speaker;
        dialogueText.text = node.text;

        // 기존 선택지 버튼 제거
        foreach (Transform t in choicesParent)
            Destroy(t.gameObject);

        // 선택지가 있는 경우
        if (node.choices != null && node.choices.Count > 0)
        {
            foreach (var choice in node.choices)
            {
                Button btn = Instantiate(choiceButtonPrefab, choicesParent);
                btn.GetComponentInChildren<TMP_Text>().text = choice.text;

                btn.onClick.AddListener(() =>
                {
                    ShowNode(choice.nextNode);
                });
            }
        }
        else
        {
            // 선택지가 없으면 스페이스로 종료
            StartCoroutine(WaitForEndInput());
        }
    }

    System.Collections.IEnumerator WaitForEndInput()
    {
        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;

        EndDialogue();
    }

    void EndDialogue()
    {
        isTalking = false;
        dialoguePanel.SetActive(false);
        interactText.SetActive(false);
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
            isPlayerNear = false;
            interactText.SetActive(false);
        }
    }
}
