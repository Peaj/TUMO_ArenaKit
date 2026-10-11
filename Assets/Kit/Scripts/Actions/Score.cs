using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Points and winning for the player who triggered it: coins, finish flags, goals...
    /// For balls and rockets that is the player who touched or shot them last.
    /// </summary>
    public class Score : MonoBehaviour
    {
        [Tooltip("Points for 'Add Points' and 'Remove Points'")]
        public int Points = 1;
        [Tooltip("0 = the player who triggered it. 1-4 = always this player (volleyball: the player on the other side)")]
        [Range(0, GameManager.MaxPlayers)] public int ForPlayer = 0;

        int Player => ForPlayer > 0 ? ForPlayer : TriggeredBy.Player;

        public void AddPoints()
        {
            if (Player > 0 && LevelRules.Current != null)
                LevelRules.Current.AddPoints(Player, Points);
        }

        public void RemovePoints()
        {
            if (Player > 0 && LevelRules.Current != null)
                LevelRules.Current.AddPoints(Player, -Points);
        }

        /// <summary>This player wins the round right away (finish flag).</summary>
        public void WinRound()
        {
            if (Player > 0 && LevelRules.Current != null && !LevelRules.Current.IsRoundOver)
                LevelRules.Current.WinRound(Player);
        }

        /// <summary>Sends the player back to their spawn point (races with several laps).</summary>
        public void SendToStart()
        {
            LevelRules rules = LevelRules.Current;
            Character character = rules != null && Player > 0 ? rules.GetPlayer(Player) : null;
            if (character != null && !character.IsDead && !rules.IsRoundOver)
                rules.SendToSpawn(character);
        }
    }
}
