using System.Collections.Generic;
using UnityEngine;

public class CellSpawner : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private int cellsToSpawn = 5;

    private readonly List<GameObject> activeCells =
        new List<GameObject>();

    // Posiciones relativas a la cámara.
    // De esta forma funcionan aunque cambie
    // el tamaño de la ventana Game.
    private readonly Vector2[] spawnPoints =
    {
        new Vector2(0.20f, 0.32f),
        new Vector2(0.50f, 0.32f),
        new Vector2(0.80f, 0.32f),

        new Vector2(0.32f, 0.68f),
        new Vector2(0.68f, 0.68f)
    };

    public void SpawnCells()
    {
        if (cellPrefab == null)
        {
            Debug.LogError(
                "CellSpawner: Cell Prefab no está asignado."
            );

            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "CellSpawner: No existe una Main Camera."
            );

            return;
        }

        // Por seguridad, eliminamos cualquier célula anterior.
        ClearCells();

        int amount =
            Mathf.Min(
                cellsToSpawn,
                spawnPoints.Length
            );

        for (int i = 0; i < amount; i++)
        {
            Vector3 worldPosition =
                GetWorldPosition(
                    mainCamera,
                    spawnPoints[i]
                );

            GameObject newCell =
                Instantiate(
                    cellPrefab,
                    worldPosition,
                    Quaternion.identity,
                    transform
                );

            newCell.name =
                "Cell_" + (i + 1);

            activeCells.Add(newCell);
        }

        Debug.Log(
            "CELULAS CREADAS: " +
            activeCells.Count
        );
    }

    private Vector3 GetWorldPosition(
        Camera camera,
        Vector2 viewportPosition)
    {
        Vector3 viewportPoint =
            new Vector3(
                viewportPosition.x,
                viewportPosition.y,
                Mathf.Abs(
                    camera.transform.position.z
                )
            );

        Vector3 worldPosition =
            camera.ViewportToWorldPoint(
                viewportPoint
            );

        worldPosition.z = 0f;

        return worldPosition;
    }

    public void RegisterSurvivingCells()
    {
        foreach (GameObject cellObject in activeCells)
        {
            if (cellObject == null)
                continue;

            Cell cell =
                cellObject.GetComponent<Cell>();

            if (cell != null)
            {
                cell.RegisterSurvival();
            }
        }
    }

    public void ClearCells()
    {
        foreach (GameObject cellObject in activeCells)
        {
            if (cellObject != null)
            {
                Destroy(cellObject);
            }
        }

        activeCells.Clear();
    }
}