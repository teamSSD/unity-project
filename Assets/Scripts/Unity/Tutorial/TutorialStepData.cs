using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 개별 스텝 콘텐츠 SO.
/// 하나의 stepId 안에 여러 파트(bubble)를 순차 표시.
/// 각 파트는 자기 타겟(TutorialTarget key)과 tail 방향을 가짐.
/// </summary>
[CreateAssetMenu(fileName = "TutorialStep_", menuName = "Tutorial/Step Data")]
public class TutorialStepData : ScriptableObject
{
    [Tooltip("고유 step ID. TutorialStepId 상수와 매치.")]
    public int stepId;

    public List<TutorialStepPart> parts = new();
}

[System.Serializable]
public class TutorialStepPart
{
    [TextArea(1, 5)]
    [Tooltip("이 파트의 안내 텍스트 (짧게 한 줄 권장).")]
    public string message;

    [Tooltip("옵션 스냅샷 이미지 (미니게임 등).")]
    public Sprite optionalImage;

    [Tooltip("TutorialTarget 컴포넌트의 key. 비우면 화면 중앙.")]
    public string targetKey;

    [Tooltip("tail 방향. Down=타겟이 아래(bubble 위), Up=타겟이 위(bubble 아래).")]
    public TutorialBubble.TailDirection tailDirection = TutorialBubble.TailDirection.Down;
}
