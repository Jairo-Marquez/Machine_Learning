using System.Collections.Generic;
using UnityEngine;

public class CellSpawner : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private int cellsToSpawn = 5;

    [Header("Posiciones de aparición")]
    [SerializeField] private float screenMargin = 0.15f;

    private List<GameObject> activeCells =
        new List<GameObject>();

    // Cinco posiciones fijas dentro de la pantalla.
    // Usamos coordenadas de Viewport:
    // X = 0 izquierda, 1 derecha
    // Y = 0 abajo, 1 arriba
    private readonly Vector2[] spawnPoints =
    {
        new Vector2(0.20f, 0.40f),
        new Vector2(0.50f, 0.40f),
        new Vector2(0.80f, 0.40f),
        new Vector2(0.30f, 0.70f),
        new Vector2(0.70f, 0.70f)
    };

    public void SpawnCells()
    {
        if (cellPrefab == null)
        {
            Debug.LogError(
                "CellSpawner: No se ha asignado Cell Prefab."
            );

            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "CellSpawner: No se encontró Main Camera."
            );

            return;
        }

        activeCells.Clear();

        int amountToSpawn =
            Mathf.Min(
                cellsToSpawn,
                spawnPoints.Length
            );

        for (int i = 0; i < amountToSpawn; i++)
        {
            Vector3 spawnPosition =
                ViewportToWorldPosition(
                    mainCamera,
                    spawnPoints[i]
                );

            GameObject newCell =
                Instantiate(
                    cellPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

            activeCells.Add(newCell);
        }

        Debug.Log(
            "Células generadas: " +
            activeCells.Count
        );
    }

    private Vector3 ViewportToWorldPosition(
        Camera camera,
        Vector2 viewportPosition)
    {
        // Aplicamos un pequeño margen para alejarnos
        // de los bordes de la pantalla.
        float x = Mathf.Clamp(
            viewportPosition.x,
            screenMargin,
            1f - screenMargin
        );

        float y = Mathf.Clamp(
            viewportPosition.y,
            screenMargin,
            1f - screenMargin
        );

        Vector3 viewportPoint =
            new Vector3(
                x,
                y,
                Mathf.Abs(camera.transform.position.z)
            );

        Vector3 worldPosition =
            camera.ViewportToWorldPoint(
                viewportPoint
            );

        // Como nuestro juego es 2D,
        // todas las células deben estar en Z = 0.
        worldPosition.z = 0f;

        return worldPosition;
    }

    public void RegisterSurvivingCells()
    {
        foreach (GameObject cellObject in activeCells)
        {
            if (cellObject != null)
            {
                Cell cell =
                    cellObject.GetComponent<Cell>();

                if (cell != null)
                {
                    cell.RegisterSurvival();
                }
            }
        }
    }

    public void ClearCells()
    {
        foreach (GameObject cell in activeCells)
        {
            if (cell != null)
            {
                Destroy(cell);
            }
        }

        activeCells.Clear();
    }
}