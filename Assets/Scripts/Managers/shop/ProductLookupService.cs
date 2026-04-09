using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 상품 검색 서비스 (Singleton).
/// TempSearchProductUsecase를 대체.
/// </summary>
public class ProductLookupService : SingletonMonoBehaviour<ProductLookupService>, SearchProductUsecase
{
    private List<ProductData> productDatas;

    public void Initialize()
    {
        productDatas = CsvModelConverter.Parse<ProductData>("driveAssets/dataTables/store");
        Debug.Log($"[ProductLookupService] Loaded {productDatas.Count} products");
    }

    public ProductData Search(string id)
    {
        if (productDatas == null) Initialize();
        return productDatas.Find(p => p.id == id);
    }

    public List<ProductData> GetSpecial()
    {
        if (productDatas == null) Initialize();
        return productDatas.FindAll(p => p.type == ProductType.Special);
    }

    public List<ProductData> GetGeneral()
    {
        if (productDatas == null) Initialize();
        return productDatas.FindAll(p => p.type == ProductType.General);
    }
}
