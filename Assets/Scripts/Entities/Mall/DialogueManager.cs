using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    [Header("Choices")]
    public Transform choicesParent;
    public Button choiceButtonPrefab;

    Dictionary<string, DialogueRow> dialogMap;
    Dictionary<string, List<BranchRow>> branchMap;

    //private bool waitingForInput;
    private string pendingNextId;
    private bool waitingForChoice;

    string currentId;

    void Awake()
    {
        dialogMap = new Dictionary<string, DialogueRow>();
        branchMap = new Dictionary<string, List<BranchRow>>();

        foreach (var d in CSVLoader.LoadDialog("driveAssets/dataTables/dialog"))
            dialogMap[d.id] = d;

        foreach (var b in CSVLoader.LoadBranch("driveAssets/dataTables/dialogBranch"))
        {
            if (!branchMap.ContainsKey(b.id))
                branchMap[b.id] = new List<BranchRow>();

            branchMap[b.id].Add(b);
        }
        Debug.Log("DialogueManager awoke!");

    }

    void Start()
    {
        dialoguePanel.SetActive(false);
    }
    
    public void StartDialogue(string startId)
    {
        dialoguePanel.SetActive(true);
        waitingForChoice = false;
        ShowDialogue(startId);
    }
    void Update()
    {
        if (!dialoguePanel.activeSelf)
            return;

        //if (waitingForInput && Input.GetKeyDown(KeyCode.Space))
        if (!waitingForChoice && Input.GetKeyDown(KeyCode.Space))
        {
            //waitingForInput = false;

            if (!string.IsNullOrEmpty(pendingNextId))
            {
                ShowDialogue(pendingNextId);
            }
            else {
                EndDialogue();
            }
        }
    }

    void ShowDialogue(string id)
    {
        currentId = id;
        var d = dialogMap[id];

        // UI 출력
        nameText.text = d.npc;
        dialogueText.text = d.contents;

        // 분기 있으면 선택지 표시
        if (!string.IsNullOrEmpty(d.branchId))
        {
            ShowBranch(d.branchId);
            //waitingForInput = false;
            pendingNextId = null;
            waitingForChoice = true;
            return;
        }

        // 다음 대사를 "저장만" 해둠
        pendingNextId = d.next;
        //waitingForInput = !string.IsNullOrEmpty(d.next);
    }

    void ShowBranch(string branchId)
    {
        var list = branchMap[branchId];
        list.Sort((a, b) => a.order.CompareTo(b.order));

        foreach (var b in list)
        {
            Button btn = Instantiate(choiceButtonPrefab, choicesParent);
            btn.GetComponentInChildren<TMP_Text>().text = b.contents;

            btn.onClick.AddListener(() =>
            {
                ClearChoices();
                waitingForChoice = false;
                ShowDialogue(b.next);
            });
        }
    }
    void ClearChoices()
    {
        foreach (Transform t in choicesParent)
        {
            Destroy(t.gameObject);
        }
    }
    public void EndDialogue()
    {
        Debug.Log("대화종료");
        dialoguePanel.SetActive(false);
        ClearChoices();

        //waitingForInput = false;
        pendingNextId = null;
        currentId = null;
    }
}
