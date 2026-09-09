/// <summary>씬 전환 전에 열린 전역 UI를 정상 경로로 닫아 표시 상태와 잠금을 함께 정리한다.</summary>
public static class UIFlowController
{
    public static bool TryOpenSettings()
    {
        if (!UILockManager.CanOpen(UILockManager.Owner.Settings)) return false;
        SettingsUIManager.Instance?.Open();
        return SettingsUIManager.Instance != null;
    }

    public static bool TryOpenShop(ShopUIAdapter.Tab tab)
    {
        if (!UILockManager.CanOpen(UILockManager.Owner.Shop)) return false;
        ShopUIAdapter.Instance?.OpenShop(tab);
        return ShopUIAdapter.Instance != null;
    }

    public static bool TryOpenBentoSelection(BentoSelectionController controller, System.Action onConfirm)
    {
        if (controller == null || !UILockManager.CanOpen(UILockManager.Owner.BentoSelection)) return false;
        controller.Show(onConfirm);
        return true;
    }

    public static void CloseAllForSceneTransition()
    {
        ConfirmModal.Dismiss();
        RecipeBookManager.Instance?.Close();
        SettingsUIManager.Instance?.Close();
        ShopUIAdapter.Instance?.CloseShop();
        BentoSelectionController.ActiveInstance?.Close();
    }
}
