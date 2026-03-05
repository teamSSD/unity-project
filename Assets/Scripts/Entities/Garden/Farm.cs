using TMPro;
using UnityEngine;

public class Farm : MonoBehaviour
{
    public CropData cropData;
    public TextMeshProUGUI actionPrompt;
    public int farmIndex;
    private FarmTile tile;
    private TimePhaseProvider phaseProvider;

    private bool playerIn = false;
    public bool IsLocked => farmIndex >= FarmUpgradeManager.Instance.GetCurrentTileCount();

    private void Start()
    {
        phaseProvider = TempTimePhaseProvider.Instance;
        tile = new FarmTile(phaseProvider);

        UpdatePrompt();
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

            UpdatePrompt();
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
}
