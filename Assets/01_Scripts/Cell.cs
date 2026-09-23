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
    private float camouflageLevel;

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

        // Calcular camuflaje.
        camouflageLevel =
            CalculateCamouflage();

        SpriteRenderer spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                GetCamouflagedColor(currentColor);
        }

        Debug.Log(
            "Célula creada → " +
            "Color: " +
            currentColorIndex +
            " | Tamaño: " +
            currentSizeLevel +
            " | Camuflaje: " +
            (camouflageLevel * 100f).ToString("F0") +
            "%"
        );
    }


    private float CalculateCamouflage()
    {
        if (GameManager.Instance == null)
        {
            return 0f;
        }

        Color environmentColor =
            GameManager.Instance.EnvironmentColor;

        float colorDifference =
            Vector3.Distance(
                new Vector3(
                    currentColor.r,
                    currentColor.g,
                    currentColor.b
                ),
                new Vector3(
                    environmentColor.r,
                    environmentColor.g,
                    environmentColor.b
                )
            );

        // Convertimos diferencia de color
        // en similitud.
        float colorCamouflage =
            1f - Mathf.Clamp01(
                colorDifference
            );

        // Las células pequeñas son más difíciles
        // de detectar.
        float sizeCamouflage;

        switch (currentSizeLevel)
        {
            case 0:
                sizeCamouflage = 0.8f;
                break;

            case 1:
                sizeCamouflage = 0.5f;
                break;

            case 2:
                sizeCamouflage = 0.2f;
                break;

            default:
                sizeCamouflage = 0.5f;
                break;
        }

        // Combinamos color y tamaño.
        float finalCamouflage =
            (colorCamouflage * 0.7f) +
            (sizeCamouflage * 0.3f);

        return Mathf.Clamp01(
            finalCamouflage
        );
    }

    private Color GetCamouflagedColor(
    Color originalColor)
    {
        if (GameManager.Instance == null)
        {
            return originalColor;
        }

        Color environmentColor =
            GameManager.Instance.EnvironmentColor;

        // Cuanto mayor sea el camuflaje,
        // más se mezcla la célula con el entorno.
        Color camouflagedColor =
            Color.Lerp(
                originalColor,
                environmentColor,
                camouflageLevel * 0.65f
            );

        return camouflagedColor;
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