using System;
using FullSerializer;
using fsSerializer = On.FullSerializer.fsSerializer;

namespace AmadarePlugin.InventoryPresets;

public class GameSaveInterceptor
{
    private readonly LoadoutRepository repository;
    private bool isLoadingSave = false;

    public GameSaveInterceptor(LoadoutRepository repository)
    {
        this.repository = repository;

        On.uiStartGame.LoadGame += (orig, self, finished) =>
        {
            this.isLoadingSave = true;
            Plugin.Log.LogInfo("In loading game state");
            try
            {
                orig(self, finished);
            }
            finally
            {
                this.isLoadingSave = false;
                Plugin.Log.LogInfo("Exiting loading game state");
            }
        };
        
        On.FullSerializer.fsSerializer.TrySerialize += FsSerializerOnTrySerialize;
        On.FullSerializer.fsSerializer.TryDeserialize += FsSerializerOnTryDeserialize;
    }

    private fsResult FsSerializerOnTryDeserialize(fsSerializer.orig_TryDeserialize orig, FullSerializer.fsSerializer self, fsData data, Type storagetype, ref object result)
    {
        if (storagetype == typeof(GameSerialize))
        {
            var res = orig(self, data, storagetype, ref result);
            if (res.Succeeded)
            {
                if (!this.isLoadingSave)
                {
                    Plugin.Log.LogInfo("Loading loadouts prevented - not in loading game mode");
                    return res;
                }
                
                // try to fetch loadout info each time game state is deserialized
                if (data.AsDictionary.TryGetValue("m_Loadouts", out var entry))
                {
                    var asString = entry.AsString;
                    Plugin.Log.LogInfo("Loading loadouts info " + asString);
                    this.repository.Load(asString);
                }
                else
                {
                    Plugin.Log.LogInfo("Save doesn't have loadout info");
                }
            }

            return res;
        }
        return orig(self, data, storagetype, ref result);
    }

    private fsResult FsSerializerOnTrySerialize(fsSerializer.orig_TrySerialize orig, FullSerializer.fsSerializer self, Type storagetype, object instance, out fsData data)
    {
        // adding loadout info each time game state is serialized
        if (instance is GameSerialize)
        {
            var result = orig(self, storagetype, instance, out data);
            var serialize = this.repository.Serialize();
            data.AsDictionary["m_Loadouts"] = new fsData(serialize);
            Plugin.Log.LogInfo("Saved loadouts info: " + serialize);
            return result;
        }
        
        return orig(self, storagetype, instance, out data);
    }
}