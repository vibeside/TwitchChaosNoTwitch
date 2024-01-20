using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TwitchChaosWithoutTwitch.Components;
using Unity.Netcode;
using System.Reflection;
using GameNetcodeStuff;
using UnityEngine.EventSystems;
using BepInEx.Configuration;
using MonoMod;
using MonoMod.RuntimeDetour;

namespace TwitchChaosWithoutTwitch.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal static class OnCollideWithPlayerPatch
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach(var obj in NoMoreTwitch.objects)
            {
                yield return AccessTools.Method(obj.GetType(), "OnCollideWithPlayer");
            }
        }
        //xilo and nomnom came in clutch for this
        [HarmonyPrefix]
        public static bool Prefix(EnemyAI __instance,Collider other)
        {
            
            PlayerControllerB player;
            other.TryGetComponent(out player);
            EnemyStatModifiers modifiers;
            if (__instance.TryGetComponent(out modifiers) && other.TryGetComponent(out player))
            {
                switch (modifiers.currentMod)
                {
                    case EnemyStatModifiers.DamageModifiers.Zero:
                        return false;
                    case EnemyStatModifiers.DamageModifiers.More:
                        player.KillPlayer(Vector3.zero);
                        return false;
                    case EnemyStatModifiers.DamageModifiers.None:
                        break;
                    default:
                        return true;
                }

            }
            return true;
        }
    }

}
