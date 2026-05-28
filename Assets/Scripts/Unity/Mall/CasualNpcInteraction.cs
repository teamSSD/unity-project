using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일상 대화 NPC 인터랙션.
/// DeliveryNpcInteraction이 Space키/프롬프트를 관리하고,
/// 이 컴포넌트는 Interact() 호출 시 랜덤 대사를 재생.
/// </summary>
public class CasualNpcInteraction : MonoBehaviour, INpcInteraction
{
    private string npcId;
    private string characterName;
    private Sprite portrait;
    private DialogueManager dialogueManager;

    public void Init(string npcId, string characterName, Sprite portrait)
    {
        this.npcId = npcId;
        this.characterName = characterName;
        this.portrait = portrait;
    }

    private void Start()
    {
        dialogueManager = Object.FindFirstObjectByType<DialogueManager>();
    }

    public void Interact()
    {
        var dialogue = CasualDialogueProvider.GetRandomDialogue(npcId);
        if (dialogue == null || dialogueManager == null) return;

        Dictionary<string, Sprite> portraits = null;
        if (portrait != null)
            portraits = new Dictionary<string, Sprite> { { characterName, portrait } };

        dialogueManager.StartDialogue(dialogue, portraits);
    }
}
