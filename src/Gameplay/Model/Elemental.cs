namespace Misled.Gameplay.Model;

public enum Element {
    Fire,
    Ice,
    Water,
    Electric,
}

public class Elemental {
    public Element Affection { get; set; }
    public Element Power { get; set; }
}
