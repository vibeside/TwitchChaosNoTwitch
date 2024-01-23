using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TwitchChaosWithoutTwitch.Components
{
    public class ChaosEvent
    {
        public string name;
        public delegate void EventDelegate();
        public EventDelegate delegatedEvent;
        public bool availableForTimer;
        public ChaosEvent(EventDelegate eventDelegate, string _name = "", bool _availableForTimer = false)
        {
            name = _name;
            if (_name == "") name = $"Unnamed Event {ChaosManager.listOfAllEvents.Count}";
            NoMoreTwitch.mls.LogInfo($"{name} was loaded!");
            delegatedEvent = eventDelegate;
            availableForTimer = _availableForTimer;
        }
    }
}
