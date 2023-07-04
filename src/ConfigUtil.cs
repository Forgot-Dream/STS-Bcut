using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using STS_Bcut.src.Common;

namespace STS_Bcut.src
{
    public class ConfigUtil
    {
        public static string ConfigPath = System.IO.Directory.GetCurrentDirectory() + "/sts-bcut-config.json";


        public static Config ReadConfig()
        {
            Config? config = null;
            try
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
            }
            catch(Exception exception)
            {
                Debug.WriteLine(exception.Message);
            }

            config ??= new() { SaveConvertedAudio = false };
            return config;
        }

        public static void WriteConfig(Config config)
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config,Formatting.Indented), Encoding.UTF8);
        }

    }
}
