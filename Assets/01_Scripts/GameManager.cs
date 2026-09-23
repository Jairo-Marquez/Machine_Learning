using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // =========================================================
    // EXPERIENCIA DE UNA COMBINACIÓN
    // =========================================================

    [System.Serializable]
    public class CellExperience
    {
        public int colorIndex;
        public int sizeLevel;

        public int attempts;
        public int survivals;

        public float SurvivalRate
        {
            get
            {
                if (attempts == 0)
                    return 0f;

                return (float)survivals / attempts;
            }
        }

        public float LearningScore
        {
            get
            {
                // Suavizado:
                // una sola prueba no determina
                // completamente la estrategia.
                return (survivals + 1f) /
                       (attempts + 2f);
            }
        }

        public CellExperience(
            int colorIndex,
            int sizeLevel)
        {
            this.colorIndex = colorIndex;
            this.sizeLevel = sizeLevel;
            attempts = 0;
            survivals = 0;
        }
    }


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text roundText;

    [SerializeField] private TMP_Text survivorsText;
    [SerializeField] private TMP_Text eliminatedText;

    [SerializeField] private TMP_Text learningText;
    [SerializeField] private TMP_Text learningModeText;
    [SerializeField] private TMP_Text learningTableText;


    // =========================================================
    // PANEL DE RESULTADO
    // =========================================================

    [Header("Resultado de ronda")]
    [SerializeField] private GameObject roundResultPanel;
    [SerializeField] private TMP_Text resultEliminatedText;
    [SerializeField] private TMP_Text resultSurvivorsText;
    [SerializeField] private TMP_Text resultLearningText;


    // =========================================================
    // MENU INICIAL
    // =========================================================

    [Header("Pantalla inicial")]
    [SerializeField] private GameObject startPanel;


    // =========================================================
    // RONDA
    // =========================================================

    [Header("Configuración de ronda")]
    [SerializeField] private float roundDuration = 10f;


    // =========================================================
    // SPAWNER
    // =========================================================

    [Header("Spawner")]
    [SerializeField] private CellSpawner cellSpawner;


    // =========================================================
    // APRENDIZAJE
    // =========================================================

    [Header("Machine Learning")]
    [Range(0f, 1f)]
    [SerializeField] private float explorationChance = 0.30f;

    [SerializeField] private int minimumAttemptsToLearn = 2;

    [SerializeField] private float adaptationPerFailure = 0.20f;


    // =========================================================
    // ENTORNO
    // =========================================================

    [Header("Entorno")]
    [SerializeField]
    private Color environmentColor =
        new Color(
            106f / 255f,
            106f / 255f,
            106f / 255f,
            1f
        );


    // =========================================================
    // VARIABLES INTERNAS
    // =========================================================

    private List<CellExperience> experiences =
        new List<CellExperience>();

    // Adaptación independiente para:
    // color + tamaño.
    //
    // 5 colores x 3 tamaños.
    private float[,] adaptationLevels =
        new float[5, 3];


    private int score = 0;
    private int currentRound = 1;

    private float currentTime;

    private int roundSurvivals = 0;
    private int roundEliminations = 0;

    private bool waitingForNextRound = false;

    // El temporizador solo corre despues de pulsar JUGAR.
    private bool gameRunning = false;


    // =========================================================
    // PROPIEDADES PÚBLICAS
    // =========================================================

    public Color EnvironmentColor
    {
        get
        {
            return environmentColor;
        }
    }


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        currentTime =
            roundDuration;

        ApplyEnvironmentColor();

        UpdateScoreUI();
        UpdateTimerUI();
        UpdateRoundUI();
        UpdateStatsUI();
        UpdateLearningTableUI();

        UpdateLearningModeUI(
            "Esperando"
        );

        // Mostrar menú inicial.
        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }

        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(false);
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!gameRunning || waitingForNextRound)
        {
            return;
        }

        currentTime -=
            Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;

            EndRound();

            return;
        }

        UpdateTimerUI();
    }


    // =========================================================
    // INICIAR PARTIDA
    // =========================================================

    public void StartGame()
    {
        score = 0;

        currentRound = 1;

        currentTime = roundDuration;

        roundSurvivals = 0;

        roundEliminations = 0;

        waitingForNextRound = false;

        gameRunning = true;

        // Reiniciar completamente el aprendizaje.
        experiences.Clear();

        adaptationLevels =
            new float[5, 3];

        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }

        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(false);
        }

        ApplyEnvironmentColor();

        UpdateScoreUI();
        UpdateTimerUI();
        UpdateRoundUI();
        UpdateStatsUI();
        UpdateLearningTableUI();

        UpdateLearningModeUI(
            "Exploración"
        );

        if (cellSpawner != null)
        {
            cellSpawner.ClearCells();
            cellSpawner.SpawnCells();
        }

        Debug.Log(
            "===== COMIENZA LA PARTIDA ====="
        );
    }


    // =========================================================
    // PUNTUACIÓN
    // =========================================================

    public void AddScore(int amount)
    {
        score += amount;

        UpdateScoreUI();
    }


    // =========================================================
    // REGISTRAR EXPERIENCIA
    // =========================================================

    public void RegisterCellExperience(
        int colorIndex,
        int sizeLevel,
        bool survived)
    {
        CellExperience experience =
            FindExperience(
                colorIndex,
                sizeLevel
            );


        if (experience == null)
        {
            experience =
                new CellExperience(
                    colorIndex,
                    sizeLevel
                );

            experiences.Add(
                experience
            );
        }


        experience.attempts++;


        if (survived)
        {
            experience.survivals++;

            roundSurvivals++;

            Debug.Log(
                "SUPERVIVENCIA → " +
                GetColorName(colorIndex) +
                " + " +
                GetSizeName(sizeLevel)
            );
        }
        else
        {
            roundEliminations++;

            // Una eliminación provoca
            // adaptación hacia el fondo.
            AdaptAfterFailure(
                colorIndex,
                sizeLevel
            );
        }


        Debug.Log(
            "APRENDIZAJE → " +
            GetColorName(colorIndex) +
            " + " +
            GetSizeName(sizeLevel) +
            " | Intentos: " +
            experience.attempts +
            " | Supervivencias: " +
            experience.survivals +
            " | Supervivencia: " +
            (
                experience.SurvivalRate * 100f
            ).ToString("F1") +
            "%"
        );


        UpdateStatsUI();
        UpdateLearningTableUI();
    }


    // =========================================================
    // ADAPTACIÓN DESPUÉS DE SER ELIMINADA
    // =========================================================

    private void AdaptAfterFailure(
    int colorIndex,
    int sizeLevel)
    {
        if (colorIndex < 0 ||
            colorIndex >= 5)
        {
            return;
        }

        if (sizeLevel < 0 ||
            sizeLevel >= 3)
        {
            return;
        }

        adaptationLevels[
            colorIndex,
            sizeLevel
        ] += adaptationPerFailure;

        adaptationLevels[
            colorIndex,
            sizeLevel
        ] = Mathf.Clamp01(
            adaptationLevels[
                colorIndex,
                sizeLevel
            ]
        );

        Debug.Log(
            "ADAPTACIÓN → " +
            GetColorName(colorIndex) +
            " + " +
            GetSizeName(sizeLevel) +
            " | Nivel: " +
            (
                adaptationLevels[
                    colorIndex,
                    sizeLevel
                ] * 100f
            ).ToString("F0") +
            "%"
        );
    }


    // =========================================================
    // OBTENER COLOR ADAPTADO
    // =========================================================

    public Color GetAdaptiveColor(
    int colorIndex,
    int sizeLevel,
    Color originalColor)
    {
        if (colorIndex < 0 ||
            colorIndex >= 5 ||
            sizeLevel < 0 ||
            sizeLevel >= 3)
        {
            return originalColor;
        }

        float adaptation =
            adaptationLevels[colorIndex, sizeLevel];

        // MUY IMPORTANTE:
        // Si nunca ha sido eliminada esta combinación,
        // debe conservar exactamente su color original.
        if (adaptation <= 0f)
        {
            return originalColor;
        }

        return Color.Lerp(
            originalColor,
            environmentColor,
            adaptation
        );
    }


    // =========================================================
    // DECISIÓN DE LA IA
    // =========================================================

    public bool TryGetLearnedCharacteristics(
        out int learnedColorIndex,
        out int learnedSizeLevel)
    {
        learnedColorIndex = 0;
        learnedSizeLevel = 1;


        // Todavía no existe suficiente conocimiento.
        if (experiences.Count == 0)
        {
            UpdateLearningModeUI(
                "Exploración"
            );

            return false;
        }


        // 30%: explorar algo nuevo.
        if (Random.value <
            explorationChance)
        {
            UpdateLearningModeUI(
                "Exploración"
            );

            return false;
        }


        CellExperience best =
            GetBestExperience();


        if (best == null)
        {
            UpdateLearningModeUI(
                "Exploración"
            );

            return false;
        }


        // 70%: utilizar conocimiento.
        learnedColorIndex =
            best.colorIndex;

        learnedSizeLevel =
            best.sizeLevel;


        UpdateLearningModeUI(
            "Aprendizaje"
        );


        Debug.Log(
            "DECISIÓN IA → " +
            GetColorName(
                learnedColorIndex
            ) +
            " + " +
            GetSizeName(
                learnedSizeLevel
            ) +
            " | Score: " +
            (
                best.LearningScore * 100f
            ).ToString("F1") +
            "%"
        );


        return true;
    }


    // =========================================================
    // BUSCAR EXPERIENCIA
    // =========================================================

    private CellExperience FindExperience(
        int colorIndex,
        int sizeLevel)
    {
        foreach (
            CellExperience experience
            in experiences)
        {
            if (
                experience.colorIndex ==
                    colorIndex &&
                experience.sizeLevel ==
                    sizeLevel)
            {
                return experience;
            }
        }

        return null;
    }


    // =========================================================
    // BUSCAR MEJOR EXPERIENCIA
    // =========================================================

    private CellExperience GetBestExperience()
    {
        CellExperience best = null;


        foreach (
            CellExperience experience
            in experiences)
        {
            if (
                experience.attempts <
                minimumAttemptsToLearn)
            {
                continue;
            }


            if (
                best == null ||
                experience.LearningScore >
                best.LearningScore)
            {
                best = experience;
            }
        }


        return best;
    }


    // =========================================================
    // FIN DE RONDA
    // =========================================================

    private void EndRound()
    {
        Debug.Log(
            "===== FIN DE RONDA " +
            currentRound +
            " ====="
        );


        // Registrar células supervivientes.
        if (cellSpawner != null)
        {
            cellSpawner.RegisterSurvivingCells();
        }


        UpdateStatsUI();
        UpdateLearningTableUI();


        // Eliminar células restantes.
        if (cellSpawner != null)
        {
            cellSpawner.ClearCells();
        }


        // Mostrar resultado.
        ShowRoundResults();


        // Pausar hasta CONTINUAR.
        waitingForNextRound = true;
    }


    // =========================================================
    // MOSTRAR RESULTADOS
    // =========================================================

    private void ShowRoundResults()
    {
        if (resultEliminatedText != null)
        {
            resultEliminatedText.text =
                "Eliminadas: " +
                roundEliminations;
        }


        if (resultSurvivorsText != null)
        {
            resultSurvivorsText.text =
                "Sobrevivientes: " +
                roundSurvivals;
        }


        if (resultLearningText != null)
        {
            CellExperience best =
                GetBestExperience();


            if (best != null)
            {
                resultLearningText.text =
                    "Mejor estrategia: " +
                    GetColorName(
                        best.colorIndex
                    ) +
                    " + " +
                    GetSizeName(
                        best.sizeLevel
                    ) +
                    "\nSupervivencia: " +
                    (
                        best.SurvivalRate *
                        100f
                    ).ToString("F1") +
                    "%";
            }
            else
            {
                resultLearningText.text =
                    "Recopilando datos...";
            }
        }


        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(true);
        }
    }


    // =========================================================
    // CONTINUAR A SIGUIENTE RONDA
    // =========================================================

    public void ContinueToNextRound()
    {
        if (!waitingForNextRound)
        {
            return;
        }


        waitingForNextRound = false;


        currentRound++;

        currentTime =
            roundDuration;

        roundSurvivals = 0;

        roundEliminations = 0;


        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(false);
        }


        UpdateRoundUI();
        UpdateStatsUI();
        UpdateLearningTableUI();


        if (cellSpawner != null)
        {
            cellSpawner.SpawnCells();
        }


        Debug.Log(
            "===== COMIENZA RONDA " +
            currentRound +
            " ====="
        );
    }


    // =========================================================
    // COLOR DEL ENTORNO
    // =========================================================

    private void ApplyEnvironmentColor()
    {
        Camera mainCamera =
            Camera.main;


        if (mainCamera != null)
        {
            mainCamera.backgroundColor =
                environmentColor;
        }
    }


    // =========================================================
    // TABLA DE APRENDIZAJE
    // =========================================================

    private void UpdateLearningTableUI()
    {
        if (learningTableText == null)
        {
            return;
        }


        string table =
            "MEMORIA DE LA IA\n\n";


        table +=
            "              PEQ    MED    GRA\n";


        string[] colorNames =
        {
            "Rojo",
            "Verde",
            "Azul",
            "Amarillo",
            "Magenta"
        };


        for (
            int colorIndex = 0;
            colorIndex < 5;
            colorIndex++)
        {
            table +=
                colorNames[colorIndex]
                    .PadRight(12);


            for (
                int sizeLevel = 0;
                sizeLevel < 3;
                sizeLevel++)
            {
                CellExperience experience =
                    FindExperience(
                        colorIndex,
                        sizeLevel
                    );


                float percentage = 0f;


                if (
                    experience != null &&
                    experience.attempts > 0)
                {
                    percentage =
                        experience.SurvivalRate *
                        100f;
                }


                table +=
                    percentage
                        .ToString("F0") +
                    "%".PadLeft(6);
            }


            table += "\n";
        }


        learningTableText.text =
            table;
    }


    // =========================================================
    // UI
    // =========================================================

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Puntuación: " +
                score;
        }
    }


    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text =
                "Tiempo: " +
                Mathf.CeilToInt(
                    currentTime
                );
        }
    }


    private void UpdateRoundUI()
    {
        if (roundText != null)
        {
            roundText.text =
                "Ronda: " +
                currentRound;
        }
    }


    private void UpdateStatsUI()
    {
        if (survivorsText != null)
        {
            survivorsText.text =
                "Sobrevivientes: " +
                roundSurvivals;
        }


        if (eliminatedText != null)
        {
            eliminatedText.text =
                "Eliminadas: " +
                roundEliminations;
        }


        if (learningText != null)
        {
            CellExperience best =
                GetBestExperience();


            if (best != null)
            {
                learningText.text =
                    "Mejor estrategia: " +
                    GetColorName(
                        best.colorIndex
                    ) +
                    " + " +
                    GetSizeName(
                        best.sizeLevel
                    ) +
                    " | " +
                    (
                        best.SurvivalRate *
                        100f
                    ).ToString("F1") +
                    "%";
            }
            else
            {
                learningText.text =
                    "Aprendizaje: recopilando datos...";
            }
        }
    }


    private void UpdateLearningModeUI(
        string mode)
    {
        if (learningModeText != null)
        {
            learningModeText.text =
                "Modo IA: " +
                mode;
        }
    }


    // =========================================================
    // NOMBRES
    // =========================================================

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