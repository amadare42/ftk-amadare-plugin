using System.Collections.Generic;
using System.Linq;
using GridEditor;
using Newtonsoft.Json;
using UnityEngine;

namespace AmadarePlugin.InventoryPresets;

public class LoadoutRepository
{
    private Dictionary<string, Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[]> Loadouts = new();

    public void Set(string name, int slot, Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> loadout)
    {
        if (!this.Loadouts.TryGetValue(name, out var array))
        {
            array = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[InventoryPresetManager.ButtonsCount];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>();
            }
            this.Loadouts[name] = array;
        }

        array[slot] = loadout;
    }

    public bool HasLoadoutAtSlot(string name, int slot)
    {
        if (this.Loadouts.TryGetValue(name, out var array))
        {
            return array[slot].Any();
        }

        return false;
    }

    public Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> Get(string name, int slot)
    {
        if (!this.Loadouts.TryGetValue(name, out var array))
        {
            array = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[InventoryPresetManager.ButtonsCount];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>();
            }
            this.Loadouts[name] = array;
        }

        return array[slot];
    }

    public void Clear(string name, int slot)
    {
        if (!this.Loadouts.TryGetValue(name, out var array))
        {
            return;
        }

        array[slot].Clear();
    }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(new LoadoutDto()
        {
            Loadouts = this.Loadouts
        });
    }

    public void Load(string serialized)
    {
        if (string.IsNullOrEmpty(serialized))
        {
            this.Loadouts = new();
            return;
        }

        this.Loadouts = JsonConvert.DeserializeObject<LoadoutDto>(serialized)?.Loadouts ?? new ();
    }
}

public class LoadoutDto
{
    public Dictionary<string, Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[]> Loadouts { get; set; } =
        new();
}