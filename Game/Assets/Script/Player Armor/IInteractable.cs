public enum InteractionType
{
    Weapon,
    Crest
}

public interface IInteractable
{
    void Interact(InteractionType type);
}
