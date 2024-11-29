using LurkbotV7.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LurkbotV7.Managers
{
    public class ConfigurationManager
    {

        public static void Init()
        {
            YamlSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            YamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

        }

        public static ISerializer YamlSerializer { get; private set; }

        public static IDeserializer YamlDeserializer { get; private set; }

        public const string CONFIGPATH = "./config/";

        public static void SaveConfiguration<T>(T input) where T : ModuleConfiguration
        {
            string text = YamlSerializer.Serialize(input, typeof(T));
            string path = Path.Combine(CONFIGPATH, input.FileName + ".yml");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, text);
        }

        public static T GetConfiguration<T>() where T : ModuleConfiguration
        {
            T defaultVal = (T)Activator.CreateInstance(typeof(T));
            if(!File.Exists(Path.Combine(CONFIGPATH, defaultVal.FileName + ".yml")))
            {
                return defaultVal;
            }
            string text = File.ReadAllText(Path.Combine(CONFIGPATH, defaultVal.FileName + ".yml"));
            T returnedValue = YamlDeserializer.Deserialize<T>(text);
            if(returnedValue == default(T))
            {
                Log.Warning($"Deserialization failed for config file \"{defaultVal.FileName}\", default returned.");
                return defaultVal;
            }
            return returnedValue;
        }
    }
}
