using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전역 UI 잠금 관리. 여러 시스템이 동시에 잠글 수 있으며,
/// 모든 잠금이 해제되어야 다른 UI가 열림.
/// </summary>
public static class UILockManager
{
    public enum Owner
    {
        Minigame,
        Dialogue,
        RecipeBook,
        BentoSelection,
        GameStart,
        Loading,
        Shop,
        Settings,
        PhaseSelection,
        Tutorial,
        CookingTutorial
    }

    private static readonly HashSet<Owner> activeLocks = new();

    /// <summary>하나라도 잠겨있으면 true</summary>
    public static bool IsLocked => activeLocks.Count > 0;

    public static void Lock(Owner owner)
    {
        activeLocks.Add(owner);
    }

    public static void Unlock(Owner owner)
    {
        activeLocks.Remove(owner);
    }

    public static bool IsLockedBy(Owner owner)
    {
        return activeLocks.Contains(owner);
    }

    /// <summary>
    /// requester가 열어도 되는지 확인.
    /// 자기 이외의 잠금이 없으면 true.
    /// </summary>
    public static bool CanOpen(Owner requester)
    {
        if (activeLocks.Count == 0) return true;
        if (activeLocks.Count == 1 && activeLocks.Contains(requester)) return true;
        return false;
    }

    /// <summary>씬 전환 시 모든 잠금 초기화</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        activeLocks.Clear();
    }
}
