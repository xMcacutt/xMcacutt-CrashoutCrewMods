using System;
using System.Collections.Generic;
using System.IO;

namespace CrashoutCrew_LobbyEnforcer;

public enum EnforcementDirection
{
    Server,  // Server must have mod
    Clients, // Clients must have mod
    Both,    // Both must have mod
}

public class ModEntry
{
    public string modId = "";
    public string modVersion = "";
    public EnforcementDirection direction = EnforcementDirection.Both;
    public string data = "";
    
    public static string SerializeList(List<ModEntry> mods)
    {
        if (mods.Count == 0) 
            return string.Empty;
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);
        writer.Write(mods.Count);
        foreach (var m in mods)
        {
            writer.Write(m.modId);
            writer.Write(m.modVersion);
            writer.Write((int)m.direction);
            writer.Write(m.data);
        }
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public static List<ModEntry> DeserializeList(string base64)
    {
        var result = new List<ModEntry>();
        if (string.IsNullOrEmpty(base64)) 
            return result;
        var bytes = Convert.FromBase64String(base64);
        using var memoryStream = new MemoryStream(bytes);
        using var reader = new BinaryReader(memoryStream);
        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            result.Add(new ModEntry
            {
                modId = reader.ReadString(),
                modVersion = reader.ReadString(),
                direction = (EnforcementDirection)reader.ReadInt32(),
                data = reader.ReadString()
            });
        }
        return result;
    }
}
