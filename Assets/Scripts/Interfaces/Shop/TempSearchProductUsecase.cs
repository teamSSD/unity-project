using System.Collections.Generic;
using UnityEngine;

public class TempSearchProductUsecase : SearchProductUsecase
{
    List<ProductData> productDatas;

    public TempSearchProductUsecase()
    {
        productDatas = CsvModelConverter.Parse<ProductData>("driveAssets/dataTables/store");
    }

    /* null을 반환할 수 있음*/
    public ProductData Search(string id)
    {
        return productDatas.Find(productData => productData.id == id);
    }

    public List<ProductData> GetSpecial()
    {
        return productDatas.FindAll(productDatas => productDatas.type == ProductType.Special);
    }
    public List<ProductData> GetGeneral()
    {
        return productDatas.FindAll(productDatas => productDatas.type == ProductType.General);
    }
}
