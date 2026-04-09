using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 도시락 선택 데이터 (MenuSchema 개선 버전)
/// Schema 레이어: 순수 데이터 + 검증 로직
/// </summary>
[System.Serializable]
public class BentoSelection
{
    public string Name;
    public int OrderNumber;
    public FoodData MainMenu;
    public List<FoodData> SideMenus;

    /// <summary>
    /// 도시락 간 중복 허용 여부 (디폴트: true)
    /// 나중에 설정으로 변경 가능
    /// </summary>
    public bool AllowDuplicates = true;

    public BentoSelection(string name, int orderNumber)
    {
        Name = name;
        OrderNumber = orderNumber;
        MainMenu = null;
        SideMenus = new List<FoodData>();
        AllowDuplicates = true;
    }

    /// <summary>
    /// 도시락 구성이 유효한지 검증
    /// - 사이드 최대 3개
    /// - 메인이 있으면 MAIN 타입이어야 함
    /// - AllowDuplicates가 false면 중복 체크
    /// </summary>
    public bool IsValid()
    {
        // 사이드 최대 3개
        if (SideMenus.Count > 3)
            return false;

        // 메인 타입 검증
        if (MainMenu != null && MainMenu.type != FoodType.MAIN)
            return false;

        // 사이드 타입 검증
        if (SideMenus.Any(s => s.type != FoodType.SIDE))
            return false;

        // 중복 체크 (AllowDuplicates가 false일 때만)
        if (!AllowDuplicates)
        {
            var ids = new HashSet<string>();
            if (MainMenu != null && !ids.Add(MainMenu.id))
                return false;

            foreach (var side in SideMenus)
            {
                if (!ids.Add(side.id))
                    return false; // 중복 발견
            }
        }

        return true;
    }

    /// <summary>
    /// 선택된 메뉴가 있는지 확인 (MenuSelect 활성화 조건)
    /// </summary>
    public bool HasSelection()
    {
        return MainMenu != null || SideMenus.Count > 0;
    }

    /// <summary>
    /// 도시락 구성이 완전한지 확인 (메인 1개 필수)
    /// </summary>
    public bool IsComplete()
    {
        return MainMenu != null || SideMenus.Count > 0;
    }

    public override string ToString()
    {
        string mainName = MainMenu?.ingredientName ?? "None";
        string sideNames = string.Join(", ", SideMenus?.Select(f => f.ingredientName) ?? new string[0]);
        string duplicateInfo = AllowDuplicates ? " (중복 허용)" : " (중복 불허)";
        return $"{Name} (#{OrderNumber}): Main={mainName}, Sides=[{sideNames}]{duplicateInfo}";
    }
}
