using TMPro;
using UnityEngine;

public class Farm : MonoBehaviour
{
    public CropData cropData;
    public TextMeshProUGUI actionPrompt;
    private FarmTile tile;
    private TimePhaseProvider phaseProvider;

    private bool playerIn = false;

    private void Start()
    {
        phaseProvider = new TempTimePhaseProvider();
        tile = new FarmTile(phaseProvider);

        UpdatePrompt();
    }
    void Update()
    {
        if (playerIn && Input.GetKeyDown(KeyCode.Space))
        {
            if (tile.IsHarvestable())
            {
                tile.Harvest();
                Debug.Log("Crop harvested!");
            }
            else
            {
                tile.Plant(cropData);
                Debug.Log("Crop planted!");
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

        if (tile.IsHarvestable())
            actionPrompt.text = "Press [Space] to Harvest";
        else if (tile.IsEmpty())
            actionPrompt.text = "Press [Space] to Plant";
        else
            actionPrompt.text = "";
    }

    public void NextPhase()
    {
        phaseProvider.NextPhase();
        Debug.Log("Next Phase");
    }
}
