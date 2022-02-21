using System;
using System.Collections.Generic;
using System.Linq;
using AmadarePlugin.Loadouts;
using GridEditor;
using NUnit.Framework;
using static PlayerInventory.ContainerID;
using Container = PlayerInventory.ContainerID;
using Item = GridEditor.FTK_itembase.ID;

namespace AmadarePlugin.Tests;


// public enum ContainerID
// {
//     LeftHand,
//     RightHand,
//     Head,
//     Body,
//     Foot,
//     Trinket,
//     Belt,
//     Backpack,
//     Neck,
// }

[TestFixture]
public class UnitTest1
{
    [Test]
    public void TestMethod1()
    {
        var sut = new InventoryCalculator();
        var ctx = new TestContextFactory()
            .AddItem("left 10 oh e")
            .AddItem("right -5 oh e")
            .AddItem("right 5 th")
            .AddItem("head 0 e")
            .AddItem("foot 0 e")
            .AddItem("tri 0 e")
            .AddItem("nec 0 e")
            .Build();
        
        sut.Calculate(ctx);
        
        foreach (var pair in ctx.Loadout)
        {
            Console.WriteLine($"{pair.Key:G}: {(int)pair.Value}");
        }
    }
}

public class TestContextFactory
{
    public NullContainerFactory NullFactory { get; set; }
    private List<IItemContainer> EquippedContainers = new();
    private List<IItemContainer> AllContainers = new();
    private int itemCounter = 0;

    public TestContextFactory()
    {
        this.NullFactory = new NullContainerFactory();
    }

    public TestContextFactory AddItem(string query)
    {
        Container ParseContainer(string part)
        {
            if (part == "left")
            {
                return LeftHand;
            }

            if (part == "right")
            {
                return RightHand;
            }

            return Enum.GetValues(typeof(Container))
                .Cast<Container>()
                .First(c => c.ToString("G").StartsWith(part, StringComparison.InvariantCultureIgnoreCase));
        }
        
        var queryParts = query.Split();
        var ct = ParseContainer(queryParts[0]);
        var score = int.Parse(queryParts[1]);
        var eqipped = queryParts.Skip(2).Any(p => p == "e");
        var oh = queryParts.Skip(2).Any(p => p == "oh");
        var th = queryParts.Skip(2).Any(p => p == "th");
        var id = ++this.itemCounter;

        var handingnesStr = $"{(oh ? "One Handed" : "")} {(th ? "Two Handed" : "")}".PadLeft(11);
        Console.WriteLine($"ITEM {id}: {ct,-9:G} {score,3} {handingnesStr}{(eqipped ? "Equipped" : ""),-8}");
        
        return AddItem(ct, score, eqipped, oh, th, (FTK_itembase.ID)id);
    }

    public TestContextFactory AddItem(PlayerInventory.ContainerID containerID, float score, bool isEquipped, bool isOneHanded = false, bool isTwoHanded = false, FTK_itembase.ID itemId = default)
    {
        var item = new BaseItemContainer
        {
            Score = score,
            IsEmpty = false,
            ContainerId = containerID,
            ItemId = itemId,
            IsOneHanded = isOneHanded,
            IsTwoHanded = isTwoHanded
        };
        item.Items = new[] { item };
        item.Left = containerID == LeftHand
            ? item
            : this.NullFactory.GetNullContainerFor(LeftHand);
        item.Right = containerID == RightHand
            ? item
            : this.NullFactory.GetNullContainerFor(RightHand);
        this.AllContainers.Add(item);
        if (isEquipped)
        {
            this.EquippedContainers.Add(item);
        }

        return this;
    }

    public HandsCalculatorContext Build()
    {
        return new HandsCalculatorContext(new NullContainerFactory(), this.AllContainers, this.EquippedContainers);
    }
}