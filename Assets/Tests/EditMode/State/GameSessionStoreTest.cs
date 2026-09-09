using System;
using Game.Domain.Common;
using Game.Schema.State;
using NUnit.Framework;

public class GameSessionStoreTest
{
    [Test]
    public void NewGameFactory_CreatesCompleteIndependentStates()
    {
        var first = NewGameStateFactory.Create();
        var second = NewGameStateFactory.Create();

        Assert.IsNotNull(first.stats);
        Assert.IsNotNull(first.phase);
        Assert.IsNotNull(first.garden);
        Assert.IsNotNull(first.shop);
        Assert.IsNotNull(first.mall);
        Assert.IsNotNull(first.mall.session);
        Assert.IsNotNull(first.inventory);
        Assert.IsNotNull(first.menuSelection);
        Assert.IsNotNull(first.unlockedFood);
        Assert.AreNotSame(first, second);
        Assert.AreNotSame(first.inventory, second.inventory);
        Assert.AreNotSame(first.mall.session, second.mall.session);
        Assert.AreNotSame(first.menuSelection, second.menuSelection);
        Assert.AreNotSame(first.unlockedFood, second.unlockedFood);
    }

    [Test]
    public void Constructor_UsesProvidedStateAsSingleOwnedInstance()
    {
        var state = new GameState();
        var store = new GameSessionStore(state);

        Assert.AreSame(state, store.State);
    }

    [Test]
    public void Constructor_RejectsMissingState()
    {
        Assert.Throws<ArgumentNullException>(() => new GameSessionStore(null));
    }
}
