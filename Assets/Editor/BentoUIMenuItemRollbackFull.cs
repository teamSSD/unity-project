using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BentoUIMenuItemRollbackFull
{
    [MenuItem("Tools/Bento/Rollback Menu Prefab")]
    public static void ExecuteRollback()
    {
        string prefabPath = "Assets/Resources/Prefabs/recipebook/MenuSelection/Menu.prefab";
        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        if (instance == null)
        {
            Debug.LogError("Menu.prefab not found at: " + prefabPath);
            return;
        }

        // Reset Root VerticalLayoutGroup
        var rootVlg = instance.GetComponent<VerticalLayoutGroup>();
        if (rootVlg != null)
        {
            rootVlg.childControlHeight = true;
            rootVlg.childControlWidth = true;
            rootVlg.childForceExpandHeight = true; // Was likely true initially
            rootVlg.childForceExpandWidth = true;
        }

        // Clean up Image container
        Transform containerTransform = instance.transform.Find("Image");
        if (containerTransform != null)
        {
            var le = containerTransform.GetComponent<LayoutElement>();
            if (le != null) Object.DestroyImmediate(le, true);
            
            var vlg = containerTransform.GetComponent<VerticalLayoutGroup>();
            // V3 added a VLG, but actually original Menu Hierarchy had a VLG on Image too according to step 2232:
            // "Image" ... componentTypes:["UnityEngine.RectTransform","UnityEngine.UI.VerticalLayoutGroup"]
            // If it had it originally, don't delete it, just reset
            if (vlg != null)
            {
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = true;
            }
        }

        // Clean up Image/Image
        Transform foodImageTransform = instance.transform.Find("Image/Image"); 
        if (foodImageTransform != null)
        {
            var le = foodImageTransform.GetComponent<LayoutElement>();
            if (le != null) Object.DestroyImmediate(le, true);
        }

        // Clean up Text
        Transform textTransform = instance.transform.Find("Text (TMP)");
        if (textTransform != null)
        {
            var le = textTransform.GetComponent<LayoutElement>();
            if (le != null) Object.DestroyImmediate(le, true);

            // Re-add ContentSizeFitter if missing
            var csf = textTransform.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = textTransform.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        PrefabUtility.UnloadPrefabContents(instance);
        Debug.Log("[BentoSelection] Menu Item Full Rollback applied!");
    }
}
