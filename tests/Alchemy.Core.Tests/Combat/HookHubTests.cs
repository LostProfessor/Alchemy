using Alchemy.Core.Combat;
using Alchemy.Core.Entities;
using Alchemy.Core.Hooks;
using Xunit;

namespace Alchemy.Core.Tests.Combat;

/// <summary>钩子中枢的广播健壮性：监听者可能在回调中增删（快照迭代，不应抛"集合已修改"）。</summary>
public class HookHubTests
{
    private sealed class SelfRemoving : IHookListener
    {
        public HookHub? Hub;
        public int Calls;

        public void OnAfterDamage(DamageContext c)
        {
            Calls++;
            Hub?.RemoveListener(this); // 广播中移除自己
        }
    }

    private sealed class Adding : IHookListener
    {
        public HookHub? Hub;
        public IHookListener? ToAdd;
        public int Calls;

        public void OnAfterDamage(DamageContext c)
        {
            Calls++;
            if (ToAdd != null)
            {
                Hub?.AddListener(ToAdd);
                ToAdd = null;
            }
        }
    }

    [Fact]
    public void Broadcast_SurvivesListenerRemovingItself()
    {
        var hub = new HookHub();
        var listener = new SelfRemoving { Hub = hub };
        hub.AddListener(listener);

        hub.RaiseAfterDamage(new DamageContext(null, new Creature("t", 10), 1));

        Assert.Equal(1, listener.Calls);
    }

    [Fact]
    public void Broadcast_SurvivesListenerAddingAnother_NewOneNotCalledThisRound()
    {
        var hub = new HookHub();
        var added = new SelfRemoving { Hub = hub };
        var adder = new Adding { Hub = hub, ToAdd = added };
        hub.AddListener(adder);

        hub.RaiseAfterDamage(new DamageContext(null, new Creature("t", 10), 1));

        Assert.Equal(1, adder.Calls);
        Assert.Equal(0, added.Calls); // 本帧新增者不参与本次广播
    }
}
