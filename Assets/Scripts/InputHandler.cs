using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public void OnClick(InputAction.CallbackContext context)
    {
        // Alleen reageren als de muisknop wordt ingedrukt en de beurt niet bezig is
        if (!context.performed) return;
        
        // Controleer of er al een actie bezig is via de Singleton
        if (GameController.instance.isTurnInProgress) return;
        if (GameState.instance.isSaving) return;
        if (GameState.instance.isLoading) return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider != null)
        {
            string clickedName = hit.collider.gameObject.name;

            // Als we op een dobbelsteen klikken
            if (clickedName.Equals("Dice1") || clickedName.Equals("Dice2"))
            {
                GameController.instance.OnDiceClicked();
            }

            if (clickedName.Equals("Save"))
            {
                GameState.instance.Save();
            }

            if (clickedName.Equals("Load"))
            {
                GameState.instance.Load();
            }
        }
    }
}