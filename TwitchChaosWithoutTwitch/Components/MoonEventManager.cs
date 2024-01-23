using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace TwitchChaosWithoutTwitch.Components
{
    public class MoonEventManager : NetworkBehaviour
    {
        public static MoonEventManager Instance;
        public RoundManager rMInstance;
        public EnemyVent furthestVent;
        public bool doBrokenForceField = false;
        public GameObject brokenForceField;
        public float timeTillDamage = 0f;
        public List<ChaosEvent> moonEvents = new List<ChaosEvent>();
        public Coroutine? scaleCoroutine;
        public void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(Instance);
            }
            Instance = this;

        }
        public void Update()
        {
            if(rMInstance == null && RoundManager.Instance != null) rMInstance = RoundManager.Instance;
            if (StartOfRound.Instance.shipHasLanded && doBrokenForceField) doBrokenForceField = StartOfRound.Instance.shipHasLanded;
            if (doBrokenForceField) DoBrokenForceField();
            if (moonEvents.Count == 0) PopulateMoonEvents();
            if (!StartOfRound.Instance.shipHasLanded && TimeOfDay.Instance.globalTime == 2f) TimeOfDay.Instance.globalTimeSpeedMultiplier = 1f;
            timeTillDamage += Time.deltaTime;
        }
        public void SpawnAllOutsideEnemies()
        {
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("OutsideAINode");
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                rMInstance.SpawnRandomOutsideEnemy(spawnPoints, 17);
            }
            ChaosManager.NetworkDisplayTip("Warning: Multiple Enemies Detected","\nThe outside is a risky place to be.");
        }
        public void DoBrokenForceField()
        {
            if (brokenForceField == null) return;
            foreach(var i in EnemyEventManager.Instance.spawnedEnemies)
            {
                if(i == null) continue;
                if(Vector3.Distance(i.transform.position, furthestVent.transform.position) < brokenForceField.transform.localScale.x/2)
                {
                    i.GetComponent<NetworkObject>().Despawn();
                }
            }
            foreach(var i in StartOfRound.Instance.allPlayerScripts)
            {
                if (i == null) continue;
                if (Vector3.Distance(i.transform.position,furthestVent.transform.position) < brokenForceField.transform.localScale.x/2)
                {
                    if (timeTillDamage >= 1f)
                    {
                        timeTillDamage = 0f;
                        i.DamagePlayerFromOtherClientServerRpc(3, Vector3.zero, 0);
                    }
                }
            }
        }
        [ClientRpc]
        public void BrokenForcefieldSomewhereClientRpc()
        {
            furthestVent = RoundManager.Instance.allEnemyVents
            .OrderByDescending(x => Vector3.Distance(x.transform.position, RoundManager.FindMainEntrancePosition()))
            .First();
            brokenForceField = Instantiate(NoMoreTwitch.forcefieldPrefab);
            brokenForceField.transform.position = furthestVent.transform.position;
            scaleCoroutine = StartCoroutine(ChangeScale(brokenForceField.transform, Vector3.one, Vector3.one * (Vector3.Distance(brokenForceField.transform.position, RoundManager.FindMainEntrancePosition()) * 2),300f));
            doBrokenForceField = true;
            ChaosManager.NetworkDisplayTip("A forcefield has broken","Do not enter the red field. Your suits will protect you for a few seconds.");
            
        }
        public void SpawnAllInsideEnemies()
        {
            foreach(var vent in rMInstance.allEnemyVents)
            {
                vent.spawnTime = 0f;
                if (vent.ventIsOpen) continue;
                rMInstance.AssignRandomEnemyToVent(vent, 0);
                rMInstance.currentMaxInsidePower += 10;
                rMInstance.SpawnEnemyFromVent(vent);
                vent.OpenVentClientRpc();
            }
            ChaosManager.NetworkDisplayTip("Multiple enemies detected","Are you sure that tattered metal sheet is worth it?");
        }
        [ClientRpc]
        public void MakeTimeFasterClientRpc()
        {
            TimeOfDay.Instance.globalTimeSpeedMultiplier = 2f;
            ChaosManager.NetworkDisplayTip("The earth rumbles", "The day is getting shorter!");
        }
        [ClientRpc]
        public void PullFakeApparatusClientRpc()
        {
            StartCoroutine(FakeApparatus());
        }
        public static IEnumerator FakeApparatus()
        {
            ChaosManager.NetworkDisplayTip("WARNING: POWER SURGE DETECTED", "");
            yield return new WaitForSeconds(0.5f);
            RoundManager.Instance.FlickerLights(false, false);
            yield return new WaitForSeconds(0.5f);
            RoundManager.Instance.FlickerLights(false, false);
            yield return new WaitForSeconds(0.5f);
            RoundManager.Instance.FlickerLights(false, false);
            yield return new WaitForSeconds(2.5f);
            RoundManager.Instance.SwitchPower(false);
            RoundManager.Instance.powerOffPermanently = true;
            yield return new WaitForSeconds(3f);
            ChaosManager.NetworkDisplayTip("Facitily diagnostics report:\n" +
                                           "Electric system fried\n" +
                                           "Lights+Doors are broken", "");
            yield break;
        }
        public void RegisterMoonEvent(ChaosEvent chaosEvent)
        {
            if (moonEvents.Contains(chaosEvent)) return;
            moonEvents.Add(chaosEvent);
        }
        public void PopulateMoonEvents()
        {
            RegisterMoonEvent(new ChaosEvent(SpawnAllOutsideEnemies, "AllEnemies", true));
            RegisterMoonEvent(new ChaosEvent(BrokenForcefieldSomewhereClientRpc, "Broken",true));
            RegisterMoonEvent(new ChaosEvent(SpawnAllInsideEnemies, "Quicksand", true));
            RegisterMoonEvent(new ChaosEvent(MakeTimeFasterClientRpc, "FasterTime",true));
            RegisterMoonEvent(new ChaosEvent(PullFakeApparatusClientRpc, "FakeApp",true));
            ChaosManager.listOfAllEvents.AddRange(moonEvents);
        }
        public static IEnumerator ChangeScale(Transform target, Vector3 startScale, Vector3 endScale, float duration)
        {
            var startTime = Time.time;
            target.localScale = startScale;
            while (Time.time - startTime <= duration)
            {
                var progress = (Time.time - startTime) / duration;
                target.localScale = Vector3.Lerp(startScale, endScale, progress);
                yield return null;
            }
            target.localScale = endScale;
        }
    }
}
