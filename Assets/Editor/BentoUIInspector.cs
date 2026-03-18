using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BentoUIInspector
{
    [MenuItem("Tools/Adjust Bento Layout")]
    public static void AdjustLayout()
    {
        string prefabPath = "Assets/Resources/Prefabs/recipebook/MenuSelection/MenuSelection.prefab";
        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        if (instance == null) return;

        Transform background = instance.transform.Find("Background");
        if (background != null)
        {
            var vlg = background.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                // Increase bottom padding and spacing
                vlg.padding.bottom = 40;
                vlg.spacing = 30;
                vlg.childForceExpandWidth = false; // Allow buttons to have their own width
                vlg.childAlignment = TextAnchor.UpperCenter;
            }

            Transform confirmBtn = background.Find("ConfirmButton");
            if (confirmBtn != null)
            {
                var le = confirmBtn.GetComponent<LayoutElement>();
                if (le == null) le = confirmBtn.gameObject.AddComponent<LayoutElement>();
                
                // Give it a fixed size (premium look)
                le.preferredWidth = 400;
                le.preferredHeight = 120;
                
                // Ensure text inside is centered (optional)
            }
        }

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        PrefabUtility.UnloadPrefabContents(instance);
        Debug.Log("[BentoSelection] Confirm Button layout adjusted.");
    }
}
