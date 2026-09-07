using NUnit.Framework;

public class SettingsEscapePolicyTest
{
    [Test]
    public void Escape_OpensSettingsWhenNoUiIsOpenAndBrowserIsNotFullscreen()
    {
        Assert.IsTrue(SettingsUIManager.ShouldOpenOnEscape(
            isAnyUiLocked: false,
            wasUiLocked: false,
            isGameStart: false,
            isBrowserFullscreen: false));
    }

    [Test]
    public void Escape_DoesNotOpenSettingsWhenBrowserIsFullscreen()
    {
        Assert.IsFalse(SettingsUIManager.ShouldOpenOnEscape(
            isAnyUiLocked: false,
            wasUiLocked: false,
            isGameStart: false,
            isBrowserFullscreen: true));
    }

    [Test]
    public void Escape_DoesNotOpenSettingsAfterAnotherUiConsumedIt()
    {
        Assert.IsFalse(SettingsUIManager.ShouldOpenOnEscape(
            isAnyUiLocked: false,
            wasUiLocked: true,
            isGameStart: false,
            isBrowserFullscreen: false));
    }
}
