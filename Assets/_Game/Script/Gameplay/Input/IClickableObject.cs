public interface IClickableObject
{
    public bool Interactable { get; set; }
    public void OnObjectClicked();
    public bool CanBeClicked();
    public void OnClickBlocked();
}
