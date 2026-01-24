using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CSVLoader
{

    public static List<DialogueRow> LoadDialog(string fileName)
    {
        var list = new List<DialogueRow>();
        var lines = Resources.Load<TextAsset>(fileName).text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var t = ParseCsvLine(lines[i]);

            list.Add(new DialogueRow
            {
                id = t[0].Trim(),
                npc = t[1].Trim(),
                contents = t[2].Trim(),
                condition = t[3].Trim(),
                next = t[4].Trim(),
                branchId = t[5].Trim()
            });
        }
        return list;
    }

    public static List<BranchRow> LoadBranch(string fileName)
    {
        var list = new List<BranchRow>();
        var lines = Resources.Load<TextAsset>(fileName).text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var t = ParseCsvLine(lines[i]);

            list.Add(new BranchRow
            {
                id = t[0].Trim(),
                order = int.Parse(t[1].Trim()),
                contents = t[2].Trim(),
                next = t[3].Trim()
            });
        }
        return list;
    }

    static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result;
    }

}
