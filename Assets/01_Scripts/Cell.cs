using UnityEngine;

public class Cell : MonoBehaviour
{
    private bool wasClicked = false;

    private void OnMouseDown()
    {
        if (wasClicked)
            return;

        wasClicked = true;

        Debug.Log("¡Célula eliminada!");

        // Aumenta la puntuación.
        GameManager.Instance.AddScore(1);

        // Elimina la célula.
        Destroy(gameObject);
    }
}