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
        Assert.IsNotNull(first.inventory);
        Assert.AreNotSame(first, second);
        Assert.AreNotSame(first.inventory, second.inventory);
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
