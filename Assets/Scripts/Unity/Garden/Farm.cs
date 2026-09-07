using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Farm : MonoBehaviour
{
    [System.NonSerialized] public CropData cropData;
    public TextMeshProUGUI actionPrompt;

    [Header("작물 표시")]
    public SpriteRenderer cropSpriteRenderer;
    public float uiOffsetY = 0.5f;

    [Header("성장 게이지 UI")]
    public GaugeUI growthGauge;
    public GameObject gaugeCanvas;

    [Header("작물 이름 UI")]
    public GameObject cropNameCanvas;
    public TextMeshProUGUI cropNameLabel;
    [Tooltip("crop sprite 하단에서 이름 라벨까지의 offset (양수 = 더 아래).")]
    public float nameOffsetY = 0.3f;

    public int farmIndex;
    private FarmTile tile;
    private TimePhaseProvider phaseProvider;

    private bool playerIn = false;
    public bool IsLocked => farmIndex >= (int)(GameSessionRoot.Instance?.FarmUpgrade?.GetCurrentData("tile")?.value ?? 3);

    private void Start()
    {
        if (GameSessionRoot.Instance?.Progress == null)
            ManagerBootstrap.EnsureAll();

        var progress = GameSessionRoot.Instance?.Progress;
        if (progress == null)
        {
            Debug.LogError("[Farm] ProgressService not found");
            return;
        }
        phaseProvider = progress;

        tile = new FarmTile(phaseProvider);

        // 저장된 타일 상태 복원 (GardenPersistent.tiles)
        var gp = GameSessionRoot.Instance?.State.garden.persistent;
        if (gp != null && farmIndex >= 0 && farmIndex < gp.tiles.Length && gp.tiles[farmIndex] != null)
            tile.ApplySaveData(gp.tiles[farmIndex]);

        // 빈 타일이면 자동 심기
        var cropCatalog = GameSessionRoot.Instance?.CropCatalog;
        if (!IsLocked && tile.IsEmpty() && cropCatalog != null)
        {
            CropData randomCrop = cropCatalog.GetRandomCropByWeight();
            if (randomCrop != null)
            {
                cropData = randomCrop;
                tile.Plant(randomCrop);
            }
        }

        progress.OnPhaseChanged += OnPhaseChangedHandler;
        OnTimePassed();
    }

    private void OnDestroy()
    {
        // 씬 나가기 전에 타일 상태 저장 (GardenPersistent.tiles)
        if (tile != null)
        {
            var gp = GameSessionRoot.Instance?.State.garden.persistent;
            if (gp != null && farmIndex >= 0 && farmIndex < gp.tiles.Length)
                gp.tiles[farmIndex] = tile.GetSaveData();
        }

        var progress = GameSessionRoot.Instance?.Progress;
        if (progress != null) progress.OnPhaseChanged -= OnPhaseChangedHandler;
    }

    private void OnPhaseChangedHandler(PhaseType _) => OnTimePassed();

    void Update()
    {
        if (tile == null) return;
        if (playerIn && Input.GetKeyDown(KeyCode.Space))
        {
            if (IsLocked)
            {
            }
            else if (tile.IsHarvestable())
            {
                int harvestCount = (int)(GameSessionRoot.Instance?.FarmUpgrade?.GetCurrentData("harvestCount")?.value ?? 5);
                if (tile.Harvest(out string id, out int crops, harvestCount))
                {
                    Debug.Log($"[Farm] Harvested! [{id}] x{crops}");
                    GameSessionRoot.Instance?.Inventory?.AddHarvestedCrop(id, crops);

                    // 수확 후 자동 심기
                    CropData nextCrop = GameSessionRoot.Instance.CropCatalog.GetRandomCropByWeight();
                    if (nextCrop != null)
                    {
                        cropData = nextCrop;
                        tile.Plant(nextCrop);
                    }
                }
            }
            else
            {
            }

            OnTimePassed();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(Tags.Player))
        {
            playerIn = true;
            UpdatePrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(Tags.Player))
        {
            playerIn = false;
            actionPrompt.text = "";
        }
    }

    public void UpdateVisuals()
    {
        if (cropSpriteRenderer == null || tile == null) return;

        CropData currentCrop = tile.GetCurrentCrop();

        if (currentCrop == null)
        {
            cropSpriteRenderer.sprite = null;
            if (gaugeCanvas != null) gaugeCanvas.SetActive(false);
            if (cropNameCanvas != null) cropNameCanvas.SetActive(false);
            return;
        }

        if (gaugeCanvas != null) gaugeCanvas.SetActive(true);
        if (cropNameCanvas != null) cropNameCanvas.SetActive(true);
        cropSpriteRenderer.sprite = currentCrop.GetSpriteAt(tile.GetPassedPhases());

        if (growthGauge != null)
        {
            int passed = tile.GetPassedPhases();
            if (passed == 0)
                growthGauge.SnapTo(0, currentCrop.growPhaseCount);
            else
                growthGauge.SetProgress(passed, currentCrop.growPhaseCount);
        }

        if (cropNameLabel != null)
        {
            var food = SearchDataUtil.GetFoodDataById(currentCrop.cropId);
            cropNameLabel.text = food?.ingredientName ?? currentCrop.cropId;
        }

        // 이전엔 AdjustUIPosition으로 gauge/name을 sprite bounds 기준 이동시켰지만,
        // 크롭 sprite가 3840x2160 @ PPU 217로 매우 크고 custom pivot이라 bounds.max.y가
        // 예측 불가 (천장에 매달림 이슈). 지금은 prefab에서 잡은 anchoredPosition 그대로 사용.
        // 위치 튜닝은 Editor Inspector에서 Canvas/GrowthGauge RectTransform 직접 조작.
    }

    private void UpdatePrompt()
    {
        if (!playerIn || actionPrompt == null || tile == null) return;

        if (IsLocked)
            actionPrompt.text = "*locked*";
        else if (tile.IsHarvestable())
            actionPrompt.text = "(press spacebar to harvest)";
        else if (tile.IsEmpty())
            actionPrompt.text = "(press spacebar to plant)";
        else
        {
            // "성장 중..." 대신 실제 작물 이름 표시.
            var food = SearchDataUtil.GetFoodDataById(tile.GetCurrentCrop()?.cropId);
            actionPrompt.text = food?.ingredientName ?? "";
        }
    }

    // AdjustUIPosition 삭제: prefab의 anchoredPosition만 사용.

    private void OnTimePassed()
    {
        UpdateVisuals();
        UpdatePrompt();
    }
}
