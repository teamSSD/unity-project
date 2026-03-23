using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteStackRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Farm : MonoBehaviour
{
    [Header("설정")]
    public CropData cropData;
    public TextMeshProUGUI actionPrompt;

    [Header("시각적 요소")]
    [Tooltip("작물 이미지를 띄워줄 SpriteRenderer를 연결")]
    public SpriteRenderer cropSpriteRenderer;
    
    public int farmIndex;
    private FarmTile tile;
    private TimePhaseProvider phaseProvider;

    private bool playerIn = false;
    public bool IsLocked => farmIndex >= FarmUpgradeManager.Instance.GetCurrentTileCount();

    private void Start()
    {
        phaseProvider = TempTimePhaseProvider.Instance;
        if (phaseProvider == null)
        {
            Debug.LogError("No TempTimePhaseProvider in this Scene");
        }
        else
        {
            tile = new FarmTile(phaseProvider);

            TempTimePhaseProvider.Instance.OnPhaseChanged += OnTimePassed;
        }

        OnTimePassed();
    }

    private void OnDestroy()
    {
        if (TempTimePhaseProvider.Instance != null)
        {
            TempTimePhaseProvider.Instance.OnPhaseChanged -= OnTimePassed;
        }
    }

    void Update()
    {
        if (playerIn && Input.GetKeyDown(KeyCode.Space))
        {
            if (IsLocked)
            {
                Debug.Log("This Fram is locked!");
            }
            else if (tile.IsEmpty())
            {
                tile.Plant(cropData);
                Debug.Log("Crop planted!");
            }
            else if (tile.IsHarvestable())
            {
                if (tile.Harvest(out string id, out int crops, out int seeds))
                {
                    Debug.Log("Crop harvested!");
                    Debug.Log($"Crop harvested! {crops} [{id}] Crop, {seeds} Seeds get");
                    // TODO. Chain Inventory System
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
        if (other.CompareTag("Player"))
        {
            playerIn = true;
            UpdatePrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIn = false;
            actionPrompt.text = "";
        }
    }

    public void UpdateVisuals()
    {
        if (cropSpriteRenderer == null) return;

        CropData currentCrop = tile.GetCurrentCrop();

        if (currentCrop == null || currentCrop.growthSprites == null || currentCrop.growthSprites.Length == 0)
        {
            cropSpriteRenderer.sprite = null;
            return;
        }

        int passed = tile.GetPassedPhases();

        int maxIndex = currentCrop.growthSprites.Length - 1;
        int spriteIndex = Mathf.Clamp(passed, 0, maxIndex);
        Debug.Log($"[이미지 갱신] 경과 페이즈: {passed} => 표시할 이미지 번호: [{spriteIndex}]");

        cropSpriteRenderer.sprite = currentCrop.growthSprites[spriteIndex];
    }

    private void UpdatePrompt()
    {
        if (!playerIn || actionPrompt == null) return;


        if (IsLocked)
            actionPrompt.text = "This Fram is locked";
        if (tile.IsHarvestable())
            actionPrompt.text = "Press [Space] to Harvest";
        else if (tile.IsEmpty())
            actionPrompt.text = "Press [Space] to Plant";
        else
            actionPrompt.text = "Growing...";
    }

    public void NextPhase()
    {
        phaseProvider.NextPhase();
        Debug.Log("Next Phase");
    }

    private void OnTimePassed()
    {
        UpdateVisuals();
        UpdatePrompt();
    }
}
