using Dalamud.Configuration;

namespace LeveUp;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    public static bool Automatic { get; set; }  = true;
    public static bool LargeLeves { get; set; }
    public static bool SingleLeveMode { get; set; }

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
