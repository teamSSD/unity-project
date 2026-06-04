using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// npcCasualDialogue.csv에서 NPC 일상 대사를 로드하고,
/// 날씨 조건에 맞는 대사를 랜덤으로 제공.
/// </summary>
public static class CasualDialogueProvider
{
    private struct Line
    {
        public string condition; // "Any", "Good", "Bad"
        public string speaker;
        public string text;
    }

    private static Dictionary<string, List<Line>> npcLines;

    public static void Load()
    {
        npcLines = new Dictionary<string, List<Line>>();

        var csv = CatalogProvider.Csvs?.npcCasualDialogue;
        if (csv == null) { Debug.LogError("[CasualDialogue] CSV not in CatalogProvider"); return; }

        var lines = csv.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i]);
            if (fields.Count < 4) continue;

            string npcId = fields[0].Trim();
            var line = new Line
            {
                condition = fields[1].Trim(),
                speaker = fields[2].Trim(),
                text = fields[3].Trim()
            };

            if (!npcLines.ContainsKey(npcId))
                npcLines[npcId] = new List<Line>();
            npcLines[npcId].Add(line);
        }

        Debug.Log($"[CasualDialogue] Loaded {npcLines.Count} NPCs");
    }

    /// <summary>
    /// NPC ID와 현재 날씨에 맞는 대사를 랜덤으로 하나 반환.
    /// DialogueSO를 런타임에 생성.
    /// </summary>
    public static DialogueSO GetRandomDialogue(string npcId)
    {
        if (npcLines == null) Load();
        if (!npcLines.TryGetValue(npcId, out var lines)) return null;

        bool badWeather = GameSessionRoot.Instance?.Weather?.IsBadWeather ?? false;

        // 날씨 조건 필터: Any는 항상 포함, Good/Bad는 날씨에 따라
        var candidates = lines.Where(l =>
            l.condition == "Any" ||
            (badWeather && l.condition == "Bad") ||
            (!badWeather && l.condition == "Good")
        ).ToList();

        if (candidates.Count == 0) return null;

        var picked = candidates[GameRandom.Range(GameRandom.Variable, 0, candidates.Count)];

        var so = ScriptableObject.CreateInstance<DialogueSO>();
        so.entries = new List<DialogueEntry>
        {
            new DialogueEntry
            {
                speaker = picked.speaker,
                text = picked.text,
                choices = new List<DialogueChoice>()
            }
        };
        return so;
    }

    private static List<string> ParseCsvLine(string line)
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
}
