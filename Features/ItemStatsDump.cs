using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AmadarePlugin.Common;
using BepInEx;
using GridEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AmadarePlugin.Features;

public static class ItemStatsDump
{
    private static void DumpItemsAndMods()
    {
        // AmadarePlugin.Features.ItemStatsDump.DumpItemsAndMods();
        Plugin.Log.LogInfo("Item dump started");
        var arr = CachedDB.ItemToModMap.Select(pair =>
        {
            try
            {
                var item = FTK_itemsDB.GetDB().GetEntry(pair.Key);
                CachedDB.TryGetModById(pair.Value, out var mod);
                return new ItemInfo
                {
                    Item = item,
                    Modifier = mod,
                    EngName = item.GetLocalizedName()
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }).Where(i => i != null).ToArray();
        var path = Path.Combine(Paths.PluginPath, "itemdump.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(arr, Formatting.Indented, new JsonSerializerSettings
        {
            ContractResolver = new IgnoreUnityProps()
        }));
        Plugin.Log.LogInfo("Dump finished");
    }

    public class ItemInfo
    {
        public FTK_itembase Item { get; set; }
        public FTK_characterModifier Modifier { get; set; }
        public string EngName { get; set; }
    }

    public class IgnoreUnityProps : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            Type type = member is PropertyInfo info ? info.PropertyType : ((FieldInfo)member).FieldType;
            if (type.FullName?.Contains("UnityEngine") ?? false)
            {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }
    }
}