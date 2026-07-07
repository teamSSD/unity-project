using System.Collections.Generic;
using UnityEngine;

/// <summary>모든 튜토리얼 스텝의 SO 컬렉션. TutorialController가 stepId로 조회.</summary>
[CreateAssetMenu(fileName = "TutorialStepCatalog", menuName = "Tutorial/Step Catalog")]
public class TutorialStepCatalog : ScriptableObject
{
    public List<TutorialStepData> steps = new();

    private Dictionary<int, TutorialStepData> _map;

    public TutorialStepData Get(int stepId)
    {
        if (_map == null || _map.Count != steps.Count)
        {
            _map = new Dictionary<int, TutorialStepData>();
            foreach (var s in steps)
                if (s != null) _map[s.stepId] = s;
        }
        return _map.TryGetValue(stepId, out var d) ? d : null;
    }
}
