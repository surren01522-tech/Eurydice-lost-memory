namespace Eurydice.Gameplay.Interaction
{
    public interface IInteractable
    {
        bool CanInteract { get; }
        string InteractionAnimationState { get; }
        float InteractionAnimationDuration { get; }
        void Interact();
    }
}
