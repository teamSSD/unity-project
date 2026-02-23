using UnityEngine;
using UnityEditor;

/// <summary>
/// ActionToggle의 phase 필드를 GameObject 경로에 따라 자동으로 수정하는 Editor 스크립트
/// </summary>
public class FixActionTogglePhases : EditorWindow
{
    [MenuItem("Tools/Fix ActionToggle Phases")]
    public static void FixPhases()
    {
        var actionToggles = FindObjectsOfType<ActionToggle>(true);
        int fixedCount = 0;

        foreach (var toggle in actionToggles)
        {
            string path = GetGameObjectPath(toggle.gameObject);
            PhaseType correctPhase = DeterminePhaseFromPath(path);

            if (toggle.phase != correctPhase)
            {
                Undo.RecordObject(toggle, "Fix ActionToggle Phase");
                toggle.phase = correctPhase;
                EditorUtility.SetDirty(toggle);
                fixedCount++;

                Debug.Log($"[FixPhases] {toggle.name}: {toggle.phase} → {correctPhase} (path: {path})");
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"[FixPhases] Fixed {fixedCount} ActionToggle phases");
            EditorUtility.DisplayDialog("Fix Complete", $"Fixed {fixedCount} ActionToggle phases", "OK");
        }
        else
        {
            Debug.Log("[FixPhases] All ActionToggle phases are already correct");
            EditorUtility.DisplayDialog("Fix Complete", "All ActionToggle phases are already correct", "OK");
        }
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static PhaseType DeterminePhaseFromPath(string path)
    {
        if (path.Contains("/Morning/"))
            return PhaseType.Morning;
        else if (path.Contains("/Afternoon/") || path.Contains("/Lunch/"))
            return PhaseType.Afternoon;
        else if (path.Contains("/Evening/"))
            return PhaseType.Evening;
        else if (path.Contains("/Night/"))
            return PhaseType.Night;
        else if (path.Contains("/PrefabTime/") || path.Contains("/Preparation/"))
            return PhaseType.Preparation;
        else
        {
            // 기본값은 Preparation
            Debug.LogWarning($"[FixPhases] Could not determine phase from path: {path}, using Preparation");
            return PhaseType.Preparation;
        }
    }
}
