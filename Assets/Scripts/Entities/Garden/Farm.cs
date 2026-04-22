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

    public int farmIndex;
    private FarmTile tile;
    private TimePhaseProvider phaseProvider;

    private bool playerIn = false;
    public bool IsLocked => farmIndex >= (FarmUpgradeManager.Instance?.GetCurrentData()?.tileCount ?? 3);

    private void Start()
    {
        if (ProgressSystem.Instance == null)
            ManagerBootstrap.EnsureAll();

        phaseProvider = ProgressSystem.Instance;
        if (phaseProvider == null)
        {
            Debug.LogError("[Farm] ProgressSystem not found");
            return;
        }

        tile = new FarmTile(phaseProvider);

        // 저장된 타일 상태 복원
        var saved = FarmTileStorage.GetTileData(farmIndex);
        if (saved != null) tile.ApplySaveData(saved);

        // 빈 타일이면 자동 심기
        if (!IsLocked && tile.IsEmpty())
        {
            CropData randomCrop = CropDataManager.Instance.GetRandomCropByWeight();
            if (randomCrop != null)
            {
                cropData = randomCrop;
                tile.Plant(randomCrop);
            }
        }

        ProgressSystem.Instance.OnPhaseChanged += OnPhaseChangedHandler;
        OnTimePassed();
    }

    private void OnDestroy()
    {
        // 씬 나가기 전에 타일 상태 저장
        if (tile != null) FarmTileStorage.SetTileData(farmIndex, tile.GetSaveData());

        if (ProgressSystem.Instance != null)
            ProgressSystem.Instance.OnPhaseChanged -= OnPhaseChangedHandler;
    }

    private void OnPhaseChangedHandler(PhaseType _) => OnTimePassed();

    void Update()
    {
        if (tile == null) return;
        if (playerIn && Input.GetKeyDown(KeyCode.Space))
        {
            if (IsLocked)
            {
                Debug.Log("This farm is locked!");
            }
            else if (tile.IsHarvestable())
            {
                int harvestCount = FarmUpgradeManager.Instance?.GetCurrentData()?.harvestCount ?? 5;
                if (tile.Harvest(out string id, out int crops, harvestCount))
                {
                    Debug.Log($"[Farm] Harvested! [{id}] x{crops}");
                    InventoryManager.Instance?.AddHarvestedCrop(id, crops);

                    // 수확 후 자동 심기
                    CropData nextCrop = CropDataManager.Instance.GetRandomCropByWeight();
                    if (nextCrop != null)
                    {
                        cropData = nextCrop;
                        tile.Plant(nextCrop);
                    }
                }
            }
            else
            {
                Debug.Log("Crop is still growing...");
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
            return;
        }

        if (gaugeCanvas != null) gaugeCanvas.SetActive(true);
        cropSpriteRenderer.sprite = currentCrop.sprite;

        if (growthGauge != null)
        {
            int passed = tile.GetPassedPhases();
            if (passed == 0)
                growthGauge.SnapTo(0, currentCrop.growPhaseCount);
            else
                growthGauge.SetProgress(passed, currentCrop.growPhaseCount);
        }

        AdjustUIPosition();
    }

    private void UpdatePrompt()
    {
        if (!playerIn || actionPrompt == null || tile == null) return;

        if (IsLocked)
            actionPrompt.text = "잠겨 있음";
        else if (tile.IsHarvestable())
            actionPrompt.text = "(스페이스바로 수확)";
        else if (tile.IsEmpty())
            actionPrompt.text = "(스페이스바로 심기)";
        else
            actionPrompt.text = "성장 중...";
    }

    private void AdjustUIPosition()
    {
        if (cropSpriteRenderer.sprite == null || gaugeCanvas == null) return;

        Bounds bounds = cropSpriteRenderer.bounds;
        float topY = bounds.max.y;
        Vector3 newPos = gaugeCanvas.transform.position;
        newPos.y = topY + uiOffsetY;
        gaugeCanvas.transform.position = newPos;
    }

    private void OnTimePassed()
    {
        UpdateVisuals();
        UpdatePrompt();
    }
}
