using Game.Domain.Garden;
using NUnit.Framework;

public class CropCatalogServiceTest
{
    private static CropData Crop(string id, float weight = 1f)
    {
        return new CropData { cropId = id, spawnWeight = weight, growPhaseCount = 3 };
    }

    [Test]
    public void GetCropById_Found()
    {
        var svc = new CropCatalogService(new[] { Crop("C001"), Crop("C002") });
        Assert.AreEqual("C001", svc.GetCropById("C001").cropId);
        Assert.AreEqual("C002", svc.GetCropById("C002").cropId);
    }

    [Test]
    public void GetCropById_UnknownReturnsNull()
    {
        var svc = new CropCatalogService(new[] { Crop("C001") });
        Assert.IsNull(svc.GetCropById("MISSING"));
    }

    [Test]
    public void All_ReturnsAllCrops()
    {
        var svc = new CropCatalogService(new[] { Crop("A"), Crop("B"), Crop("C") });
        Assert.AreEqual(3, svc.All.Count);
    }

    [Test]
    public void EmptyCatalog_ReturnsNullForRandom()
    {
        var svc = new CropCatalogService(System.Array.Empty<CropData>());
        Assert.IsNull(svc.GetRandomCropByWeight());
        Assert.AreEqual(0, svc.All.Count);
    }

    [Test]
    public void GetRandomCropByWeight_NonNullForNonEmpty()
    {
        GameRandom.InitSession(1, 1);
        GameRandom.InitDay(0);
        var svc = new CropCatalogService(new[] { Crop("X", 1f), Crop("Y", 1f) });
        var picked = svc.GetRandomCropByWeight();
        Assert.IsNotNull(picked);
        Assert.IsTrue(picked.cropId == "X" || picked.cropId == "Y");
    }
}
