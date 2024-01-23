using BepInEx;
using BepInEx.Logging;
using ExampleChaosEvent.Managers;
using System;
using UnityEngine;

namespace ExampleChaosEvent
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ExamplePlugin : BaseUnityPlugin
    {
        // Basic essential plugin requirements
        public const string modGUID = "exampleEvents";
        public const string modName = "Examples Examples Examples";
        public const string modVersion = "0.1.0.0";
        public static ManualLogSource? mls;
        // this object is 100% required, as this holds every script you make
        public static GameObject? exampleEventGameObject;
        public void Awake()
        {
            mls = base.Logger;
            // this is required in order to have your events happen in game
            // if your events require networking, you'll need to do more than just this
            // but i won't cover that in this example, you can ask the unofficial modding discord about it
            exampleEventGameObject = new GameObject("eventGameObject");
            // adds the actual eventmanager to our gameobject to ensure it runs
            exampleEventGameObject.AddComponent<ExampleEventManager>();

            // my library uses monomod extensively, don't be afraid to use it a bunch!
            // an example of patching into where i say on line 56 of the exampleeventmanager
            // would look like this on monomod!
            On.StartOfRound.openingDoorsSequence += StartOfRound_openingDoorsSequence;
        }

        private System.Collections.IEnumerator StartOfRound_openingDoorsSequence(On.StartOfRound.orig_openingDoorsSequence orig, StartOfRound self)
        {
            // since its an "IEnumerator" you must return the original instead of calling it
            // here you'd set all of your permanent events to be off
            // this only occurs once the player takes off and lands again
            if(ExampleEventManager.Instance != null) ExampleEventManager.Instance.keepDoingEvent = false;
            return orig(self);
        }
    }
}
