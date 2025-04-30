namespace Misled.Characters.Core;

public abstract class Ability {
    public abstract void Use(CharacterBase user);
    public abstract float Cooldown { get; }
}
