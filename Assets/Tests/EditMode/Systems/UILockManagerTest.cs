using NUnit.Framework;

public class UILockManagerTest
{
    [SetUp]
    public void Setup()
    {
        // 매 테스트 전 잠금 초기화
        UILockManager.Unlock(UILockManager.Owner.Minigame);
        UILockManager.Unlock(UILockManager.Owner.Dialogue);
        UILockManager.Unlock(UILockManager.Owner.RecipeBook);
        UILockManager.Unlock(UILockManager.Owner.BentoSelection);
        UILockManager.Unlock(UILockManager.Owner.GameStart);
        UILockManager.Unlock(UILockManager.Owner.Loading);
        UILockManager.Unlock(UILockManager.Owner.Settings);
    }

    [Test]
    public void Initially_NotLocked()
    {
        Assert.IsFalse(UILockManager.IsLocked);
    }

    [Test]
    public void Lock_SetsIsLocked()
    {
        UILockManager.Lock(UILockManager.Owner.Dialogue);
        Assert.IsTrue(UILockManager.IsLocked);
        Assert.IsTrue(UILockManager.IsLockedBy(UILockManager.Owner.Dialogue));
    }

    [Test]
    public void Unlock_ClearsLock()
    {
        UILockManager.Lock(UILockManager.Owner.Dialogue);
        UILockManager.Unlock(UILockManager.Owner.Dialogue);
        Assert.IsFalse(UILockManager.IsLocked);
    }

    [Test]
    public void MultipleLocks_AllMustUnlock()
    {
        UILockManager.Lock(UILockManager.Owner.Minigame);
        UILockManager.Lock(UILockManager.Owner.Loading);

        UILockManager.Unlock(UILockManager.Owner.Minigame);
        Assert.IsTrue(UILockManager.IsLocked); // Loading still locked

        UILockManager.Unlock(UILockManager.Owner.Loading);
        Assert.IsFalse(UILockManager.IsLocked);
    }

    [Test]
    public void CanOpen_NoLocks_ReturnsTrue()
    {
        Assert.IsTrue(UILockManager.CanOpen(UILockManager.Owner.RecipeBook));
    }

    [Test]
    public void CanOpen_SelfLocked_ReturnsTrue()
    {
        UILockManager.Lock(UILockManager.Owner.RecipeBook);
        Assert.IsTrue(UILockManager.CanOpen(UILockManager.Owner.RecipeBook));
    }

    [Test]
    public void CanOpen_OtherLocked_ReturnsFalse()
    {
        UILockManager.Lock(UILockManager.Owner.Dialogue);
        Assert.IsFalse(UILockManager.CanOpen(UILockManager.Owner.RecipeBook));
    }

    [Test]
    public void CanOpen_GameStart_BlocksRecipeBook()
    {
        UILockManager.Lock(UILockManager.Owner.GameStart);
        Assert.IsFalse(UILockManager.CanOpen(UILockManager.Owner.RecipeBook));
    }

    [Test]
    public void CanOpen_Settings_WhenOnlyGameStartLocked_ReturnsTrue()
    {
        UILockManager.Lock(UILockManager.Owner.GameStart);
        Assert.IsTrue(UILockManager.CanOpen(UILockManager.Owner.Settings));
    }

    [Test]
    public void CanOpen_Settings_WhenLoadingIsAlsoLocked_ReturnsFalse()
    {
        UILockManager.Lock(UILockManager.Owner.GameStart);
        UILockManager.Lock(UILockManager.Owner.Loading);
        Assert.IsFalse(UILockManager.CanOpen(UILockManager.Owner.Settings));
    }

    [Test]
    public void CanOpen_Minigame_BlocksDialogue()
    {
        UILockManager.Lock(UILockManager.Owner.Minigame);
        Assert.IsFalse(UILockManager.CanOpen(UILockManager.Owner.Dialogue));
    }

    [Test]
    public void DuplicateLock_NoDuplicate()
    {
        UILockManager.Lock(UILockManager.Owner.Dialogue);
        UILockManager.Lock(UILockManager.Owner.Dialogue); // 중복
        UILockManager.Unlock(UILockManager.Owner.Dialogue);
        Assert.IsFalse(UILockManager.IsLocked); // 한 번 해제로 충분
    }
}
