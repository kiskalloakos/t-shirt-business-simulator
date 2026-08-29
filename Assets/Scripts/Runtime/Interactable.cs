using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public abstract string GetPrompt(Day1Game game);
    public abstract void Interact(PlayerInteractor player, Day1Game game);
}
