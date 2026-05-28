#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DeliveryDataExporter : Editor
{
    private const string BaseSOPath = "Assets/Bundles/ScriptableObjects";
    private const string BaseCSVPath = "Assets/Bundles/driveAssets/dataTables";

    [MenuItem("Tools/Export Delivery to SO")]
    public static void ExportAll()
    {
        ExportNpcs();
        ExportDialogue();
        Debug.Log("<color=cyan><b>[Delivery Data Exported: NPC + Dialogue]</b></color>");
    }

    // ── NPC 변환 ─────────────────────────────────────────

    static void ExportNpcs()
    {
        ExportCsvParsable<DeliveryNpcData>(
            $"{BaseCSVPath}/deliveryNPC.csv",
            $"{BaseSOPath}/DeliveryNpcData"
        );
    }

    private static void ExportCsvParsable<T>(string csvAssetPath, string savePath)
        where T : ScriptableObject, CsvParsable
    {
        TextAsset csvFile = AssetDatabase.LoadAssetAtPath<TextAsset>(csvAssetPath);
        if (csvFile == null)
        {
            Debug.LogError($"CSV를 찾을 수 없음: {csvAssetPath}");
            return;
        }

        ClearFolder(savePath);

        string[] lines = csvFile.text.Split(new[] { '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] args = lines[i].Split(',');
            T so = ScriptableObject.CreateInstance<T>();

            try
            {
                so.Init(args);
                string id = typeof(T).GetField("id").GetValue(so).ToString();
                AssetDatabase.CreateAsset(so, $"{savePath}/{id}.asset");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{typeof(T).Name} 파싱 에러 [{i}라인]: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ── Dialogue 변환 (groupId 지원) ─────────────────────

    static void ExportDialogue()
    {
        var dialogRows = ParseDialogCSV();
        var branchGroups = ParseBranchCSV();
        if (dialogRows == null || branchGroups == null) return;

        string dialogueRoot = $"{BaseSOPath}/Dialogue";
        ClearFolder(dialogueRoot);

        // groupId별로 stage 시작점 그룹핑
        var stageStarts = dialogRows.Values
            .Where(r => r.condition != "Serial")
            .ToList();

        var groupedStarts = stageStarts
            .GroupBy(r => r.groupId)
            .Where(g => !string.IsNullOrEmpty(g.Key));

        foreach (var group in groupedStarts)
        {
            string groupId = group.Key;
            string groupPath = $"{dialogueRoot}/{groupId}";
            Directory.CreateDirectory(groupPath);

            var config = ScriptableObject.CreateInstance<DeliveryDialogueConfig>();
            int created = 0;

            foreach (var start in group)
            {
                var so = BuildDialogueSO(start, dialogRows, branchGroups);
                if (so == null) continue;

                so.name = start.condition;
                AssetDatabase.CreateAsset(so, $"{groupPath}/{start.condition}.asset");
                created++;

                // Config에 바인딩
                switch (start.condition)
                {
                    case "FirstMeet":  config.firstMeet  = so; break;
                    case "Normal":     config.normal     = so; break;
                    case "QuestStart": config.questStart = so; break;
                    case "Ordering":   config.ordering   = so; break;
                    case "OrderEnd":   config.orderEnd   = so; break;
                }
            }

            AssetDatabase.CreateAsset(config, $"{groupPath}/Config.asset");
            Debug.Log($"[DeliveryDataExporter] {groupId}: {created} DialogueSO + Config 생성");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static DialogueSO BuildDialogueSO(
        DialogRow start,
        Dictionary<string, DialogRow> allRows,
        Dictionary<string, List<BranchRow>> allBranches)
    {
        var so = ScriptableObject.CreateInstance<DialogueSO>();
        so.entries = new List<DialogueEntry>();

        string currentId = start.id;

        while (!string.IsNullOrEmpty(currentId) && allRows.TryGetValue(currentId, out var row))
        {
            var entry = new DialogueEntry
            {
                speaker = row.npc,
                text = row.contents,
                choices = new List<DialogueChoice>()
            };

            if (!string.IsNullOrEmpty(row.branchId)
                && allBranches.TryGetValue(row.branchId, out var branches))
            {
                string mergePoint = FindMergePoint(branches, allRows);

                foreach (var branch in branches.OrderBy(b => b.order))
                {
                    var choice = new DialogueChoice
                    {
                        label = branch.contents,
                        resultTag = branch.resultTag,
                        responses = BuildResponses(branch.next, mergePoint, allRows)
                    };
                    entry.choices.Add(choice);
                }

                so.entries.Add(entry);
                currentId = mergePoint;
            }
            else
            {
                so.entries.Add(entry);
                currentId = string.IsNullOrEmpty(row.next) ? null : row.next;
            }
        }

        return so;
    }

    static List<DialogueLine> BuildResponses(
        string startId, string mergePoint,
        Dictionary<string, DialogRow> allRows)
    {
        var responses = new List<DialogueLine>();
        string id = startId;

        while (!string.IsNullOrEmpty(id)
               && id != mergePoint
               && allRows.TryGetValue(id, out var row))
        {
            responses.Add(new DialogueLine
            {
                speaker = row.npc,
                text = row.contents
            });
            id = string.IsNullOrEmpty(row.next) ? null : row.next;
        }

        return responses;
    }

    static string FindMergePoint(
        List<BranchRow> branches,
        Dictionary<string, DialogRow> allRows)
    {
        var chainSets = new List<HashSet<string>>();
        foreach (var branch in branches)
        {
            var chain = new HashSet<string>();
            string id = branch.next;
            while (!string.IsNullOrEmpty(id) && allRows.ContainsKey(id))
            {
                chain.Add(id);
                id = allRows[id].next;
                if (string.IsNullOrEmpty(id)) break;
            }
            chainSets.Add(chain);
        }

        if (chainSets.Count < 2) return null;

        string curr = branches[0].next;
        while (!string.IsNullOrEmpty(curr) && allRows.ContainsKey(curr))
        {
            if (chainSets.All(s => s.Contains(curr)))
                return curr;
            curr = allRows[curr].next;
            if (string.IsNullOrEmpty(curr)) break;
        }

        return null;
    }

    // ── CSV 파싱 (groupId 컬럼 지원) ─────────────────────

    class DialogRow
    {
        public string groupId, id, npc, contents, condition, next, branchId;
    }

    class BranchRow
    {
        public string id;
        public int order;
        public string contents, next, resultTag;
    }

    static Dictionary<string, DialogRow> ParseDialogCSV()
    {
        var csv = AssetDatabase.LoadAssetAtPath<TextAsset>($"{BaseCSVPath}/dialog.csv");
        if (csv == null) { Debug.LogError("dialog.csv not found"); return null; }

        var rows = new Dictionary<string, DialogRow>();
        var lines = csv.text.Split(new[] { '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            var t = ParseCsvLine(lines[i]);
            if (t.Count < 7) continue;

            var row = new DialogRow
            {
                groupId   = t[0].Trim(),
                id        = t[1].Trim(),
                npc       = t[2].Trim(),
                contents  = t[3].Trim(),
                condition = t[4].Trim(),
                next      = t[5].Trim(),
                branchId  = t[6].Trim()
            };
            rows[row.id] = row;
        }

        return rows;
    }

    static Dictionary<string, List<BranchRow>> ParseBranchCSV()
    {
        var csv = AssetDatabase.LoadAssetAtPath<TextAsset>($"{BaseCSVPath}/dialogBranch.csv");
        if (csv == null) { Debug.LogError("dialogBranch.csv not found"); return null; }

        var groups = new Dictionary<string, List<BranchRow>>();
        var lines = csv.text.Split(new[] { '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            var t = ParseCsvLine(lines[i]);
            if (t.Count < 5) continue;

            var row = new BranchRow
            {
                id        = t[0].Trim(),
                order     = int.Parse(t[1].Trim()),
                contents  = t[2].Trim(),
                next      = t[3].Trim(),
                resultTag = t[4].Trim()
            };

            if (!groups.ContainsKey(row.id))
                groups[row.id] = new List<BranchRow>();
            groups[row.id].Add(row);
        }

        return groups;
    }

    static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ',' && !inQuotes) { result.Add(current); current = ""; }
            else { current += c; }
        }

        result.Add(current);
        return result;
    }

    // ── 유틸 ─────────────────────────────────────────────

    static void ClearFolder(string path)
    {
        if (Directory.Exists(path)) AssetDatabase.DeleteAsset(path);
        Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
    }
}
#endif
