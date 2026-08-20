namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    public interface IS5AInteractable
    {
        string InteractionLabel { get; }
        string LastInteractionResult { get; }
        bool ActivateInteraction();
    }
}
