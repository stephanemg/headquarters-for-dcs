﻿using System;
using System.Collections.Generic;

namespace Headquarters4DCS.GeneratedMission
{
    public struct MissionScript
    {
        public readonly string LuaFile;
        public readonly FeatureScriptScope ScriptScope;
        public readonly Dictionary<string, string> Replacements;

        public MissionScript(string luaFile, FeatureScriptScope scriptScope, params string[] replacements)
        {
            LuaFile = luaFile;
            ScriptScope = scriptScope;
            Replacements = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < replacements.Length - 1; i += 2)
            {
                if (Replacements.ContainsKey(replacements[i])) continue;
                Replacements.Add(replacements[i], replacements[i + 1]);
            }
        }
    }
}
