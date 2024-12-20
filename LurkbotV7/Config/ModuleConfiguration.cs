﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LurkbotV7.Config
{
    public class ModuleConfiguration
    {
        public ModuleConfiguration() 
        {
            if(FileName == "config" && GetType() != typeof(ModuleConfiguration))
            {
                Log.Warning($"Did you forget to change the default config name in {GetType().FullName}? It is set to \"config\", this may cause issues.");
            }
        }

        public virtual string FileName { get; set; } = "config";
    }
}
