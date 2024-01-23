using BepInEx;
using BepInEx.Logging;
using JetBrains.Annotations;
//using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace TwitchChaosWithoutTwitch.Components
{
    // thank you mama llama | robin

    public class ChaosManager : NetworkBehaviour
    {
        // naming conventions be damned, i use my own
        public static ChaosManager Instance;
        // v Not technically needed, makes code slightly easier
        public LandingEventManager LEMinstance;
        public EnemyEventManager EEMinstance;
        public MoonEventManager MEMinstance;
        public PlayersEventManager PEMinstance;
        // v Not technically needed, makes code slightly easier
        public StartOfRound SOR;
        public bool shipLandedLastFrame = false;
        public float timeTillNextEvent = 0f;
        public float timeBetweenEvents = 60f;
        public static List<ChaosEvent> listOfAllEvents = new List<ChaosEvent>();
        public List<ChaosEvent> timerEvents = new List<ChaosEvent>();

        public void Awake()
        {
            if(Instance != null)
            {
                DestroyImmediate(Instance);
            }
            Instance = this;
        }

        public void Update()
        {
            timeTillNextEvent += Time.deltaTime;
            if (SOR == null && StartOfRound.Instance != null) SOR = StartOfRound.Instance;
            if (LEMinstance == null && LandingEventManager.Instance != null) LEMinstance = LandingEventManager.Instance;
            //if (EEMinstance == null && EnemyEventManager.instance != null) EEMinstance = EnemyEventManager.instance;
            if (SOR != null && LEMinstance != null)
            {
                if (shipLandedLastFrame != SOR.shipHasLanded && SOR.shipHasLanded && SOR.currentLevel.levelID != 3 && IsHost && IsServer)
                {
                    LandingStuff();
                }
                
            }
            if (SOR != null)
            {
                if (!SOR.shipHasLanded || SOR.currentLevel.levelID == 3)
                {
                    timeTillNextEvent = 0f;
                }
            }
            if(timeTillNextEvent > 60f && IsHost && IsServer)
            {
                timeTillNextEvent = 0f;
                timerEvents = listOfAllEvents.Where(x => x.availableForTimer).ToList();
                if(timerEvents.Count != 0)
                {
                    timerEvents[Random.Range(0, timerEvents.Count)].delegatedEvent();
                }
            }
        }
        public void LandingStuff()
        {
            if(LEMinstance == null || SOR == null) return;
            LEMinstance.ChooseRandomEvent();
        }
        public void PickEventByName(string name)
        {
            foreach(var item in listOfAllEvents)
            {
                if(item.name == name)
                {
                    item.delegatedEvent();
                    return;
                }
            }
            HUDManager.Instance.DisplayTip($"Didn't find an event by the name {name}","");
        }
        [ClientRpc]
        public void AllPlayerTipClientRpc(string header, string body)
        {
            HUDManager.Instance.DisplayTip(header, body);
        }
        public static void NetworkDisplayTip(string header, string body)
        {
            //if (NetworkManager.Singleton.IsServer)
            //{
                Instance.AllPlayerTipClientRpc(header, body);
            //}
        }
        [ClientRpc]
        public void SpawnExplosionIn5SecondsClientRpc(Vector3 player)
        {
            Instance.StartCoroutine(DelayedExplosion(player));
        }
        public static IEnumerator DelayedExplosion(Vector3 player)
        {
            for (int i = 6; i > 0; i--)
            {
                yield return new WaitForSeconds(1);
                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.DisplayTip("Explosion in", $"{i - 1}");
                }
            }
            Landmine.SpawnExplosion(player, true, 5, 15);
        }
    }
}
