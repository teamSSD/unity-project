using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;

public static class WireInventoryButton
{
    [MenuItem("Tools/Setup/Wire Inventory Bookmark Button")]
    public static void Run()
    {
        string path = "Assets/Resources/Prefabs/recipebook/legacy/RecipeBook.prefab";
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var root = scope.prefabContentsRoot.transform;
            var rbm = scope.prefabContentsRoot.GetComponent<RecipeBookManager>();
            if (rbm == null) { Debug.LogError("[WireInventory] RecipeBookManager not found"); return; }

            var bmTransform = root.Find("Page/Inventory/Inventory_BookMark");
            if (bmTransform == null) { Debug.LogError("[WireInventory] Inventory_BookMark not found"); return; }

            var btn = bmTransform.GetComponent<Button>();
            if (btn == null) { Debug.LogError("[WireInventory] Button not found"); return; }

            btn.onClick.RemoveAllListeners();
            UnityEventTools.AddVoidPersistentListener(btn.onClick, rbm.OpenInventory);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[WireInventory] Done.");
    }
}
