using HarmonyLib;
using SFS.Parts;
using Cysharp.Threading.Tasks;
using LogicGateInjector;
using SFS;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace LogicGateModules
{
    public static class GameObjectUtility
    {
        public static Component GetOrAddComponent(this Component origin, Type toAdd)
        {
            var value = origin.GetComponent(toAdd);

            if (!value)
            {
                value = origin.gameObject.AddComponent(toAdd);
            }

            return value;
        }
    }
    public class Patches
    {
        [HarmonyPatch(typeof(CustomAssetsLoader), "LoadAllCustomAssets")]
        public static class CALPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref UniTask __result) 
            {
                __result.GetAwaiter().OnCompleted(() => 
                {
                    ExternalModule exMod;

                    foreach (Part part in Base.partsLoader.parts.Values)
                    {
                        exMod = part.GetComponentInChildren<ExternalModule>();
                        if (exMod)
                        {
                            foreach (var type in GetModules()) 
                            {
                                CustomModuleBase cb = exMod.GetOrAddComponent(type) as CustomModuleBase;
                                if (cb.Id() != exMod.moduleType)
                                {
                                    UnityEngine.Object.Destroy(cb);
                                } else
                                {
                                    cb.vars = exMod.vars;
                                }
                            }
                        }
                    }
                });
            }
            private static Type[] GetModules()
            {
                List<Type> result = new List<Type>();
                Assembly logicModAsm = Assembly.GetExecutingAssembly();
                foreach(Type type in logicModAsm.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(CustomModuleBase)))
                    {
                        result.Add(type);
                    }
                }

                return result.ToArray();
            }
        }
    }
}
