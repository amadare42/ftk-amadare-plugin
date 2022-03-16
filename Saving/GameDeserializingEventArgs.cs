using System;
using System.Collections.Generic;
using FullSerializer;

namespace AmadarePlugin.Saving;

public class GameDeserializingEventArgs : EventArgs
{
    public Dictionary<string, fsData> Dict { get; }

    public GameDeserializingEventArgs(Dictionary<string, fsData> dict)
    {
        this.Dict = dict;
    }

    public bool TryGetEntry(string name, out fsData value)
    {
        return this.Dict.TryGetValue(name, out value);
    }
}