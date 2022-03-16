using System;
using System.Collections.Generic;
using System.Linq;
using AmadarePlugin.Saving;
using GridEditor;
using Newtonsoft.Json;

namespace AmadarePlugin.Features.Loadouts;

public class LoadoutRepository
{
    private Dictionary<string, Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[]> Loadouts = new();

    public LoadoutRepository(GameSaveInterceptor gameSaveInterceptor)
    {
        gameSaveInterceptor.OnGameSerializing += OnGameSerializing;
        gameSaveInterceptor.OnGameDeserializing += OnGameDeserializing;
    }

    private void OnGameDeserializing(object sender, GameDeserializingEventArgs e)
    {
        if (e.TryGetEntry("m_Loadouts", out var data))
        {
            Load(data.AsString);
        }
        else
        {
            ClearAll();
        }
    }

    private void OnGameSerializing(object sender, GameSerializingEventArgs e)
    {
        var serialize = Serialize();
        e.AddEntry("m_Loadouts", serialize);
        Plugin.Log.LogInfo("Saved loadouts info: " + serialize);
    }

    public void Set(string name, int slot, Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> loadout)
    {
        if (!this.Loadouts.TryGetValue(name, out var array))
        {
            array = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[LoadoutManager.SlotsCount];
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
            array = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[LoadoutManager.SlotsCount];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>();
            }
            this.Loadouts[name] = array;
        }

        return array[slot];
    }
    
    public Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[] GetAllSlots(string name)
    {
        if (!this.Loadouts.TryGetValue(name, out var array))
        {
            array = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[LoadoutManager.SlotsCount];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>();
            }
            this.Loadouts[name] = array;
        }

        return array;
    }
    
    public void SetAllSlots(string name, Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[] loadouts)
    {
        for (var i = 0; i < loadouts.Length; i++)
        {
            Set(name, i, loadouts[i]);
        }
    }

    public void Clear(string name, int slot)
    {
        if (!this.Loadouts.TryGetValue(name, out var array))
        {
            return;
        }

        array[slot].Clear();
    }

    public void ClearAll()
    {
        this.Loadouts.Clear();
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
        
        foreach (var key in this.Loadouts.Keys.ToArray())
        {
            var loadout = this.Loadouts[key];
            if (loadout.Length < LoadoutManager.SlotsCount)
            {
                var arr = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[LoadoutManager.SlotsCount];
                loadout.CopyTo(arr, 0);
                this.Loadouts[key] = arr;
                Plugin.Log.LogInfo("Loadout was extended");
            } 
            else if (loadout.Length > LoadoutManager.SlotsCount)
            {
                var arr = new Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[LoadoutManager.SlotsCount];
                Array.Copy(loadout, 0, arr, 0, LoadoutManager.SlotsCount);
                this.Loadouts[key] = arr;
                Plugin.Log.LogInfo("Loadout was trimmed");
            }
        }
    }
}

public class LoadoutDto
{
    public Dictionary<string, Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[]> Loadouts { get; set; } =
        new();
}