using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public partial class DialogueManager : MonoBehaviour
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
    private CancellationTokenSource _typingCts;
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
        SoundManager.Instance?.PlayUIBook();
        SoundManager.Instance?.RegisterButtons(dialoguePanel.transform);
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

        _typingCts?.Cancel();
        _typingCts?.Dispose();
        _typingCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        TypeTextAsync(line.text, _typingCts.Token).Forget();
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

    private async UniTaskVoid TypeTextAsync(string text, CancellationToken ct)
    {
        _currentLineText = text;
        _isTyping = true;
        dialogueText.text = "";
        int charIndex = 0;
        try
        {
            foreach (char c in text)
            {
                dialogueText.text += c;
                if (!char.IsWhiteSpace(c))
                {
                    charIndex++;
                    if (charIndex % 3 == 0)
                        SoundManager.Instance?.Play2DSFX(npcBlipSfx, 0.7f);
                }
                await UniTask.Delay(TimeSpan.FromSeconds(0.04f), cancellationToken: ct);
            }
        }
        catch (OperationCanceledException) { /* Skip */ }
        finally
        {
            _isTyping = false;
        }
    }

    private void SkipTyping()
    {
        _typingCts?.Cancel();
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
        DelayedUnlockAsync(resultTag).Forget();
    }

    private async UniTaskVoid DelayedUnlockAsync(string resultTag)
    {
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        UILockManager.Unlock(UILockManager.Owner.Dialogue);
        OnDialogueEnded?.Invoke(resultTag);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
