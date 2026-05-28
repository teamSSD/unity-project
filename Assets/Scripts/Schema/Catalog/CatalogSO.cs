using System.Collections.Generic;
using UnityEngine;

namespace Game.Schema.Catalog
{
    /// <summary>
    /// id 키로 ScriptableObject를 조회하는 카탈로그 베이스.
    /// 인스펙터에서 items 리스트에 자산을 드래그 등록.
    /// Resources.Load/LoadAll을 대체 (코드 동적 키 → catalog.GetById(id)).
    /// 빌드 시 인스펙터 참조 → 자산 자동 포함.
    /// </summary>
    public abstract class CatalogSO<T> : ScriptableObject where T : Object
    {
        [SerializeField] protected List<T> items = new List<T>();
        private Dictionary<string, T> _byId;

        /// <summary>각 item에서 키(id)를 추출하는 방법 — 자식 클래스가 구현</summary>
        protected abstract string GetKey(T item);

        public T GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null) RebuildIndex();
            return _byId.TryGetValue(id, out var v) ? v : null;
        }

        public IReadOnlyList<T> All
        {
            get
            {
                if (_byId == null) RebuildIndex();
                return items;
            }
        }

        public int Count => items?.Count ?? 0;

        private void RebuildIndex()
        {
            _byId = new Dictionary<string, T>(items?.Count ?? 0);
            if (items == null) return;
            foreach (var item in items)
            {
                if (item == null) continue;
                var key = GetKey(item);
                if (!string.IsNullOrEmpty(key)) _byId[key] = item;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _byId = null; // 인스펙터 수정 시 캐시 무효화
        }
#endif
    }
}
