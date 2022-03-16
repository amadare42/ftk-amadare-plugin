using System;
using System.Collections.Generic;

namespace AmadarePlugin.Saving;

public class GameSerializingEventArgs : EventArgs
{
    private Dictionary<string, string> dict;

    public GameSerializingEventArgs(Dictionary<string, string> dict)
    {
        this.dict = dict;
    }

    public GameSerializingEventArgs AddEntry(string key, string value)
    {
        this.dict.Add(key, value);
        return this;
    }
}