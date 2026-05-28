using UnityEngine;

public static class UIColors
{
    // Panel / Background
    public static readonly Color PanelBg         = new Color(0.9804f, 0.9608f, 0.9451f); // #FAF5F1 light warm white
    public static readonly Color PanelBgAlt      = new Color(0.9725f, 0.9647f, 0.9569f); // #F8F6F4 light ivory

    // Button
    public static readonly Color ButtonPrimary   = new Color(0.9804f, 0.9608f, 0.9451f); // #FAF5F1 confirm/active
    public static readonly Color ButtonSecondary = new Color(218/255f, 175/255f, 144/255f); // #DAAF90 back/inactive

    // Text
    public static readonly Color TextPrimary     = new Color( 88/255f,  60/255f,  40/255f); // #583C28 dark brown
    public static readonly Color TextSecondary   = new Color( 98/255f,  70/255f,  52/255f); // #624634 medium brown

    // Selection overlay used by MenuSelectionItem.cellImage
    public static readonly Color SelectionOverlay = new Color(218/255f, 175/255f, 144/255f, 0.30f); // #DAAF90 @ 30%

    // Status
    public static readonly Color Income  = new Color(0.18f, 0.70f, 0.35f); // #2EB25A
    public static readonly Color Expense = new Color(0.85f, 0.25f, 0.25f); // #D84040
}
