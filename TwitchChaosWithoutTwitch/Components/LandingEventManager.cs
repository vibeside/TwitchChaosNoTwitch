using BepInEx;
using BepInEx.Logging;
using System.CodeDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Diagnostics;
using GameNetcodeStuff;

namespace TwitchChaosWithoutTwitch.Components
{
    public class LandingEventManager : NetworkBehaviour
    {
        public static LandingEventManager Instance;
        public bool deathFieldOn = false;
        public bool alreadyLandEvented = false;
        public StartOfRound SOR;
        public List<ChaosEvent> landingEvents = new List<ChaosEvent>();
        public GameObject? deathfieldObject;
        public float DeathFieldRange = 35f;
        public List<PlayerControllerB> activePlayers = new List<PlayerControllerB>();
        public void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(Instance);
            }
            Instance = this;

        }
        public void Update() {
            if (SOR == null && StartOfRound.Instance != null) SOR = StartOfRound.Instance;
            if(SOR != null && deathFieldOn) deathFieldOn = SOR.shipHasLanded;
            if (deathFieldOn) DeathField();
            if (landingEvents.Count == 0) PopulateLandingEvents();
            //if(UnityInput.Current.GetKeyDown(KeyCode.X)) TurnOnDeathFieldClientRpc();
            //if (UnityInput.Current.GetKeyDown(KeyCode.Z)) deathfieldObject.GetComponent<Renderer>().sharedMaterial.SetFloat("_StartTime", Time.time);
        }
        public void ChooseRandomEvent(int overrideRandom = -1, bool restrictToOnce = true)
        {
            if (alreadyLandEvented && restrictToOnce) return;
            if (overrideRandom == -1)
            {
                overrideRandom = Random.Range(0, landingEvents.Count);
            }
            landingEvents[overrideRandom].delegatedEvent();
            alreadyLandEvented = true;
        }
        public void UnlockUpgrades()    
        {
            if (SOR.localPlayerController.IsHost)
            {
                // all me baby(plus help)
                List<UnlockableItem> validItems = StartOfRound.Instance.unlockablesList.unlockables.Where(un =>
                !un.hasBeenUnlockedByPlayer && !un.alreadyUnlocked).ToList();
                for (int i = 0; i < 5; i++)
                {
                    if (validItems.Count > 0) SOR.UnlockShipObject(SOR.unlockablesList.unlockables.IndexOf(validItems[Random.Range(0, validItems.Count)]));
                    if (Random.Range(0, 2) == 1) break;
                }
                //HUDManager.Instance.DisplayTip("Have some upgrades", "Enjoy decorations, suits, or even upgrades!");
            }
            ChaosManager.NetworkDisplayTip("Have some upgrades!","Enjoy decorations, suits, or even upgrades!");
        }
        public void WalksAndFlashes()
        {
            //thanks dancetools
            int temp = 14;
            for (int j = 0; j < 2; j++)
            {
                //walkie 14
                // pro 9
                // normal 3
                for (int i = 0; i < 4; i++)
                {
                    GameObject val = Instantiate(StartOfRound.Instance.allItemsList.itemsList[temp].spawnPrefab,
                    (SOR.middleOfShipNode).transform.position,
                    Quaternion.identity);
                    val.GetComponent<GrabbableObject>().fallTime = 0f;
                    val.GetComponent<NetworkObject>().Spawn(true);
                    val.AddComponent<ScanNodeProperties>().scrapValue = 0;
                    val.GetComponent<GrabbableObject>().SetScrapValue(0);
                }
                if (Random.Range(0, 2) == 1) { temp = 9; } else { temp = 3; }
            }
            //HUDManager.Instance.DisplayTip("Enjoy some tools!", "There are four walkies and four flashlights in the ship!");
            ChaosManager.NetworkDisplayTip("Enjoy some tools!", "There are four walkies and four flashlights in the ship!");
        }
        public void FiveHundredScrap()
        {
            // clown horn 25
            // thanks dancetools
            for (int i = 0; i < 4; i++)
            {
                GameObject val = Instantiate(StartOfRound.Instance.allItemsList.itemsList[25].spawnPrefab,
                    (SOR.middleOfShipNode).transform.position,
                    Quaternion.identity);
                val.GetComponent<GrabbableObject>().fallTime = 0f;
                val.GetComponent<NetworkObject>().Spawn(false);
                val.GetComponent<GrabbableObject>().SetScrapValue(125);
                RoundManager.Instance.SyncScrapValuesClientRpc(new NetworkObjectReference[] { val.GetComponent<NetworkObject>() }, new[] { 125 });
            }
            //HUDManager.Instance.DisplayTip("Enjoy the clown horns!", "There are four horns on the ship! Make sure you pick them up or they'll despawn!");
            ChaosManager.NetworkDisplayTip("Praise the honkmother!", "There are four clownhorns on the ship!\nIf you dont pick it up, they'll despawn!");
        }
        public void RandomDamagePlayer()
        {
            activePlayers.Clear();
            foreach (var p in SOR.allPlayerScripts)
            {
                if (p.isPlayerControlled)
                {
                    activePlayers.Add(p);
                }
            }
            // thank u gulag
            int cache = Random.Range(1, 50);
            int cache2 = Random.Range(0, activePlayers.Count);
            activePlayers[cache2].DamagePlayerFromOtherClientServerRpc(cache,Vector3.zero,cache2);
            ChaosManager.NetworkDisplayTip("Space debris!",$"One of your suits has been damaged! Sorry!\nSuit integrity:{activePlayers[cache2].health}");
        }
        public void DeathField()
        {
            deathFieldOn = true;
            if (SOR != null && SOR.middleOfShipNode != null && EnemyEventManager.Instance != null)
            {
                foreach(EnemyAI item in EnemyEventManager.Instance.spawnedEnemies)
                {
                    if(item != null)
                    {
                        if (Vector3.Distance(item.transform.position, SOR.middleOfShipNode.transform.position) < (DeathFieldRange/2f))
                        {
                            item.GetComponent<NetworkObject>().Despawn();
                        }
                    }
                }
            }
        }
        [ClientRpc]
        public void TurnOnDeathFieldClientRpc()
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                deathFieldOn = true;
            }
            deathfieldObject = Instantiate(NoMoreTwitch.forcefieldPrefab);
            deathfieldObject.transform.localScale = Vector3.one * DeathFieldRange;
            deathfieldObject.transform.position = SOR.middleOfShipNode.position;
            deathfieldObject.GetComponent<Renderer>().sharedMaterial.SetFloat("_StartTime", Time.time);
            ChaosManager.NetworkDisplayTip("Ship defense activated!","Any enemies that get too close get EVAPORATED");
            //debug code, ignore
            
        }
        public void RegisterLandingEvent(ChaosEvent chaosEvent)
        {
            if (landingEvents.Contains(chaosEvent)) return;
            landingEvents.Add(chaosEvent);
        }
        public void PopulateLandingEvents()
        {
            RegisterLandingEvent(new ChaosEvent(UnlockUpgrades,"Upgrades"));
            RegisterLandingEvent(new ChaosEvent(WalksAndFlashes, "Tools"));
            RegisterLandingEvent(new ChaosEvent(FiveHundredScrap, "Scrap"));
            RegisterLandingEvent(new ChaosEvent(RandomDamagePlayer, "Weaken"));
            RegisterLandingEvent(new ChaosEvent(TurnOnDeathFieldClientRpc, "Death"));
            ChaosManager.listOfAllEvents.AddRange(landingEvents);
        }

    }
}
