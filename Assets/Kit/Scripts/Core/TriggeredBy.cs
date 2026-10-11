using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Who or what set off the event that is running right now.
    /// Triggers (TriggerZone, HitSensor, Breakable...) remember it while their events run, so actions
    /// like ForcePush, Hurt or Score work on "whoever triggered it" without any extra setup.
    /// It is passed down a chain: TriggerZone > Switch > Counter > Explode still knows the player.
    /// </summary>
    public static class TriggeredBy
    {
        struct Entry
        {
            public GameObject Thing;
            public int Player;
        }

        static readonly List<Entry> stack = new List<Entry>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            stack.Clear();
        }

        /// <summary>The object that triggered the event (a player, ball, crate...). Null when nothing did, e.g. a timer.</summary>
        public static GameObject Thing => stack.Count > 0 ? stack[stack.Count - 1].Thing : null;

        /// <summary>Number of the player behind it (1-4), also for balls and projectiles. 0 = nobody.</summary>
        public static int Player => stack.Count > 0 ? stack[stack.Count - 1].Player : 0;

        /// <summary>The player character that triggered the event itself (not the owner of a ball or rocket).</summary>
        public static Character Character => Thing != null ? Thing.GetComponentInParent<Character>() : null;

        public static Rigidbody2D Body => Thing != null ? Thing.GetComponentInParent<Rigidbody2D>() : null;

        public static void Invoke(UnityEvent<int> unityEvent, GameObject thing, int player)
        {
            stack.Add(new Entry { Thing = thing, Player = player });
            try
            {
                unityEvent.Invoke(player);
            }
            finally
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }

        public static void Invoke(UnityEvent unityEvent, GameObject thing, int player)
        {
            stack.Add(new Entry { Thing = thing, Player = player });
            try
            {
                unityEvent.Invoke();
            }
            finally
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }

        /// <summary>The character of a player number, if that player is alive in this level.</summary>
        public static GameObject PlayerObject(int player)
        {
            Character character = player > 0 && LevelRules.Current != null ? LevelRules.Current.GetPlayer(player) : null;
            return character != null ? character.gameObject : null;
        }
    }
}
