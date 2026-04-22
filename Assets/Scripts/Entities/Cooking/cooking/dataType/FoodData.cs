using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum FoodType
{
    GARBAGE, INGREDIENT, PROCESSING, MAIN, SIDE
}

[CreateAssetMenu(fileName = "NewFoodData", menuName = "Data/Food Data")]
public class FoodData : ScriptableObject, CsvParsable
{
    // Tool ID → 배열 인덱스 매핑
    private static readonly string[] toolSuffixes = { "_pan", "_pot", "_bowl", "_cut", "_plate" };
    private static readonly Dictionary<string, int> toolIdToIndex = new()
    {
        { "T001", 0 }, { "T002", 1 }, { "T003", 2 }, { "T004", 3 }, { "T005", 4 }
    };
    private static readonly string[] bentoSuffixes = { "_bento", "_bento_left", "_bento_middle", "_bento_right" };

    public string id;
    public string ingredientName;
    public string description;
    public Sprite image;
    public IngredientData ingredient;
    public List<string> availableTools = new List<string>();
    public FoodType type;

    [Header("Variant Sprites")]
    public Sprite[] toolVariants = new Sprite[5];    // [0]pan [1]pot [2]bowl [3]cut [4]plate
    public Sprite[] bentoVariants = new Sprite[4];   // [0]bento [1]left [2]middle [3]right
    public Sprite pieceSprite;

    public void Init(string[] args)
    {
        if (args.Length != 6) throw new CsvParsingException("length of args isn't match.");
        try
        {
            id = args[0].Trim();
            ingredientName = args[1].Trim();
            description = args[2].Trim();
            string spritePath = ResourcePaths.Art.Food + args[3].Trim();
            image = Resources.Load<Sprite>(spritePath);
            string ingredientPath = ResourcePaths.SO.IngredientDataById + id;
            ingredient = Resources.Load<IngredientData>(ingredientPath);
            availableTools = new List<string>(args[4].Split('/'));
            type = (FoodType)Enum.Parse(typeof(FoodType), args[5].Trim());

            // Variant 스프라이트 로드
            string baseName = args[3].Trim().Replace("_raw", "");

            // Tool variants
            if (type == FoodType.INGREDIENT)
            {
                foreach (string toolId in availableTools)
                {
                    if (toolIdToIndex.TryGetValue(toolId.Trim(), out int idx))
                        toolVariants[idx] = Resources.Load<Sprite>(ResourcePaths.Art.Food + baseName + toolSuffixes[idx]);
                }
            }
            else
            {
                for (int i = 0; i < 5; i++)
                    toolVariants[i] = Resources.Load<Sprite>(ResourcePaths.Art.Food + baseName + toolSuffixes[i]);
            }

            // Piece variant (커팅 미니게임용)
            if (type == FoodType.INGREDIENT && availableTools.Contains("T004"))
                pieceSprite = Resources.Load<Sprite>(ResourcePaths.Art.Food + baseName + "_piece");

            // Bento variants
            if (type == FoodType.MAIN || type == FoodType.SIDE)
            {
                for (int i = 0; i < 4; i++)
                    bentoVariants[i] = Resources.Load<Sprite>(ResourcePaths.Art.Food + baseName + bentoSuffixes[i]);
            }
        }
        catch (Exception e)
        {
            throw new CsvParsingException($"Exception occured during parsing \nmessage : {e.Message}");
        }
    }

    public Sprite GetRepresentativeBentoImage()
    {
        if (type == FoodType.MAIN)
        {
            return bentoVariants[0] != null ? bentoVariants[0] : image;
        }
        else if (type == FoodType.SIDE)
        {
            for (int i = 1; i < bentoVariants.Length; i++)
            {
                if (bentoVariants[i] != null) return bentoVariants[i];
            }
            return image;
        }
        return image;
    }

    public Sprite GetMainBentoImage()
    {
        if (type != FoodType.MAIN)
        {
            Debug.LogWarning($"[FoodData] {ingredientName}(은)는 MAIN 타입이 아님에도 메인 전용 이미지를 요청받았습니다!");
            return image;
        }
        return bentoVariants[0] != null ? bentoVariants[0] : image;
    }

    public Sprite GetSideBentoImage(int sideOrder)
    {
        if (type != FoodType.SIDE)
        {
            Debug.LogWarning($"[FoodData] {ingredientName}(은)는 SIDE 타입이 아님에도 사이드 전용 이미지를 요청받았습니다!");
            return image;
        }

        int targetIndex = 1 + sideOrder;
        if (targetIndex >= bentoVariants.Length) targetIndex = bentoVariants.Length - 1; // 최대치는 마지막 인덱스
        
        return bentoVariants[targetIndex] != null ? bentoVariants[targetIndex] : image;
    }

    public Sprite GetImageForTool(string toolId)
    {
        if (!toolIdToIndex.TryGetValue(toolId, out int idx))
            return image;
        return toolVariants[idx] != null ? toolVariants[idx] : image;
    }
}
