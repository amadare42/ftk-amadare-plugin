using System;
using System.Collections.Generic;
using FullSerializer;
using fsSerializer = On.FullSerializer.fsSerializer;

namespace AmadarePlugin.Saving;

public class GameSaveInterceptor
{
    private bool isLoadingSave = false;

    public GameSaveInterceptor()
    {
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
                    Plugin.Log.LogDebug("Loading loadouts prevented - not in loading game mode");
                    return res;
                }
                Plugin.Log.LogDebug("Loading loadouts...");
                this.OnGameDeserializing?.Invoke(this, new GameDeserializingEventArgs(data.AsDictionary));
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
            try
            {
                var dict = new Dictionary<string, string>();
                this.OnGameSerializing?.Invoke(this, new GameSerializingEventArgs(dict));
                foreach (var pair in dict)
                {
                    data.AsDictionary[pair.Key] = new fsData(pair.Value);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error during writing custom save info: {ex}");
            }

            return result;
        }
        
        return orig(self, storagetype, instance, out data);
    }

    public event EventHandler<GameSerializingEventArgs> OnGameSerializing;
    public event EventHandler<GameDeserializingEventArgs> OnGameDeserializing;
}