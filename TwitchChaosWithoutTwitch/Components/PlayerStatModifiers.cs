using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TwitchChaosWithoutTwitch.Components
{
    internal class PlayerStatModifiers : MonoBehaviour
    {
        public bool immortal = false;
        public bool infiniteSprint = false;
        public void Update()
        {
            if(infiniteSprint)
            {
                if(TryGetComponent(out PlayerControllerB player))
                {
                    player.sprintMeter = 1f;
                }
            }
        }
        public void MakeTemporarilyImmortal(float time = 15f)
        {
            StartCoroutine(MakeImmortalForTime(time));
        }
        public IEnumerator MakeImmortalForTime(float timeOverride)
        {
            immortal = true;
            yield return new WaitForSeconds(timeOverride);
            immortal = false;
        }
    }
}
