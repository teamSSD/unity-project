using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuisineManager : MonoBehaviour
{
    public GameObject ingredientPrefab;
    public List<IngredientData> tempInventory;
    public Vector2 ingredientStartPosition;
    public float spacingX = 0.6f;
    public float spacingY = 0.6f;
    [Range(1, 30)] public int perRow = 999;
    void Start()
    {
        generateIngredinets();
    }

    void Update()
    {

    }

    void generateIngredinets()
    {
        if (ingredientPrefab == null || tempInventory == null) return;

        for (int i = 0; i < tempInventory.Count; i++)
        {
            generateIngredient(tempInventory[i], i);
        }
    }

    void generateIngredient(IngredientData data, int idx)
    {
        int row = idx / perRow;
        int col = idx % perRow;

        Vector3 pos = new Vector3(
            ingredientStartPosition.x + col * spacingX,
            ingredientStartPosition.y - row * spacingY,
            0f
        );

        GameObject generated = Instantiate(ingredientPrefab, pos, Quaternion.identity, transform);
        Ingredient ingredient = generated.GetComponent<Ingredient>();
        ingredient.ingredientData = data;
        ingredient.defaultPosition = pos;
    }
}
