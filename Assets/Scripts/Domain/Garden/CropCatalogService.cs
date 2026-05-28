using System.Collections.Generic;

namespace Game.Domain.Garden
{
    /// <summary>
    /// Crop 카탈로그 조회 서비스. 작물 마스터 데이터(CSV에서 로드)에 대한 query만 담당.
    /// 영구 상태 없음 (휘발성, 매 세션 재구축 가능).
    /// </summary>
    public class CropCatalogService
    {
        private readonly List<CropData> _crops;
        private readonly float _totalWeight;

        public CropCatalogService(IEnumerable<CropData> crops)
        {
            _crops = new List<CropData>(crops);
            foreach (var c in _crops) _totalWeight += c.spawnWeight;
        }

        public CropData GetCropById(string cropId) => _crops.Find(c => c.cropId == cropId);

        public CropData GetRandomCropByWeight()
        {
            if (_crops.Count == 0) return null;
            return GameRandom.WeightedPick(GameRandom.Immutable, _crops, c => c.spawnWeight);
        }

        public IReadOnlyList<CropData> All => _crops;
    }
}
