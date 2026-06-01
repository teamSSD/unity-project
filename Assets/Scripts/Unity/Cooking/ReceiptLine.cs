using TMPro;
using UnityEngine;

public class ReceiptLine : MonoBehaviour
{
    [SerializeField] private new TextMeshPro name;
    [SerializeField] private TextMeshPro count;
    
    public void Set(string name, int count)
    {
        this.name.text = name;
        this.count.text = count.ToString();
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}