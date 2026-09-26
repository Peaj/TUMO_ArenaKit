namespace ArenaKit
{
    /// <summary>
    /// Anything that can be hurt by projectiles, explosions or hazards.
    /// </summary>
    public interface IDamageable
    {
        /// <param name="damage">How much damage.</param>
        /// <param name="attacker">Number of the player who caused it (1-4). 0 = nobody, e.g. the level itself.</param>
        void TakeDamage(float damage, int attacker);
    }

    /// <summary>
    /// Anything that reacts when a projectile hits it, e.g. switches.
    /// </summary>
    public interface IShootable
    {
        void OnShot(Projectile projectile);
    }
}
