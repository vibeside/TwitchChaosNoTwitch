using BepInEx;
using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace TwitchChaosWithoutTwitch.Components
{
    public class EnemyEventManager : NetworkBehaviour
    {
        public static EnemyEventManager Instance;
        public RoundManager? roundManagerInstance;
        public List<ChaosEvent> enemyEvents = new List<ChaosEvent>();
        public List<EnemyAI> spawnedEnemies = new List<EnemyAI>();
        public bool enemiesSpawnExplosions = false;
        public NetworkVariable<int> temp = new NetworkVariable<int>();
        public void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(Instance);
            }
            Instance = this;
            temp.Value = Random.Range(0, 2);
        }
        public void Update()
        {
            if (RoundManager.Instance != null) { roundManagerInstance = RoundManager.Instance; }
            if (enemyEvents.Count == 0)
            {
                PopulateEnemyEvents();
            }
        }
        [ClientRpc]
        public void RandomFastEnemyClientRpc()
        {
            temp.Value = Random.Range(0, spawnedEnemies.Count);
            if (spawnedEnemies.Count == 0) return;
            if (spawnedEnemies[temp.Value] != null)
            {
                if (spawnedEnemies[temp.Value].GetComponent<EnemyStatModifiers>() != null)
                {
                    spawnedEnemies[temp.Value].GetComponent<EnemyStatModifiers>().increaseSpeed = true;
                }
            }
            ChaosManager.NetworkDisplayTip($"{spawnedEnemies[temp.Value].enemyType.enemyName} has been made way faster","\nIt must've gotten dem j's");
        }
        public void KillAllEnemies()
        {
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy.GetComponent<NetworkObject>() != null)
                {
                    //if (spawnedEnemies.Contains(enemy))
                    //{
                    //    spawnedEnemies.Remove(enemy);
                    //}
                    enemy.GetComponent<NetworkObject>().Despawn();
                }
            }
            ChaosManager.NetworkDisplayTip("The metaphorical sun rises", "All the 'mobs' are burning!");
        }
        [ClientRpc]
        public void DoLessDamageClientRpc()
        {
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy.GetComponent<EnemyStatModifiers>() != null)
                {
                    if (enemy.GetComponent<EnemyStatModifiers>().currentMod == EnemyStatModifiers.DamageModifiers.None)
                    {
                        enemy.GetComponent<EnemyStatModifiers>().currentMod = EnemyStatModifiers.DamageModifiers.Zero;
                    }
                }
            }
            ChaosManager.NetworkDisplayTip("Some of the enemies have decided to be nicer!", "\nThey won't hurt you at all anymore!");
        }
        [ClientRpc]
        public void DoMoreDamageClientRpc()
        {
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy.GetComponent<EnemyStatModifiers>() != null)
                {
                    if (enemy.GetComponent<EnemyStatModifiers>().currentMod == EnemyStatModifiers.DamageModifiers.None)
                    {
                        enemy.GetComponent<EnemyStatModifiers>().currentMod = EnemyStatModifiers.DamageModifiers.More;
                    }
                }
            }
        }
        [ClientRpc]
        public void SpawnExplosionsOnAttackClientRpc()
        {
            enemiesSpawnExplosions = true;
            ChaosManager.NetworkDisplayTip("WARNING:", "THE ENEMIES HAVE LEANRED GUERILLA WARFARE!");
        }
        public void RegisterEnemyEvent(ChaosEvent chaosEvent)
        {
            if (enemyEvents.Contains(chaosEvent)) return;
            enemyEvents.Add(chaosEvent);
        }
        public void PopulateEnemyEvents()
        {
            RegisterEnemyEvent(new ChaosEvent(RandomFastEnemyClientRpc, "Fast",true));
            RegisterEnemyEvent(new ChaosEvent(DoLessDamageClientRpc, "Less",true));
            RegisterEnemyEvent(new ChaosEvent(KillAllEnemies, "Kill", true));
            RegisterEnemyEvent(new ChaosEvent(DoMoreDamageClientRpc, "More", true));
            RegisterEnemyEvent(new ChaosEvent(SpawnExplosionsOnAttackClientRpc, "Bombs", true));
            ChaosManager.CheckConfig(enemyEvents);
            ChaosManager.listOfAllEvents.AddRange(enemyEvents);
        }
    }
}
