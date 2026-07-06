using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DialoguePanel.prefab 하위 참조 컨테이너. DialogueManager가 transform.Find 대신 즉시 접근.
/// </summary>
public class DialoguePanelRefs : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Transform choicesParent;
    [SerializeField] private GameObject portraitContainer;
    [SerializeField] private Image npcPortraitImage;

    public TMP_Text NameText            => nameText;
    public TMP_Text DialogueText        => dialogueText;
    public Transform ChoicesParent      => choicesParent;
    public GameObject PortraitContainer => portraitContainer;
    public Image NpcPortraitImage       => npcPortraitImage;

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
