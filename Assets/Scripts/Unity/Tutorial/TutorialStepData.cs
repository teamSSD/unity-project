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

    [Tooltip("타겟 스크린 좌표에서 이 파트 전용 offset (px). 미세 조정.")]
    public Vector2 screenOffset;

    [Range(-1f, 1f), Tooltip("Tail이 body의 어느 쪽에 붙을지. -1=왼쪽 코너, 0=중앙, +1=오른쪽 코너.")]
    public float tailHorizontalFraction = -0.5f;

    [Tooltip("이 파트를 dismiss하는 키. 기본 Space. Recipe Tab처럼 특정 키를 요구할 때 변경.")]
    public KeyCode dismissKey = KeyCode.Space;

    [Tooltip("체크하면 target의 스크린 X 위치에 따라 tailHorizontalFraction 부호 자동 반전. 우측 target = tail body 오른쪽, 좌측 = 왼쪽.")]
    public bool autoFlipByScreenSide;

    [Tooltip("체크하면 RecipeBookManager가 실제로 닫힐 때만 dismiss (dismissKey 무시). 카드 ESC로 닫혀도 안 넘어감.")]
    public bool dismissOnRecipeBookClose;

    [Tooltip("체크하면 이 파트 표시 중 UILockManager 잠금 해제 (bubble Tutorial owner + Cooking mock의 CookingTutorial owner). 손님 클릭/도시락 드래그 등 게임 상호작용 필요할 때.")]
    public bool allowSceneInteraction;

    [Tooltip("체크하면 이 파트 표시 중 카메라 자유 제어 (HorizontalCameraMove 활성). 요리하는 동안 유저가 카메라 움직일 수 있게. 미체크시 튜토리얼이 target으로 강제 이동.")]
    public bool freeCamera;

    [Tooltip("체크하면 이 파트 표시 중 RecipeBook을 강제로 열린 상태 유지 (Tab/ESC로 안 닫힘). '메뉴 클릭' 안내 파트용. 카드 ESC 닫기는 허용.")]
    public bool forceRecipeBookOpen;

    [Tooltip("체크하면 이 파트 표시 중 RecipeBook Tab으로 열리는 것 차단. 레시피북 안내(Tab hint) 이전 파트들에서 사용.")]
    public bool blockRecipeBookOpen;
}
