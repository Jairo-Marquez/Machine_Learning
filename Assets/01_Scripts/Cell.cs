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
        0.8f,
        1.2f,
        1.6f
    };

    private bool resultRegistered = false;

    private int currentColorIndex;
    private int currentSizeLevel;

    private Color currentColor;
    private float currentSize;

    private void Start()
    {
        GenerateCharacteristics();
    }

    private void GenerateCharacteristics()
    {
        if (possibleColors == null ||
            possibleColors.Length == 0)
        {
            Debug.LogError(
                "Cell: No hay colores configurados."
            );

            return;
        }

        if (sizeValues == null ||
            sizeValues.Length == 0)
        {
            Debug.LogError(
                "Cell: No hay tamaños configurados."
            );

            return;
        }

        // Intentar usar conocimiento aprendido.
        if (GameManager.Instance != null &&
            GameManager.Instance.TryGetLearnedCharacteristics(
                out int learnedColor,
                out int learnedSize))
        {
            SetCharacteristics(
                learnedColor,
                learnedSize
            );

            return;
        }

        // Si no hay conocimiento suficiente:
        // explorar una combinación aleatoria.
        int randomColor =
            Random.Range(
                0,
                possibleColors.Length
            );

        int randomSize =
            Random.Range(
                0,
                sizeValues.Length
            );

        SetCharacteristics(
            randomColor,
            randomSize
        );
    }

    private void SetCharacteristics(
        int colorIndex,
        int sizeLevel)
    {
        currentColorIndex = colorIndex;
        currentSizeLevel = sizeLevel;

        Color baseColor =
            possibleColors[colorIndex];

        // El GameManager decide cuánto se ha
        // adaptado esta combinación al fondo.
        if (GameManager.Instance != null)
        {
            currentColor =
                GameManager.Instance.GetAdaptiveColor(
                    colorIndex,
                    sizeLevel,
                    baseColor
                );
        }
        else
        {
            currentColor = baseColor;
        }

        // Forzar opacidad total: si en el Inspector algun color
        // quedo con alpha = 0, la celula seria invisible.
        currentColor.a = 1f;

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

            // Asegurar que la célula sea visible
            // sobre otros sprites.
            spriteRenderer.sortingOrder = 10;
        }

        Debug.Log(
            "CELULA CREADA → " +
            "Color: " +
            GetColorName(currentColorIndex) +
            " | Tamaño: " +
            GetSizeName(currentSizeLevel)
        );
    }

    private void OnMouseDown()
    {
        if (resultRegistered)
            return;

        // Si el jugador hizo clic,
        // esta célula NO sobrevivió.
        RegisterResult(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }

        Debug.Log(
            "CELULA ELIMINADA → " +
            GetColorName(currentColorIndex) +
            " + " +
            GetSizeName(currentSizeLevel)
        );

        Destroy(gameObject);
    }

    public void RegisterSurvival()
    {
        if (resultRegistered)
            return;

        // Llegó viva al final de los 10 segundos.
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

    private string GetColorName(
        int colorIndex)
    {
        switch (colorIndex)
        {
            case 0:
                return "Rojo";

            case 1:
                return "Verde";

            case 2:
                return "Azul";

            case 3:
                return "Amarillo";

            case 4:
                return "Magenta";

            default:
                return "Desconocido";
        }
    }

    private string GetSizeName(
        int sizeLevel)
    {
        switch (sizeLevel)
        {
            case 0:
                return "Pequeño";

            case 1:
                return "Mediano";

            case 2:
                return "Grande";

            default:
                return "Desconocido";
        }
    }
}