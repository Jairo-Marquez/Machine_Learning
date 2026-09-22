using UnityEngine;

public class Cell : MonoBehaviour
{
    [Header("Colores")]
    [SerializeField]
    private Color[] possibleColors;

    [Header("Tamaños")]
    [SerializeField]
    private float[] sizeValues =
    {
        0.6f,
        1.0f,
        1.4f
    };

    private bool resultRegistered = false;

    private Color currentColor;
    private int currentColorIndex;
    private int currentSizeLevel;
    private float currentSize;

    private void Start()
    {
        ApplyCharacteristics();
    }

    private void ApplyCharacteristics()
    {
        // Primero preguntamos a la IA si ya tiene
        // suficiente conocimiento para tomar una decisión.
        if (GameManager.Instance != null &&
            GameManager.Instance.TryGetLearnedCharacteristics(
                out int learnedColorIndex,
                out int learnedSizeLevel))
        {
            if (possibleColors != null &&
                possibleColors.Length > learnedColorIndex &&
                sizeValues != null &&
                sizeValues.Length > learnedSizeLevel)
            {
                SetCharacteristics(
                    learnedColorIndex,
                    learnedSizeLevel
                );

                return;
            }
        }

        // Si no hay conocimiento suficiente,
        // exploramos una combinación nueva.
        ApplyRandomCharacteristics();
    }

    private void ApplyRandomCharacteristics()
    {
        if (possibleColors == null ||
            possibleColors.Length == 0)
        {
            Debug.LogWarning(
                "Cell necesita al menos un color."
            );

            return;
        }

        if (sizeValues == null ||
            sizeValues.Length == 0)
        {
            Debug.LogWarning(
                "Cell necesita al menos un tamaño."
            );

            return;
        }

        int randomColorIndex =
            Random.Range(
                0,
                possibleColors.Length
            );

        int randomSizeLevel =
            Random.Range(
                0,
                sizeValues.Length
            );

        SetCharacteristics(
            randomColorIndex,
            randomSizeLevel
        );
    }

    private void SetCharacteristics(
        int colorIndex,
        int sizeLevel)
    {
        currentColorIndex = colorIndex;
        currentSizeLevel = sizeLevel;

        currentColor =
            possibleColors[colorIndex];

        currentSize =
            sizeValues[sizeLevel];

        transform.localScale =
            Vector3.one * currentSize;

        SpriteRenderer spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                currentColor;
        }
    }

    private void OnMouseDown()
    {
        if (resultRegistered)
            return;

        // El jugador eliminó la célula.
        RegisterResult(false);

        Debug.Log(
            "¡Célula eliminada!"
        );

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }

        Destroy(gameObject);
    }

    public void RegisterSurvival()
    {
        if (resultRegistered)
            return;

        // Llegó viva al final de la ronda.
        RegisterResult(true);
    }

    private void RegisterResult(
        bool survived)
    {
        resultRegistered = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterCellExperience(
                currentColorIndex,
                currentSizeLevel,
                survived
            );
        }
    }
}