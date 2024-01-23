using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TwitchChaosWithoutTwitch.Components;

namespace ExampleChaosEvent.Managers
{
    internal class ExampleEventManager : MonoBehaviour // if using RPCs/networking this needs to be a networkbehaviour
    {
        // you may need to set your script up as a singleton, although this isnt 100% needed
        // it is recommended if you have permanent events though
        public static ExampleEventManager? Instance;

        // if you plan to have any permanent effects put bools here
        public bool keepDoingEvent = false;
        // this is a list of every example event made so far! it can be used for any number of reasons,
        // such as triggering a specific type of event when something happens
        // e.g when the player leaves the ship or an enemy spawns
        public List<ChaosEvent> exampleEvents = new List<ChaosEvent>();
        public void Awake()
        {
            // this gaurantees your instance, or singleton, is always properly set to your manager

            if (Instance != null)
            {
                DestroyImmediate(Instance);
            }
            Instance = this;

            // do any other setup you may need to do, for instance set a networkvariable
            // (which is unavailable due to this class being a monobehaviour) to a random value
        }
        public void Update()
        {
            //if you want to turn this off, set "keepDoingEvent" to false anywhere
            // although i am questioning that, it should work
            if (keepDoingEvent)
            {
                DoPermanentEvent();
            }
        }
        public void NotPermanentEvent()
        {
            // do stuff n things here! at the end put another networked tip describing the event

            // one option vvvv
            ChaosManager.NetworkDisplayTip("There was an event!", "Oh no... you died, sorry");

            // another option is to do a joke or a reference to a game!
            ChaosManager.NetworkDisplayTip("What'd the airplane to the fish?", "nothing, fish cant talk");
        }
        public void DoPermanentEvent()
        {
            // calling "dopermanentevent" once will cause it to be called forever
            // so make sure you turn it off once the ship leaves using a patch into
            // the class "StartOfRound" and IEnumerator "openingDoorsSequence, unless you want it to persist
            if (!keepDoingEvent)
            {
                keepDoingEvent = true;
                // This line of code shows every client the same message
                ChaosManager.NetworkDisplayTip("Hello!", "I am a tip all clients will see!");
            }
            // an example of an event may be that all of the players controls are inverted for thirty seconds
            // or it teleports the player to a random enemy inside
            // or any number of things, thats up for you to make!
        }
        // This method is completely optional, however if you wish for others to
        // be able to add to your mod as well i would recommend it!
        public void RegisterExampleEvent(ChaosEvent chaosEvent)
        {
            // This checks if the list already contains the event, and tells the method to stop 
            // running if it does
            if (exampleEvents.Contains(chaosEvent)) return;
            // otherwise, add it!
            exampleEvents.Add(chaosEvent);
        }
        // again this method is optional, but i HEAVILY recommend you make one so that your list of events
        // is as accurate as possible!
        public void PopulateExampleEvents()
        {
            // the chaos event class contains three constructors

            // a delegate, referred to as "delegatedEvent" which can be accessed
            // through exampleEvents[index].delegatedEvent(); to call a specific event

            // a string, which is the name and allows your method to be specifically
            // chosen from a method under chaosmanager named "PickEventByName"
            
            // and a single bool, "availableForTimer" which is false by default
            // setting it to true means you have a chance for it to occur every 60 seconds
            RegisterExampleEvent(new ChaosEvent(DoPermanentEvent,"Examples!", false));
            // here's another example of what it should look like!
            RegisterExampleEvent(new ChaosEvent(NotPermanentEvent, "Examples2!!", true));

            // after registering your events, you MUST 100% ALWAYS
            // add them to the list of all events
            // this allows all of your events to be chosen from the previously
            // mentioned "PickEventByName" or used as a timer event
            ChaosManager.listOfAllEvents.AddRange(exampleEvents);
        }
    }
}
