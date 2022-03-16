using System;
using System.Collections.Generic;
using AmadarePlugin.Common;
using AmadarePlugin.Extensions;
using GridEditor;

namespace AmadarePlugin.Features.Loadouts.MaximizeStats;

public class HandsCalculatorContextFactory
{
    private Dictionary<int, BaseItemContainer> itemsCache = new();
    private readonly NullContainerFactory nullFactory = new();

    private static int ItemHash(StatType stat, FTK_itembase.ID itemId)
    {
        return ((int)stat << 20) + (int)itemId;
    }

    public HandsCalculatorContext Create(CharacterOverworld cow,
        List<CharacterOverworld> linkedCows,
        Func<FTK_characterModifier, float> accessor,
        StatType statType
    )
    {
        float GetItemScore(FTK_itembase itembase)
        {
            if (CachedDB.TryGetModByItem(itembase, out var mod, out _))
            {
                return accessor(mod);
            }

            return 0;
        }

        IItemContainer GetContainer(FTK_itembase item, PlayerInventory.ContainerID targetContainer)
        {
            var itemId = item.GetId();
            var hash = ItemHash(statType, itemId);
            if (!this.itemsCache.TryGetValue(hash, out var c))
            {
                c = new BaseItemContainer
                {
                    ContainerId = targetContainer,
                    IsOneHanded = item.IsOneHanded(),
                    IsTwoHanded = item.IsTwoHanded(),
                    Score = GetItemScore(item),
                    IsEmpty = false,
                    ItemId = itemId
                };
                c.Items = new[] { c };
                c.Left = targetContainer == PlayerInventory.ContainerID.LeftHand
                    ? c
                    : this.nullFactory.GetNullContainerFor(PlayerInventory.ContainerID.LeftHand);
                c.Right = targetContainer == PlayerInventory.ContainerID.RightHand
                    ? c
                    : this.nullFactory.GetNullContainerFor(PlayerInventory.ContainerID.RightHand);

                this.itemsCache[hash] = c;
            }

            return c;
        }

        var ctxContainersAll = new List<IItemContainer>(50);
        var ctxContainersEquipped = new List<IItemContainer>(10);

        // own items
        foreach (var pair in cow.m_PlayerInventory.m_Containers)
        {
            var containerKey = pair.Key;
            var container = pair.Value;
            if (container.IsEmpty())
            {
                continue;
            }

            foreach (var itemPairs in container.m_CountDictionary)
            {
                var itemId = itemPairs.Key;
                var item = FTK_itembase.GetItemBase(itemId);
                if (item is not { m_Equippable: true })
                {
                    continue;
                }

                var targetContainer = item.m_ObjectType.GetContainer();
                var c = GetContainer(item, targetContainer);

                ctxContainersAll.Add(c);
                if (containerKey == targetContainer)
                {
                    ctxContainersEquipped.Add(c);
                }
            }
        }

        // linked character items
        foreach (var linkedCow in linkedCows)
        {
            foreach (var pair in linkedCow.m_PlayerInventory.Get(PlayerInventory.ContainerID.Backpack)
                         .m_CountDictionary)
            {
                var itemId = pair.Key;
                var item = FTK_itembase.GetItemBase(itemId);
                if (item is not { m_Equippable: true })
                {
                    continue;
                }

                var targetContainer = item.m_ObjectType.GetContainer();
                var c = GetContainer(item, targetContainer);

                ctxContainersAll.Add(c);
            }
        }

        return new HandsCalculatorContext(this.nullFactory, ctxContainersAll, ctxContainersEquipped);
    }
}