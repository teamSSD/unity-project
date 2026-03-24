using TMPro;
using UnityEngine;

public class Receipt : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private TextMeshPro orderNumber;

    private Vector3 std = new Vector3(0, 0, 0);
    private Vector3 off = new Vector3(0, -0.25f, 0);
    
    public void Set(MenuSchema menuSchema)
    {   
        GameObject main = Instantiate(linePrefab, transform);
        main.GetComponent<ReceiptLine>().Set(menuSchema.mainMenu.ingredientName, 1);
        main.transform.localPosition = std;

        for (int i = 0; i < menuSchema.sideMenus.Count; i++)
        {
            GameObject side = Instantiate(linePrefab, transform);
            side.GetComponent<ReceiptLine>().Set(menuSchema.sideMenus[i].ingredientName, 1);
            side.transform.localPosition = std + off * (i+1);
        }

        orderNumber.text = "二쇰Ц踰덊샇 : " + menuSchema.orderNumber;
    }
}