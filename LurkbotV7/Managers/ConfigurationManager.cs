﻿using LurkbotV7.Config;
using Newtonsoft.Json;

namespace LurkbotV7.Managers
{
    public class ConfigurationManager
    {

        public static void Init()
        {

        }

        public static string CONFIGPATH
        {
            get
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "config");
            }
        }

        public static void SaveConfiguration<T>(T input) where T : ModuleConfiguration
        {
            string text = JsonConvert.SerializeObject(input, Formatting.Indented);
            string path = Path.Combine(CONFIGPATH, input.FileName + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, text);
        }

        public static T GetConfiguration<T>() where T : ModuleConfiguration
        {
            T defaultVal = (T)Activator.CreateInstance(typeof(T));
            if (!File.Exists(Path.Combine(CONFIGPATH, defaultVal.FileName + ".json")))
            {
                SaveConfiguration(defaultVal);
                return defaultVal;
            }
            string text = File.ReadAllText(Path.Combine(CONFIGPATH, defaultVal.FileName + ".json"));
            T returnedValue = JsonConvert.DeserializeObject<T>(text);
            if (returnedValue == default(T))
            {
                Log.Warning($"Deserialization failed for config file \"{defaultVal.FileName}\", default returned.");
                return defaultVal;
            }
            //Write config to make sure it updated.
            SaveConfiguration(returnedValue);
            return returnedValue;
        }
    }
}
