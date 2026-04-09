using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("Choices")]
    public Button choiceButtonPrefab;

    private GameObject dialoguePanel;
    private TMP_Text nameText;
    private TMP_Text dialogueText;
    private Transform choicesParent;

    // 초상화
    private GameObject portraitContainer;
    private Image npcPortraitImage;
    private Dictionary<string, Sprite> speakerPortraits;

    private DialogueSO currentDialogue;
    private int currentIndex;
    private string lastResultTag;
    private bool waitingForChoice;
    private bool justStarted;

    // 분기 응답 재생 상태
    private List<DialogueLine> branchResponses;
    private int branchIndex;

    public event System.Action<string> OnDialogueEnded;

    private PlayerMove playerMove;

    void Start()
    {
        // 자체 Canvas 생성 (DontDestroyOnLoad Canvas 충돌 방지)
        var canvasObj = new GameObject("DialogueCanvas");
        canvasObj.transform.SetParent(transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        dialoguePanel = Instantiate(
            Resources.Load<GameObject>("Prefabs/mall/DialoguePanel"),
            canvasObj.transform);

        nameText = dialoguePanel.transform.Find("nameText").GetComponent<TMP_Text>();
        dialogueText = dialoguePanel.transform.Find("dialogueText").GetComponent<TMP_Text>();
        choicesParent = dialoguePanel.transform.Find("choicesPanel");

        // 프리팹에서 초상화 요소 찾기
        var portraitTransform = dialoguePanel.transform.Find("PortraitContainer");
        portraitContainer = portraitTransform.gameObject;
        npcPortraitImage = portraitTransform.Find("NpcPortrait").GetComponent<Image>();

        // 대화창 클릭으로 넘기기
        var panelBtn = dialoguePanel.GetComponent<Button>();
        if (panelBtn == null) panelBtn = dialoguePanel.AddComponent<Button>();
        panelBtn.transition = Selectable.Transition.None;
        panelBtn.onClick.AddListener(OnPanelClicked);

        dialoguePanel.SetActive(false);
        playerMove = FindFirstObjectByType<PlayerMove>();
    }

    private void OnPanelClicked()
    {
        if (!dialoguePanel.activeSelf || waitingForChoice) return;
        AdvanceDialogue();
    }

    public void StartDialogue(DialogueSO dialogue, Dictionary<string, Sprite> portraits = null)
    {
        UILockManager.Lock(UILockManager.Owner.Dialogue);
        justStarted = true;
        currentDialogue = dialogue;
        currentIndex = 0;
        lastResultTag = null;
        branchResponses = null;
        waitingForChoice = false;
        speakerPortraits = portraits;

        portraitContainer.SetActive(portraits != null && portraits.Count > 0);

        dialoguePanel.SetActive(true);
        if (playerMove != null) playerMove.enabled = false;
        ShowCurrentEntry();
    }

    void Update()
    {
        if (justStarted) { justStarted = false; return; }

        if (!dialoguePanel.activeSelf || waitingForChoice)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
        }
    }

    void AdvanceDialogue()
    {
        // 분기 응답 재생 중
        if (branchResponses != null)
        {
            branchIndex++;
            if (branchIndex < branchResponses.Count)
            {
                ShowLine(branchResponses[branchIndex]);
                return;
            }
            // 분기 응답 끝 → 메인으로 복귀
            branchResponses = null;
        }

        // 메인 entries 진행
        currentIndex++;
        if (currentIndex < currentDialogue.entries.Count)
        {
            ShowCurrentEntry();
        }
        else
        {
            EndDialogue();
        }
    }

    void ShowCurrentEntry()
    {
        ShowEntry(currentDialogue.entries[currentIndex]);
    }

    void ShowLine(DialogueLine line)
    {
        dialogueText.text = line.text;

        if (speakerPortraits != null && speakerPortraits.TryGetValue(line.speaker, out var sprite))
        {
            // NPC가 말할 때: 이름 표시, 해당 NPC 초상화 활성화
            nameText.text = line.speaker;
            npcPortraitImage.sprite = sprite;
            npcPortraitImage.color = Color.white;
        }
        else if (speakerPortraits != null && speakerPortraits.Count > 0)
        {
            // 플레이어가 말할 때: 초상화 어둡게
            nameText.text = line.speaker;
            npcPortraitImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        else
        {
            // 초상화 없는 대화
            nameText.text = line.speaker;
        }
    }

    void ShowEntry(DialogueEntry entry)
    {
        ShowLine(entry);

        if (entry.choices != null && entry.choices.Count > 0)
        {
            ShowChoices(entry.choices);
            waitingForChoice = true;
        }
    }

    void ShowChoices(List<DialogueChoice> choices)
    {
        ClearChoices();

        foreach (var choice in choices)
        {
            Button btn = Instantiate(choiceButtonPrefab, choicesParent);
            var labelText = btn.GetComponentInChildren<TMP_Text>();
            labelText.text = choice.label;

            // 삼각형 마커 (별도 TMP 요소)
            var markerObj = new GameObject("Marker", typeof(RectTransform));
            markerObj.transform.SetParent(btn.transform, false);
            markerObj.transform.SetAsFirstSibling();
            var marker = markerObj.AddComponent<TextMeshProUGUI>();
            marker.text = "\u25B6";
            marker.fontSize = labelText.fontSize;
            marker.font = labelText.font;
            marker.color = Color.white;
            marker.alignment = TextAlignmentOptions.MidlineLeft;
            var markerLE = markerObj.AddComponent<LayoutElement>();
            markerLE.preferredWidth = 30;

            // 레이블 유동 너비
            labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // 가로 레이아웃
            var hlg = btn.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 5;
            hlg.padding = new RectOffset(10, 10, 0, 0);

            var captured = choice;
            btn.onClick.AddListener(() => OnChoiceSelected(captured));
        }
    }

    void OnChoiceSelected(DialogueChoice choice)
    {
        ClearChoices();
        waitingForChoice = false;

        if (!string.IsNullOrEmpty(choice.resultTag))
            lastResultTag = choice.resultTag;

        if (choice.responses != null && choice.responses.Count > 0)
        {
            branchResponses = choice.responses;
            branchIndex = 0;
            ShowLine(branchResponses[0]);
        }
        else
        {
            AdvanceDialogue();
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
        dialoguePanel.SetActive(false);
        portraitContainer.SetActive(false);
        ClearChoices();
        if (playerMove != null) playerMove.enabled = true;

        var resultTag = lastResultTag;
        currentDialogue = null;
        lastResultTag = null;
        branchResponses = null;
        waitingForChoice = false;

        UILockManager.Unlock(UILockManager.Owner.Dialogue);
        OnDialogueEnded?.Invoke(resultTag);
    }
}
