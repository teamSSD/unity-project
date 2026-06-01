using Game.Domain.Mall;
using Game.Schema.State.Mall;
using NUnit.Framework;

public class DeliveryQuestServiceTest
{
    private MallPersistent state;
    private DeliveryQuestService svc;

    [SetUp]
    public void Setup()
    {
        state = new MallPersistent();
        svc = new DeliveryQuestService(state);
    }

    [Test]
    public void GetStage_UnknownGroup_ReturnsFirstMeet()
    {
        Assert.AreEqual(DeliveryQuestStage.FirstMeet, svc.GetStage("npc_unknown"));
    }

    [Test]
    public void SetStage_AddsNewGroup()
    {
        svc.SetStage("npc_a", DeliveryQuestStage.QuestStart);
        Assert.AreEqual(DeliveryQuestStage.QuestStart, svc.GetStage("npc_a"));
    }

    [Test]
    public void SetStage_UpdatesExistingGroup()
    {
        svc.SetStage("npc_a", DeliveryQuestStage.Normal);
        svc.SetStage("npc_a", DeliveryQuestStage.Completed);
        Assert.AreEqual(DeliveryQuestStage.Completed, svc.GetStage("npc_a"));

        // Add 가 아닌 Update — 1개만 잔존
        Assert.AreEqual(1, state.questGroupIds.Count);
    }

    [Test]
    public void Clear_RemovesAllStages()
    {
        svc.SetStage("a", DeliveryQuestStage.Ordering);
        svc.SetStage("b", DeliveryQuestStage.OrderEnd);

        svc.Clear();

        Assert.AreEqual(DeliveryQuestStage.FirstMeet, svc.GetStage("a"));
        Assert.AreEqual(0, state.questGroupIds.Count);
    }

    [Test]
    public void State_PersistsAcrossServiceInstances()
    {
        svc.SetStage("npc_x", DeliveryQuestStage.Ordering);

        var svc2 = new DeliveryQuestService(state);
        Assert.AreEqual(DeliveryQuestStage.Ordering, svc2.GetStage("npc_x"));
    }
}
