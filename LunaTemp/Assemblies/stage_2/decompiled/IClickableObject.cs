public interface IClickableObject
{
	bool Interactable { get; set; }

	void OnObjectClicked();

	bool CanBeClicked();

	void OnClickBlocked();
}
