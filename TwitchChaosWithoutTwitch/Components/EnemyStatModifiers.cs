using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace TwitchChaosWithoutTwitch.Components
{
    internal class EnemyStatModifiers : MonoBehaviour
    {
        public enum DamageModifiers
        {
            None,
            Zero,
            More
        }
        public DamageModifiers currentMod = DamageModifiers.None;
        public bool increaseSpeed = false;
        public void Update()
        {
            if(increaseSpeed)
            {
                if(gameObject.TryGetComponent(out EnemyAI self))
                {
                    self.agent.speed = 50;
                }
            }
        }
    }
}
