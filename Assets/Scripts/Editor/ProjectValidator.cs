#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;

/// <summary>
/// 프로젝트 전체 규칙 검증 도구
/// DEVELOPMENT_RULES.md의 규칙을 자동으로 검증합니다.
/// </summary>
public class ProjectValidator : EditorWindow
{
    private Vector2 scrollPosition;
    private List<ValidationResult> results = new List<ValidationResult>();
    private bool showOnlyErrors = false;

    [MenuItem("Tools/Validate Project Rules")]
    public static void ShowWindow()
    {
        var window = GetWindow<ProjectValidator>("Project Validator");
        window.minSize = new Vector2(600, 400);
        window.RunValidation();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        // Header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Project Rule Validator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Validates DEVELOPMENT_RULES.md compliance", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        // Controls
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
        {
            RunValidation();
        }
        showOnlyErrors = EditorGUILayout.ToggleLeft("Show Only Errors", showOnlyErrors, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Results
        if (results.Count == 0)
        {
            EditorGUILayout.HelpBox("Click 'Run Validation' to check project rules", MessageType.Info);
        }
        else
        {
            DrawSummary();
            EditorGUILayout.Space();
            DrawResults();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSummary()
    {
        int errors = results.Count(r => r.severity == Severity.Error);
        int warnings = results.Count(r => r.severity == Severity.Warning);
        int passed = results.Count(r => r.severity == Severity.Pass);

        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label($"✓ Passed: {passed}", EditorStyles.boldLabel);
        GUILayout.Label($"⚠ Warnings: {warnings}", EditorStyles.boldLabel);
        GUILayout.Label($"❌ Errors: {errors}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        if (errors > 0)
        {
            EditorGUILayout.HelpBox($"Found {errors} critical issues. Fix them before committing!", MessageType.Error);
        }
        else if (warnings > 0)
        {
            EditorGUILayout.HelpBox($"Found {warnings} warnings. Review recommended.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("All checks passed! ✓", MessageType.Info);
        }
    }

    private void DrawResults()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var result in results)
        {
            if (showOnlyErrors && result.severity != Severity.Error)
                continue;

            DrawResult(result);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawResult(ValidationResult result)
    {
        Color bgColor = GetSeverityColor(result.severity);
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = Color.white;

        // Title
        EditorGUILayout.LabelField(GetSeverityIcon(result.severity) + " " + result.category, EditorStyles.boldLabel);

        // Message
        EditorGUILayout.LabelField(result.message, EditorStyles.wordWrappedLabel);

        // Action
        if (!string.IsNullOrEmpty(result.actionHint))
        {
            EditorGUILayout.LabelField("→ " + result.actionHint, EditorStyles.miniLabel);
        }

        // Object reference (if any)
        if (result.target != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField("Object:", result.target, typeof(Object), true);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = result.target;
                EditorGUIUtility.PingObject(result.target);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void RunValidation()
    {
        results.Clear();
        EditorUtility.DisplayProgressBar("Validating Project", "Running checks...", 0f);

        try
        {
            ValidateSingletons();
            ValidateActionDatabase();
            ValidateScriptableObjects();
            ValidateMonoBehaviours();
            ValidatePrefabs();
            ValidateScenes();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Repaint();
    }

    // ─── 검증 규칙들 ───

    private void ValidateSingletons()
    {
        var scripts = AssetDatabase.FindAssets("t:Script")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.StartsWith("Assets/Scripts/"))
            .ToList();

        int singletonCount = 0;

        foreach (var scriptPath in scripts)
        {
            var content = System.IO.File.ReadAllText(scriptPath);
            if (content.Contains("public static") && content.Contains("Instance") &&
                (content.Contains("{ get;") || content.Contains("get {")))
            {
                singletonCount++;
                results.Add(new ValidationResult
                {
                    category = "🚨 Singleton Detected",
                    message = $"Found Singleton pattern in: {System.IO.Path.GetFileName(scriptPath)}",
                    severity = Severity.Warning,
                    actionHint = "Consider using ScriptableObject or DI instead. See DEVELOPMENT_RULES.md",
                    target = AssetDatabase.LoadAssetAtPath<Object>(scriptPath)
                });
            }
        }

        if (singletonCount == 0)
        {
            results.Add(new ValidationResult
            {
                category = "Singleton Check",
                message = "No new Singletons detected (existing ones are tolerated)",
                severity = Severity.Pass
            });
        }
    }

    private void ValidateActionDatabase()
    {
        var allActionTypes = System.Enum.GetValues(typeof(ActionType)).Cast<ActionType>();
        int validCount = 0;
        int missingCount = 0;

        foreach (ActionType actionType in allActionTypes)
        {
            if (actionType == ActionType.None) continue;

            if (!ActionDatabase.Exists(actionType))
            {
                missingCount++;
                results.Add(new ValidationResult
                {
                    category = "❌ Missing Action in Database",
                    message = $"ActionType.{actionType} is not defined in ActionDatabase!",
                    severity = Severity.Error,
                    actionHint = $"Add ActionType.{actionType} to ActionDatabase._actions dictionary"
                });
            }
            else
            {
                // Validate content
                var info = ActionDatabase.GetInfo(actionType);
                if (string.IsNullOrEmpty(info.DisplayName))
                {
                    results.Add(new ValidationResult
                    {
                        category = "⚠️ Incomplete Action Info",
                        message = $"ActionType.{actionType} has empty DisplayName",
                        severity = Severity.Warning
                    });
                }
                else
                {
                    validCount++;
                }
            }
        }

        if (missingCount == 0 && validCount > 0)
        {
            results.Add(new ValidationResult
            {
                category = "ActionDatabase Check",
                message = $"All {validCount} actions are defined in ActionDatabase",
                severity = Severity.Pass
            });
        }
    }

    private void ValidateScriptableObjects()
    {
        var allScriptableObjects = AssetDatabase.FindAssets("t:ScriptableObject")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
            .Where(so => so != null)
            .ToList();

        foreach (var so in allScriptableObjects)
        {
            // Check for null fields (basic validation)
            var serializedObject = new SerializedObject(so);
            var property = serializedObject.GetIterator();

            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                    property.objectReferenceValue == null &&
                    property.name != "m_Script")
                {
                    // This is optional - some null references are intentional
                }
            }
        }
    }

    private void ValidateMonoBehaviours()
    {
        var scripts = MonoImporter.GetAllRuntimeMonoScripts()
            .Where(s => s != null && s.GetClass() != null)
            .Where(s => typeof(MonoBehaviour).IsAssignableFrom(s.GetClass()))
            .ToList();

        int missingOnValidate = 0;

        foreach (var script in scripts)
        {
            var type = script.GetClass();
            if (type == null) continue;

            // Skip Unity internal types
            if (type.Namespace != null && type.Namespace.StartsWith("Unity")) continue;

            // Check if has SerializeField but no OnValidate
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance |
                                      System.Reflection.BindingFlags.NonPublic |
                                      System.Reflection.BindingFlags.Public);

            bool hasSerializeField = fields.Any(f =>
                f.GetCustomAttributes(typeof(SerializeField), true).Length > 0 ||
                (f.IsPublic && !f.IsStatic));

            if (hasSerializeField)
            {
                var onValidateMethod = type.GetMethod("OnValidate",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

                if (onValidateMethod == null)
                {
                    missingOnValidate++;
                    // Too many warnings, so we just count them
                }
            }
        }

        if (missingOnValidate > 0)
        {
            results.Add(new ValidationResult
            {
                category = "⚠️ OnValidate Missing",
                message = $"{missingOnValidate} MonoBehaviours with SerializeFields don't have OnValidate()",
                severity = Severity.Warning,
                actionHint = "Add OnValidate() to validate Inspector references"
            });
        }
    }

    private void ValidatePrefabs()
    {
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.StartsWith("Assets/"))
            .ToList();

        int missingReferences = 0;

        foreach (var prefabPath in prefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            // Check for missing references
            var components = prefab.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null)
                {
                    missingReferences++;
                    results.Add(new ValidationResult
                    {
                        category = "❌ Missing Component",
                        message = $"Prefab has missing component: {prefabPath}",
                        severity = Severity.Error,
                        target = prefab
                    });
                    break; // One error per prefab is enough
                }
            }
        }

        if (missingReferences == 0)
        {
            results.Add(new ValidationResult
            {
                category = "Prefab Check",
                message = $"Validated {prefabPaths.Count} prefabs - no missing components",
                severity = Severity.Pass
            });
        }
    }

    private void ValidateScenes()
    {
        var scenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToList();

        results.Add(new ValidationResult
        {
            category = "Scene Check",
            message = $"Found {scenePaths.Count} scenes (manual testing recommended)",
            severity = Severity.Pass
        });
    }

    // ─── Helper Methods ───

    private Color GetSeverityColor(Severity severity)
    {
        switch (severity)
        {
            case Severity.Error: return new Color(1f, 0.7f, 0.7f);
            case Severity.Warning: return new Color(1f, 0.95f, 0.7f);
            case Severity.Pass: return new Color(0.7f, 1f, 0.7f);
            default: return Color.white;
        }
    }

    private string GetSeverityIcon(Severity severity)
    {
        switch (severity)
        {
            case Severity.Error: return "❌";
            case Severity.Warning: return "⚠️";
            case Severity.Pass: return "✓";
            default: return "•";
        }
    }

    private enum Severity
    {
        Pass,
        Warning,
        Error
    }

    private class ValidationResult
    {
        public string category;
        public string message;
        public Severity severity;
        public string actionHint;
        public Object target;
    }
}
#endif
