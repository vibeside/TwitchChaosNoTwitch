using GameNetcodeStuff;
using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.Netcode;

namespace TwitchChaosWithoutTwitch.Components
{
    public class PlayersEventManager : NetworkBehaviour
    {
        public static PlayersEventManager Instance;
        public List<PlayerControllerB> activePlayers = new List<PlayerControllerB>();
        public List<ChaosEvent> playerEvents = new List<ChaosEvent>();
        NetworkVariable<int> temp = new NetworkVariable<int>();
        public void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(Instance);
            }
            Instance = this;
                temp.Value = Random.Range(0, activePlayers.Count());
        }
        public void Update()
        {
            activePlayers = StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled).ToList();
            if(playerEvents.Count == 0) PopulatePlayerEvents();
        }
        [ClientRpc]
        public void ResetStatsClientRpc()
        {
            foreach(var player in activePlayers)
            {
                if(player.TryGetComponent(out PlayerStatModifiers modifiers))
                {
                    modifiers.immortal = false;
                    modifiers.infiniteSprint = false;
                }
            }
        }
        [ClientRpc]
        public void DrainPlayerSprintClientRpc()
        {
            temp.Value = Random.Range(0, activePlayers.Count());
            activePlayers[temp.Value].sprintMeter = 0f;
            ChaosManager.NetworkDisplayTip($"{activePlayers[temp.Value].playerUsername} is really tired...", "\nBeing on their feet all day and running from monsters is busy and tiring work.");
        }
        [ClientRpc]
        public void TeleportPlayerUpClientRpc()
        {
            temp.Value = Random.Range(0,activePlayers.Count());
            activePlayers[temp.Value].TeleportPlayer(new Vector3(activePlayers[temp.Value].transform.position.x, activePlayers[temp.Value].transform.position.y + 100f, activePlayers[temp.Value].transform.position.z));
            ChaosManager.NetworkDisplayTip("The simulation is breaking", $"{activePlayers[temp.Value].playerUsername}'s bit has been flipped");

        }
        [ClientRpc]
        public void RandomPlayerGetsInfiniteSprintClientRpc()
        {
            temp.Value = Random.Range(0, activePlayers.Count());
            if (activePlayers[temp.Value].TryGetComponent(out PlayerStatModifiers mods))
            {
                mods.infiniteSprint = true;
            }
            ChaosManager.NetworkDisplayTip($"{activePlayers[temp.Value].playerUsername} has infinite sprint", "They must have the runner trait or something...");
        }
        public void Dropitems()
        {
            temp.Value = Random.Range(0,activePlayers.Count());
            activePlayers[temp.Value].DropAllHeldItemsAndSync();
            ChaosManager.NetworkDisplayTip($"{activePlayers[temp.Value].playerUsername} has butterfingers!", "Maybe you shouldn't eat so many chips next time.");

        }
        [ClientRpc]
        public void MakeRandomPlayerImmortalClientRpc()
        {
            temp.Value = Random.Range(0, activePlayers.Count());
            PlayerControllerB player = activePlayers[temp.Value];
            if (player.TryGetComponent(out PlayerStatModifiers mods))
            {
                mods.immortal = true;
            }
            ChaosManager.NetworkDisplayTip($"{activePlayers[temp.Value].playerUsername} has toggled god mode", "\nThey're tired of dying to that damn hoarding bug.");
        }
        public void RegisterPlayerEvent(ChaosEvent chaosEvent)
        {
            if (playerEvents.Contains(chaosEvent)) return;
            playerEvents.Add(chaosEvent);
        }
        public void PopulatePlayerEvents()
        {
            RegisterPlayerEvent(new ChaosEvent(DrainPlayerSprintClientRpc, "NoSprint", true));
            RegisterPlayerEvent(new ChaosEvent(TeleportPlayerUpClientRpc, "Teleport", true));
            RegisterPlayerEvent(new ChaosEvent(RandomPlayerGetsInfiniteSprintClientRpc, "InfiniteSprint", true));
            RegisterPlayerEvent(new ChaosEvent(Dropitems, "Scale", true));
            RegisterPlayerEvent(new ChaosEvent(MakeRandomPlayerImmortalClientRpc, "Immortal", true));
            ChaosManager.Instance.listOfAllEvents.AddRange(playerEvents);
        }
    }
}
