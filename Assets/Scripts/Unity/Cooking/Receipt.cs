using TMPro;
using UnityEngine;

public class Receipt : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private TextMeshPro orderNumber;

    private Vector3 std = new Vector3(0, 0, 0);
    private Vector3 off = new Vector3(0, -0.25f, 0);
    
    public void Set(MenuSchema menuSchema, bool isDelivery = false)
    {
        int lineIndex = 0;
        foreach (var m in menuSchema.mainMenus)
        {
            GameObject main = Instantiate(linePrefab, transform);
            main.GetComponent<ReceiptLine>().Set(m.ingredientName, 1);
            main.transform.localPosition = std + off * lineIndex++;
        }

        for (int i = 0; i < menuSchema.sideMenus.Count; i++)
        {
            GameObject side = Instantiate(linePrefab, transform);
            side.GetComponent<ReceiptLine>().Set(menuSchema.sideMenus[i].ingredientName, 1);
            side.transform.localPosition = std + off * lineIndex++;
        }

        orderNumber.text = isDelivery
            ? "배달 : " + menuSchema.orderNumber
            : "주문번호 : " + menuSchema.orderNumber;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}