using System.Collections.Generic;

namespace Game.Domain.Mall
{
    /// <summary>
    /// NPC 그룹별 퀘스트 메뉴 템플릿. deliveryQuest.csv 1회 파싱 후 메모리에 보관.
    /// 영구 상태 없음 (catalog), 세션 시작 시 GameSessionRoot가 빌드.
    /// </summary>
    public class QuestMenuCatalog
    {
        private readonly Dictionary<string, MenuSchema> _menus = new();

        public QuestMenuCatalog(IEnumerable<(string groupId, MenuSchema menu)> entries)
        {
            foreach (var (groupId, menu) in entries)
                _menus[groupId] = menu;
        }

        public MenuSchema GetByGroupId(string groupId)
            => _menus.TryGetValue(groupId, out var m) ? m : null;
    }
}
