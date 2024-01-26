using System;
using System.Collections;
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
using TwitchChaosWithoutTwitch.Patches;
using System.IO;
 
namespace TwitchChaosWithoutTwitch
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class NoMoreTwitch : BaseUnityPlugin
    {
        public const string modGUID = "grug.lethalcompany.TwitchChaosNoTwitch";
        public const string modName = "TwitchChaosNoTwitch";
        public const string modVersion = "0.1.0.0";
        //logger.loginfo(""); to log
        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource mls;
        public static NoMoreTwitch? instance;
        public static GameObject? chaosHolder;
        public static List<EnemyAI> objects = new List<EnemyAI>();
        public static GameObject? chaosContainer;
        public static bool configShowAllDebug = true;
        public static AssetBundle assetBundle = AssetBundle.LoadFromMemory(Properties.Resources.forcefield);
        public static GameObject forcefieldPrefab = assetBundle.LoadAsset<GameObject>("Assets/Sphere.prefab");
        public GameObject? despawnableobject;
        
        private (uint, uint, uint, uint) QuadHash(int SALT = 0)
        { // [!code ++]
            Hash128 longHash = new Hash128();
            longHash.Append(modGUID);
            longHash.Append(SALT);
            return ((uint)longHash.u64_0, (uint)(longHash.u64_0 >> 32),
                    (uint)longHash.u64_1, (uint)(longHash.u64_1 >> 32));
        }
        public void Awake()
        {
            mls = base.Logger;
            if(forcefieldPrefab == null)
            {
                mls.LogInfo("forcefield is null");
            }
            foreach (Type type in
                Assembly.GetAssembly(typeof(EnemyAI)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(EnemyAI))))
            {
                objects.Add((EnemyAI)Activator.CreateInstance(type));
            }

            //balls = assetBundle.LoadAsset<Shader>("assets/forcefield");
            //netValidator = new NetcodeValidator(modGUID);
            //netValidator.PatchAll();
            // credits so far
            // evaisa, day, robin, albinogeek, xilo, nomnom, noop
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                DestroyImmediate(this);
            }
            if (chaosContainer == null)
            {
                chaosContainer = new GameObject("disabled") { hideFlags = HideFlags.HideAndDontSave };
                chaosContainer.SetActive(false);
            }
            if (chaosHolder == null)
            {
                chaosHolder = new GameObject("ChaosHolder");
                chaosHolder.transform.SetParent(chaosContainer.transform);
                chaosHolder.hideFlags = HideFlags.HideAndDontSave;
                //DontDestroyOnLoad(chaosHolder);
                chaosHolder.AddComponent<ChaosManager>();
                chaosHolder.AddComponent<EnemyEventManager>();
                chaosHolder.AddComponent<MoonEventManager>();
                chaosHolder.AddComponent<LandingEventManager>();
                chaosHolder.AddComponent<PlayersEventManager>();
                chaosHolder.AddComponent<NetworkObject>();
                var (hash, _, _, _) = QuadHash(0);
                chaosHolder.GetComponent<NetworkObject>().GlobalObjectIdHash = hash;
            }
            // thank you AlbinoGeek
            On.HUDManager.SubmitChat_performed += SubmitChatPatch;
            //mostly me here
            On.EnemyAI.Start += EnemyAI_Start;
            On.EnemyAI.OnDestroy += EnemyAI_OnDestroy;

            On.GameNetcodeStuff.PlayerControllerB.DamagePlayer += PlayerControllerB_DamagePlayer;
            On.GameNetcodeStuff.PlayerControllerB.KillPlayer += PlayerControllerB_KillPlayer;
            On.GameNetcodeStuff.PlayerControllerB.Start += PlayerControllerB_Start;
            On.GameNetcodeStuff.PlayerControllerB.AllowPlayerDeath += PlayerControllerB_AllowPlayerDeath;

            On.RoundManager.DetectElevatorIsRunning += RoundManager_DetectElevatorIsRunning;
            On.StartOfRound.openingDoorsSequence += StartOfRound_openingDoorsSequence;

            On.GameNetworkManager.Start += GameNetworkManager_Start;
            On.StartOfRound.Awake += StartOfRound_Awake;
            harmony.PatchAll(typeof(OnCollideWithPlayerPatch));


            NetcodePatcher();
        }

        private IEnumerator StartOfRound_openingDoorsSequence(On.StartOfRound.orig_openingDoorsSequence orig, StartOfRound self)
        {
            TimeOfDay.Instance.globalTimeSpeedMultiplier = 1f;
            PlayersEventManager.Instance.ResetStatsClientRpc();
            MoonEventManager.Instance.doBrokenForceField = false;
            LandingEventManager.Instance.deathFieldOn = false;
            LandingEventManager.Instance.alreadyLandEvented = false;
            DestroyImmediate(LandingEventManager.Instance.deathfieldObject);
            EnemyEventManager.Instance.enemiesSpawnExplosions = false;
            return orig(self);
        }

        private void RoundManager_DetectElevatorIsRunning(On.RoundManager.orig_DetectElevatorIsRunning orig, RoundManager self)
        {
            orig(self);
            MoonEventManager.Instance.doBrokenForceField = false;
            if (MoonEventManager.Instance.scaleCoroutine != null)
            {
                StopCoroutine(MoonEventManager.Instance.scaleCoroutine);
            }
        }
        private bool PlayerControllerB_AllowPlayerDeath(On.GameNetcodeStuff.PlayerControllerB.orig_AllowPlayerDeath orig, PlayerControllerB self)
        {
            if (self.TryGetComponent(out PlayerStatModifiers mods))
            {
                if (mods.immortal) return false;
            }
            return orig(self);
        }

        private void PlayerControllerB_Start(On.GameNetcodeStuff.PlayerControllerB.orig_Start orig, PlayerControllerB self)
        {
            orig(self);
            self.gameObject.AddComponent<PlayerStatModifiers>();
        }

        private void PlayerControllerB_KillPlayer(On.GameNetcodeStuff.PlayerControllerB.orig_KillPlayer orig, PlayerControllerB self, Vector3 bodyVelocity, bool spawnBody, CauseOfDeath causeOfDeath, int deathAnimation)
        {
            orig(self, bodyVelocity, spawnBody, causeOfDeath, deathAnimation);
            if (EnemyEventManager.Instance != null)
            {
                if (EnemyEventManager.Instance.enemiesSpawnExplosions && causeOfDeath != CauseOfDeath.Bludgeoning && causeOfDeath != CauseOfDeath.Gravity)
                {
                    ChaosManager.Instance.SpawnExplosionIn5SecondsClientRpc(self.transform.position);
                }
            }
        }

        private void PlayerControllerB_DamagePlayer(On.GameNetcodeStuff.PlayerControllerB.orig_DamagePlayer orig, PlayerControllerB self, int damageNumber, bool hasDamageSFX, bool callRPC, CauseOfDeath causeOfDeath, int deathAnimation, bool fallDamage, Vector3 force)
        {
            orig(self, damageNumber, hasDamageSFX, callRPC, causeOfDeath, deathAnimation, fallDamage, force);
            if (EnemyEventManager.Instance != null)
            {
                if (self.AllowPlayerDeath() 
                    && EnemyEventManager.Instance.enemiesSpawnExplosions 
                    && !fallDamage 
                    && causeOfDeath != CauseOfDeath.Bludgeoning
                    && !self.GetComponent<PlayerStatModifiers>().immortal)
                {
                    ChaosManager.Instance.SpawnExplosionIn5SecondsClientRpc(self.transform.position);
                }
            }
        }

        private void EnemyAI_Start(On.EnemyAI.orig_Start orig, EnemyAI self)
        {
            orig(self);
            self.gameObject.AddComponent<EnemyStatModifiers>();
            if (EnemyEventManager.Instance != null)
            {
                EnemyEventManager.Instance.spawnedEnemies.Add(self);
            }
        }
        private void EnemyAI_OnDestroy(On.EnemyAI.orig_OnDestroy orig, EnemyAI self)
        {
            if (EnemyEventManager.Instance != null)
            {
                EnemyEventManager.Instance.spawnedEnemies.Remove(self);
            }
            orig(self);
        }

        private void StartOfRound_Awake(On.StartOfRound.orig_Awake orig, StartOfRound self)
        {
            orig(self);
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (chaosHolder != null)
                {
                    despawnableobject = Instantiate(chaosHolder);
                    if (despawnableobject.TryGetComponent(out NetworkObject networke))
                    {
                        if (!networke.IsSpawned)
                        {
                            networke.Spawn();
                            mls.LogInfo("Spawning object");
                        }
                    }
                    
                }
            }
        }

        private void GameNetworkManager_Start(On.GameNetworkManager.orig_Start orig, GameNetworkManager self)
        {
            orig(self);
            NetworkManager.Singleton.AddNetworkPrefab(chaosHolder);
        }
        private void SubmitChatPatch(On.HUDManager.orig_SubmitChat_performed orig, HUDManager self, UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            string[] chatMessage = self.chatTextField.text.Split(' ');
            if (chatMessage[0] == "/chc" && chatMessage.Length == 3 && self.IsHost)
            {
                //if (chatMessage[1] == "c") ChaosManager.Instance.PickEventByName(chatMessage[2]);
                //if (chatMessage[1] == "s") ChaosManager.NetworkDisplayTip(chatMessage[2].Split('*')[0], chatMessage[2].Split('*')[1]);
                switch (chatMessage[1])
                {
                    case "c":
                        ChaosManager.Instance.PickEventByName(chatMessage[2]);
                        break;
                    case "s":
                        if (chatMessage[2].Split('*').Length == 2)
                        {
                            ChaosManager.NetworkDisplayTip(chatMessage[2].Replace('|',' ').Split('*')[0], chatMessage[2].Replace('|',' ').Split('*')[1]);
                        }
                        else
                        {
                            HUDManager.Instance.DisplayTip("Improper string format!","Proper string format is /chc s Header*Body with spaces being the '|' character");
                        }
                        break;
                    default:
                        HUDManager.Instance.DisplayTip("Unknown command!", "Acceptable commands are to call(/chc c) and to display a tip(/chc s head*body)");
                        break;
                }
                self.localPlayer.isTypingChat = false;
                self.chatTextField.text = "";
                self.typingIndicator.enabled = false;
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }
            else if(chatMessage.Length != 3 && chatMessage[0] == "/chc" && self.localPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.DisplayTip("Improper command entered!", "Make sure its written as below:\n/chc c [event] or /chc s Header|s|p|a|c|e|d|o|u|t*Body");
                self.localPlayer.isTypingChat = false;
                self.chatTextField.text = "";
                self.typingIndicator.enabled = false;
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }
            orig(self, context);
        }
        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
