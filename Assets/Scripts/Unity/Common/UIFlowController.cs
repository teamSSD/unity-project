/// <summary>씬 전환 전에 열린 전역 UI를 정상 경로로 닫아 표시 상태와 잠금을 함께 정리한다.</summary>
public static class UIFlowController
{
    public static void CloseAllForSceneTransition()
    {
        ConfirmModal.Dismiss();
        RecipeBookManager.Instance?.Close();
        SettingsUIManager.Instance?.Close();
        ShopUIAdapter.Instance?.CloseShop();
        BentoSelectionController.ActiveInstance?.Close();
    }
}
