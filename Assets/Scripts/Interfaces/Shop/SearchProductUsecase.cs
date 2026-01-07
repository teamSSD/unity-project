using System.Collections.Generic;

public interface SearchProductUsecase
{
    ProductData Search(string id);
    List<ProductData> GetSpecial();
    List<ProductData> GetGeneral();
}