namespace Misled.Gameplay.Core;

public abstract class Ability {
    public abstract void Use(Base user);
    public abstract float Cooldown { get; }
}
