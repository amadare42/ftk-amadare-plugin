using System.Collections.Generic;
using AmadarePlugin.Common;
using AmadarePlugin.Saving;
using Newtonsoft.Json;

namespace AmadarePlugin.Features.Loadouts;

public class CharacterShareTracker
{
    /// <summary>
    /// Custom key to store share dictionary in save file
    /// </summary>
    public const string ShareDictKey = "m_aqol_shareDict";
    
    /// <summary>
    /// Tracks which characters are having "Share" checkbox enabled.
    /// </summary>
    private Dictionary<string, bool> shareDict = new();

    private HashSet<int> alwaysShareOwners = new(); 

    public CharacterShareTracker(GameSaveInterceptor saveInterceptor)
    {
        saveInterceptor.OnGameSerializing += (sender, args) => args.AddEntry(ShareDictKey, JsonConvert.SerializeObject(this.shareDict));
        saveInterceptor.OnGameDeserializing += (sender, args) =>
        {
            if (args.TryGetEntry(ShareDictKey, out var data))
            {
                this.shareDict = JsonConvert.DeserializeObject<Dictionary<string, bool>>(data.AsString);
            }
            else
            {
                this.shareDict.Clear();
            }
        };
    }

    public void Set(string name, bool shared) => this.shareDict[name] = shared;

    public void SetAlwaysShare(int ownerId, bool value)
    {
        if (value)
        {
            this.alwaysShareOwners.Add(ownerId);
        }
        else
        {
            this.alwaysShareOwners.Remove(ownerId);
        }
    }

    public bool Get(string name)
    {
        var cow = FtkHelpers.FindByUniqueName(name);
        if (cow != null && alwaysShareOwners.Contains(cow.m_PhotonView.ownerId))
        {
            return true;
        }
        
        if (this.shareDict.TryGetValue(name, out var value))
        {
            return value;
        }

        // false by default
        return false;
    }

    public void ClearAll()
    {
        this.shareDict.Clear();
    }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(this.shareDict);
    }

    public void Load(string data)
    {
        this.shareDict = JsonConvert.DeserializeObject<Dictionary<string, bool>>(data);
        if (this.shareDict == null)
        {
            this.shareDict = new();
        }
    }
}