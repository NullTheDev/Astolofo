using BepInEx;
using BepInEx.Logging;
using System.IO;

[BepInPlugin("com.astolofo.preloader", "Preloader", "1.0")]
public class Preloader : BaseUnityPlugin
{
    void Awake()
    {
        var path = Paths.BepInExConfigPath;

        if (File.Exists(path))
        {
            var text = File.ReadAllText(path);

            if (text.Contains("Enabled = false"))
            {
                text = text.Replace("Enabled = false", "Enabled = true");
                File.WriteAllText(path, text);
            }
        }
    }
}
