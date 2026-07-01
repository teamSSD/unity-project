using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class _TempAddStopwatchDialSubSprites
{
    private const string Path = "Assets/Bundles/driveAssets/art/ui/general/ui_stopwatch.png";

    // Detected via alpha scan (see prior Python analysis)
    private static readonly SpriteRect[] NewRects =
    {
        new SpriteRect
        {
            name = "ui_stopwatch_0_dial",
            rect = new Rect(109, 398, 528, 479),
            pivot = new Vector2(0.5f, 0.5f),
            alignment = SpriteAlignment.Center,
            border = Vector4.zero,
        },
        new SpriteRect
        {
            name = "ui_stopwatch_1_dial",
            rect = new Rect(821, 386, 526, 479),
            pivot = new Vector2(0.5f, 0.5f),
            alignment = SpriteAlignment.Center,
            border = Vector4.zero,
        },
    };

    [MenuItem("Tools/Temp/Add Stopwatch Dial SubSprites")]
    public static void Run()
    {
        var importer = AssetImporter.GetAtPath(Path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[AddDial] TextureImporter not found at {Path}");
            return;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        var current = new List<SpriteRect>(provider.GetSpriteRects());
        Debug.Log($"[AddDial] existing sub-sprites: {current.Count}");
        foreach (var r in current) Debug.Log($"[AddDial]   - {r.name}  rect={r.rect}  pivot={r.pivot}");

        foreach (var target in NewRects)
        {
            bool exists = current.Exists(r => r.name == target.name);
            if (exists)
            {
                Debug.Log($"[AddDial] '{target.name}' already exists — skipped");
                continue;
            }
            target.spriteID = GUID.Generate();
            current.Add(target);
            Debug.Log($"[AddDial] added '{target.name}' rect={target.rect} spriteID={target.spriteID}");
        }

        provider.SetSpriteRects(current.ToArray());

        // Update nameFileIdTable so sprite references by name resolve correctly
        var nameFileIdProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIdProvider != null)
        {
            var pairs = new List<SpriteNameFileIdPair>(nameFileIdProvider.GetNameFileIdPairs());
            foreach (var target in NewRects)
            {
                if (!pairs.Exists(p => p.name == target.name))
                {
                    pairs.Add(new SpriteNameFileIdPair(target.name, target.spriteID));
                }
            }
            nameFileIdProvider.SetNameFileIdPairs(pairs);
        }

        provider.Apply();

        importer.SaveAndReimport();
        AssetDatabase.Refresh();
        Debug.Log("[AddDial] DONE");
    }
}
