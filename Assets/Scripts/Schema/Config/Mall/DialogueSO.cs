using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/DialogueSO")]
public class DialogueSO : ScriptableObject
{
    public List<DialogueEntry> entries;
}

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 4)] public string text;
}

[System.Serializable]
public class DialogueEntry : DialogueLine
{
    public List<DialogueChoice> choices;
}

[System.Serializable]
public class DialogueChoice
{
    public string label;
    public List<DialogueLine> responses;
    public string resultTag;
}
