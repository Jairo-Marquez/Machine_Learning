using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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
                // Suavizado para evitar que una sola prueba
                // determine completamente la estrategia.
                return (survivals + 1f) / (attempts + 2f);
            }
        }

        public CellExperience(int colorIndex, int sizeLevel)
        {
            this.colorIndex = colorIndex;
            this.sizeLevel = sizeLevel;
            attempts = 0;
            survivals = 0;
        }
    }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text survivorsText;
    [SerializeField] private TMP_Text eliminatedText;
    [SerializeField] private TMP_Text learningText;
    [SerializeField] private TMP_Text learningModeText;
    [SerializeField] private TMP_Text learningTableText;

    [Header("Fin de partida")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalRoundText;

    [Header("Pantalla inicial")]
    [SerializeField] private GameObject startPanel;

    [Header("Panel de resultado")]
    [SerializeField] private GameObject roundResultPanel;
    [SerializeField] private TMP_Text resultEliminatedText;
    [SerializeField] private TMP_Text resultSurvivorsText;
    [SerializeField] private TMP_Text resultLearningText;

    private bool waitingForNextRound = false;

    [Header("Ronda")]
    [SerializeField] private float roundDuration = 10f;
    [SerializeField] private int maxRounds = 5;

    [Header("Spawner")]
    [SerializeField] private CellSpawner cellSpawner;

    [Header("Aprendizaje")]
    [SerializeField] private float explorationChance = 0.30f;

    [Header("Entorno")]
    [SerializeField]
    private Color environmentColor =
    new Color(0.3f, 0.3f, 0.3f);

    [SerializeField] private int minimumAttemptsToLearn = 2;

    private List<CellExperience> experiences =
        new List<CellExperience>();

    private int score = 0;
    private int currentRound = 1;
    private float currentTime;

    private bool gameFinished = false;

    private int roundSurvivals = 0;
    private int roundEliminations = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public Color EnvironmentColor
    {
        get
        {
            return environmentColor;
        }
    }

    private void Start()
    {
        currentTime = roundDuration;

        UpdateScoreUI();
        UpdateTimerUI();
        UpdateRoundUI();
        UpdateStatsUI();
        UpdateLearningTableUI();
        UpdateLearningModeUI("Esperando");

        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }

        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public void StartGame()
    {
        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }

        currentRound = 1;
        currentTime = roundDuration;

        score = 0;
        roundSurvivals = 0;
        roundEliminations = 0;

        gameFinished = false;
        waitingForNextRound = false;

        UpdateScoreUI();
        UpdateTimerUI();
        UpdateRoundUI();
        UpdateStatsUI();
        UpdateLearningTableUI();
        UpdateLearningModeUI("Exploración");

        if (cellSpawner != null)
        {
            cellSpawner.ClearCells();
            cellSpawner.SpawnCells();
        }

        Debug.Log("COMIENZA LA PARTIDA");
    }

    private void Update()
    {
        if (waitingForNextRound || gameFinished)
            return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            EndRound();
        }

        UpdateTimerUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    public void RegisterCellExperience(
        int colorIndex,
        int sizeLevel,
        bool survived)
    {
        CellExperience experience =
            FindExperience(colorIndex, sizeLevel);

        if (experience == null)
        {
            experience = new CellExperience(
                colorIndex,
                sizeLevel
            );

            experiences.Add(experience);
        }

        experience.attempts++;

        if (survived)
        {
            experience.survivals++;
            roundSurvivals++;
        }
        else
        {
            roundEliminations++;
        }

        Debug.Log(
            "APRENDIZAJE → " +
            "Color: " + GetColorName(colorIndex) +
            " | Tamaño: " + GetSizeName(sizeLevel) +
            " | Intentos: " + experience.attempts +
            " | Supervivencias: " + experience.survivals +
            " | Supervivencia: " +
            (experience.SurvivalRate * 100f).ToString("F1") +
            "%" +
            " | Score: " +
            (experience.LearningScore * 100f).ToString("F1") +
            "%"
        );

        UpdateStatsUI();
        UpdateLearningTableUI();
    }

    public bool TryGetLearnedCharacteristics(
        out int learnedColorIndex,
        out int learnedSizeLevel)
    {
        learnedColorIndex = 0;
        learnedSizeLevel = 1;

        // Todavía no tenemos experiencias.
        if (experiences.Count == 0)
        {
            UpdateLearningModeUI("Exploración");
            return false;
        }

        // 30% de exploración.
        if (Random.value < explorationChance)
        {
            UpdateLearningModeUI("Exploración");
            return false;
        }

        CellExperience bestExperience =
            GetBestExperience();

        // No existe una experiencia suficientemente estable.
        if (bestExperience == null)
        {
            UpdateLearningModeUI("Exploración");
            return false;
        }

        learnedColorIndex =
            bestExperience.colorIndex;

        learnedSizeLevel =
            bestExperience.sizeLevel;

        UpdateLearningModeUI(
            "Aprendizaje"
        );

        Debug.Log(
            "DECISIÓN IA → " +
            "Color: " +
            GetColorName(learnedColorIndex) +
            " | Tamaño: " +
            GetSizeName(learnedSizeLevel) +
            " | Score: " +
            (bestExperience.LearningScore * 100f).ToString("F1") +
            "%"
        );

        return true;
    }

    private CellExperience FindExperience(
        int colorIndex,
        int sizeLevel)
    {
        foreach (CellExperience experience in experiences)
        {
            if (experience.colorIndex == colorIndex &&
                experience.sizeLevel == sizeLevel)
            {
                return experience;
            }
        }

        return null;
    }

    private CellExperience GetBestExperience()
    {
        CellExperience best = null;

        foreach (CellExperience experience in experiences)
        {
            // Exigimos varias observaciones antes de confiar
            // en una estrategia.
            if (experience.attempts <
                minimumAttemptsToLearn)
            {
                continue;
            }

            if (best == null ||
                experience.LearningScore >
                best.LearningScore)
            {
                best = experience;
            }
        }

        return best;
    }

    private string GetColorName(int colorIndex)
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

    private string GetSizeName(int sizeLevel)
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

    private void EndRound()
    {
        Debug.Log(
            "FIN DE LA RONDA " +
            currentRound
        );

        // Registrar las células que sobrevivieron.
        if (cellSpawner != null)
        {
            cellSpawner.RegisterSurvivingCells();
        }

        // Actualizar estadísticas antes de mostrarlas.
        UpdateStatsUI();
        UpdateLearningTableUI();

        // Mostrar resultados.
        ShowRoundResults();

        // Eliminar las células de la ronda anterior.
        if (cellSpawner != null)
        {
            cellSpawner.ClearCells();
        }

        waitingForNextRound = true;
    }

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
                    GetColorName(best.colorIndex) +
                    " + " +
                    GetSizeName(best.sizeLevel) +
                    "\nSupervivencia: " +
                    (best.SurvivalRate * 100f)
                        .ToString("F1") +
                    "%";
            }
            else
            {
                resultLearningText.text =
                    "Mejor estrategia: recopilando datos...";
            }
        }

        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
    public void ContinueToNextRound()
    {
        if (!waitingForNextRound)
            return;

        // Si ya terminamos la última ronda,
        // mostramos la pantalla final.
        if (currentRound >= maxRounds)
        {
            ShowGameOver();
            return;
        }

        waitingForNextRound = false;

        currentRound++;
        currentTime = roundDuration;

        roundSurvivals = 0;
        roundEliminations = 0;

        UpdateRoundUI();
        UpdateStatsUI();
        UpdateLearningTableUI();

        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(false);
        }

        Debug.Log(
            "COMIENZA LA RONDA " +
            currentRound
        );

        if (cellSpawner != null)
        {
            cellSpawner.SpawnCells();
        }
    }

    private void ShowGameOver()
    {
        gameFinished = true;

        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(false);
        }

        if (cellSpawner != null)
        {
            cellSpawner.ClearCells();
        }

        if (finalScoreText != null)
        {
            finalScoreText.text =
                "Puntuación final: " +
                score;
        }

        if (finalRoundText != null)
        {
            finalRoundText.text =
                "Rondas completadas: " +
                maxRounds;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Debug.Log(
            "FIN DE LA PARTIDA"
        );
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Puntuación: " + score;
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text =
                "Tiempo: " +
                Mathf.CeilToInt(currentTime);
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
                    GetColorName(best.colorIndex) +
                    " + " +
                    GetSizeName(best.sizeLevel) +
                    " | " +
                    (best.SurvivalRate * 100f).ToString("F1") +
                    "%";
            }
            else
            {
                learningText.text =
                    "Aprendizaje: recopilando datos...";
            }
        }
    }
    private void UpdateLearningTableUI()
    {
        if (learningTableText == null)
            return;

        string table = "MEMORIA DE LA IA\n\n";

        table += "             PEQ    MED    GRA\n";

        string[] colorNames =
        {
        "Rojo",
        "Verde",
        "Azul",
        "Amarillo",
        "Magenta"
    };

        for (int colorIndex = 0;
             colorIndex < colorNames.Length;
             colorIndex++)
        {
            table += colorNames[colorIndex].PadRight(10);

            for (int sizeLevel = 0;
                 sizeLevel < 3;
                 sizeLevel++)
            {
                CellExperience experience =
                    FindExperience(
                        colorIndex,
                        sizeLevel
                    );

                float percentage = 0f;

                if (experience != null &&
                    experience.attempts > 0)
                {
                    percentage =
                        experience.SurvivalRate * 100f;
                }

                table +=
                    percentage.ToString("F0") +
                    "%".PadLeft(6);
            }

            table += "\n";
        }

        learningTableText.text = table;
    }
    private void UpdateLearningModeUI(
        string mode)
    {
        if (learningModeText != null)
        {
            learningModeText.text =
                "Modo IA: " + mode;
        }
    }
}