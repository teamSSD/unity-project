using UnityEngine;

public static class UIColors
{
    // Panel / Background
    public static readonly Color PanelBg         = new Color(0.9804f, 0.9608f, 0.9451f); // #FAF5F1 light warm white
    public static readonly Color PanelBgAlt      = new Color(0.9725f, 0.9647f, 0.9569f); // #F8F6F4 light ivory

    // Button
    public static readonly Color ButtonPrimary   = new Color(0.9804f, 0.9608f, 0.9451f); // #FAF5F1 confirm/active
    public static readonly Color ButtonSecondary = new Color(218/255f, 175/255f, 144/255f); // #DAAF90 back/inactive (탭 비활성 재사용)
    public static readonly Color ButtonAccent    = new Color(201/255f, 102/255f,  90/255f); // #C9665A terracotta — destructive 강조 (버리기 / 삭제 / 위험)
    public static readonly Color OnAccent        = Color.white;                              // ButtonAccent 위 텍스트
    public static readonly Color ButtonNeutral   = new Color(176/255f, 168/255f, 158/255f); // #B0A89E warm gray — 취소 / 보조

    // Tab
    public static readonly Color TabActive       = new Color(243/255f, 222/255f, 208/255f); // #F3DED0 선택된 탭 배경 (활성)

    // Text
    public static readonly Color TextPrimary     = new Color( 88/255f,  60/255f,  40/255f); // #583C28 dark brown
    public static readonly Color TextSecondary   = new Color( 98/255f,  70/255f,  52/255f); // #624634 medium brown

    // Selection overlay used by MenuSelectionItem.cellImage
    public static readonly Color SelectionOverlay = new Color(218/255f, 175/255f, 144/255f, 0.30f); // #DAAF90 @ 30%

    // Status
    public static readonly Color Income  = new Color(0.18f, 0.70f, 0.35f); // #2EB25A
    public static readonly Color Expense = new Color(0.85f, 0.25f, 0.25f); // #D84040
}
