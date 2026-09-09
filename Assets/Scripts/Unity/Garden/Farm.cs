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
    [Tooltip("작물 스프라이트 상단과 작물명 사이의 FarmTile 로컬 좌표 여백.")]
    [Min(0f)] public float cropNameClearance = 0.1f;
    [Tooltip("작물명 상단과 성장 게이지 사이의 FarmTile 로컬 좌표 여백.")]
    [Min(0f)] public float gaugeClearance = 0.1f;

    public int farmIndex;
    private FarmTile tile;
    private TimePhaseProvider phaseProvider;

    private bool playerIn = false;
    public bool IsLocked => farmIndex >= (int)(GameSessionRoot.Instance?.FarmUpgrade?.GetCurrentData("tile")?.value ?? 3);

#if AFTERTASTE_E2E
    public string E2ETargetId => $"farm.tile.{farmIndex}";
    public string E2ECropId => tile?.GetCurrentCrop()?.cropId ?? string.Empty;
    public int E2EPassedPhases => tile?.GetPassedPhases() ?? 0;
    public int E2ERequiredPhases
    {
        get
        {
            var crop = tile?.GetCurrentCrop();
            if (crop == null) return 0;
            float reduction = GameSessionRoot.Instance?.FarmUpgrade?.GetCurrentData("timeReduction")?.value ?? 0f;
            return Mathf.CeilToInt(crop.growPhaseCount * (1f - reduction));
        }
    }
    public bool E2EIsHarvestable => tile?.IsHarvestable() ?? false;
#endif

    private void Start()
    {
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.RegisterCollider($"farm.tile.{farmIndex}", GetComponent<Collider2D>());
#endif
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
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.UnregisterCollider($"farm.tile.{farmIndex}", GetComponent<Collider2D>());
#endif
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

        PositionCropIndicatorsAboveCrop();
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
            // 성장 중 작물명은 상시 라벨이 담당한다. 근접 안내문에서 중복 표시하지 않는다.
            actionPrompt.text = "";
        }
    }

    private void PositionCropIndicatorsAboveCrop()
    {
        if (cropSpriteRenderer == null || cropNameCanvas == null) return;
        if (cropNameCanvas.transform is not RectTransform nameRect) return;

        Sprite sprite = cropSpriteRenderer.sprite;
        if (sprite == null) return;

        // Tight mesh 꼭짓점은 투명 여백을 제외한 실제 그려지는 작물 윤곽이다.
        // 원본 rect 또는 Renderer.bounds를 쓰면 새싹이 작을 때 라벨이 하늘로 올라간다.
        Bounds visibleSpriteBounds = FarmLabelLayout.VisibleBounds(sprite.vertices, sprite.bounds);
        float labelHalfHeight = FarmLabelLayout.HalfHeightInAncestor(nameRect, transform);
        nameRect.localPosition = FarmLabelLayout.AboveCrop(
            cropSpriteRenderer.transform.localPosition,
            cropSpriteRenderer.transform.localScale,
            visibleSpriteBounds,
            labelHalfHeight,
            cropNameClearance);

        if (gaugeCanvas?.transform is not RectTransform gaugeRect) return;

        // 이름은 작물 높이에 따라 움직이므로 게이지도 이름을 기준으로 쌓아야
        // 성장 단계가 바뀌어도 항상 작물 → 이름 → 게이지 순서가 유지된다.
        float gaugeHalfHeight = FarmLabelLayout.HalfHeightInAncestor(gaugeRect, transform);
        Vector3 labelCenter = transform.InverseTransformPoint(nameRect.position);
        Vector3 gaugeCenter = FarmLabelLayout.AboveElement(
            labelCenter,
            labelHalfHeight,
            gaugeHalfHeight,
            gaugeClearance);
        gaugeRect.position = transform.TransformPoint(gaugeCenter);
    }

    private void OnTimePassed()
    {
        UpdateVisuals();
        UpdatePrompt();
    }
}
