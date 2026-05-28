using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Schema.Catalog
{
    /// <summary>
    /// 작물 sprite path → Sprite 매핑.
    /// CropData(POCO)의 imagePath 문자열로 Sprite를 조회.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Catalog/CropSprite", fileName = "CropSpriteCatalog")]
    public class CropSpriteCatalogSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;     // CSV의 imagePath와 동일한 값
            public Sprite sprite;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        private Dictionary<string, Sprite> _byKey;

        public Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_byKey == null) RebuildIndex();
            return _byKey.TryGetValue(key, out var s) ? s : null;
        }

        public int Count => entries?.Count ?? 0;

        private void RebuildIndex()
        {
            _byKey = new Dictionary<string, Sprite>(entries?.Count ?? 0);
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.key) && e.sprite != null)
                    _byKey[e.key] = e.sprite;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _byKey = null;
        }
#endif
    }
}
