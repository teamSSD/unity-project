using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("Choices")]
    public Button choiceButtonPrefab;
    [SerializeField] private GameObject dialoguePanelPrefab;

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

    [Header("SFX")]
    [SerializeField] private AudioClip npcBlipSfx;

    private bool _isTyping;
    private Coroutine _typingCoroutine;
    private string _currentLineText;

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

        dialoguePanel = Instantiate(dialoguePanelPrefab, canvasObj.transform);

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
        if (_isTyping) { SkipTyping(); return; }
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
        UISoundManager.Instance?.PlayUIBook();
        GlobalButtonSfxManager.Instance?.RegisterButtons(dialoguePanel.transform);
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
            if (_isTyping) SkipTyping();
            else AdvanceDialogue();
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
        ApplyPortrait(line);

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeText(line.text));
    }

    void ApplyPortrait(DialogueLine line)
    {
        if (speakerPortraits != null && speakerPortraits.TryGetValue(line.speaker, out var sprite))
        {
            nameText.text = line.speaker;
            npcPortraitImage.sprite = sprite;
            npcPortraitImage.color = Color.white;
        }
        else if (speakerPortraits != null && speakerPortraits.Count > 0)
        {
            nameText.text = line.speaker;
            npcPortraitImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        else
        {
            nameText.text = line.speaker;
        }
    }

    private System.Collections.IEnumerator TypeText(string text)
    {
        _currentLineText = text;
        _isTyping = true;
        dialogueText.text = "";
        int charIndex = 0;
        foreach (char c in text)
        {
            dialogueText.text += c;
            if (!char.IsWhiteSpace(c))
            {
                charIndex++;
                if (charIndex % 3 == 0)
                    SoundManager.Instance?.Play2DSFX(npcBlipSfx, 0.7f);
            }
            yield return new WaitForSeconds(0.04f);
        }
        _isTyping = false;
        _typingCoroutine = null;
    }

    private void SkipTyping()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;
        dialogueText.text = _currentLineText ?? "";
        _isTyping = false;
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

        // 1프레임 지연 Unlock — 같은 프레임에서 Space가 재진입하는 것을 방지
        StartCoroutine(DelayedUnlock(resultTag));
    }

    private System.Collections.IEnumerator DelayedUnlock(string resultTag)
    {
        yield return null;
        UILockManager.Unlock(UILockManager.Owner.Dialogue);
        OnDialogueEnded?.Invoke(resultTag);
    }
}
