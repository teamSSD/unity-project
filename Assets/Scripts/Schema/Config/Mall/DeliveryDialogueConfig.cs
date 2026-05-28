using UnityEngine;

[CreateAssetMenu(menuName = "Config/DeliveryDialogueConfig")]
public class DeliveryDialogueConfig : ScriptableObject
{
    public DialogueSO firstMeet;
    public DialogueSO normal;
    public DialogueSO questStart;
    public DialogueSO ordering;
    public DialogueSO orderEnd;
}
