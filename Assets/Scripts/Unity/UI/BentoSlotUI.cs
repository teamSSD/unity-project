using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages a single Bento Slot (e.g., Bento 1).
/// Orchestrates its Main and Side categories and Name input.
/// </summary>
public class BentoSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInput;
    public BentoCategoryUI mainCategory;
    public BentoCategoryUI sideCategory;

    public void Initialize(int slotIndex, System.Action<int, string> onNameChanged)
    {
        if (nameInput != null)
        {
            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener((val) => onNameChanged?.Invoke(slotIndex, val));
        }

        if (mainCategory != null) mainCategory.Initialize();
        if (sideCategory != null) sideCategory.Initialize();
    }

    public void Refresh(MenuSelection menu, FoodData currentMain, List<FoodData> currentSides)
    {
        if (menu == null) return;

        if (nameInput != null && !nameInput.isFocused)
        {
            nameInput.text = menu.Name;
        }

        if (mainCategory != null)
        {
            foreach (var item in mainCategory.Items)
            {
                if (item != null)
                    item.SetSelection(currentMain == item.CurrentFood);
            }
        }

        if (sideCategory != null)
        {
            foreach (var item in sideCategory.Items)
            {
                if (item != null)
                    item.SetSelection(currentSides.Contains(item.CurrentFood));
            }
        }
    }
}
