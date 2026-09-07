using NUnit.Framework;

public class SettingsEscapePolicyTest
{
    [Test]
    public void Escape_IsNotHandledWhenSettingsAreClosed()
    {
        Assert.IsFalse(SettingsUIManager.ShouldHandleEscape(isSettingsOpen: false));
    }

    [Test]
    public void Escape_ClosesOpenSettings()
    {
        Assert.IsTrue(SettingsUIManager.ShouldHandleEscape(isSettingsOpen: true));
    }
}
