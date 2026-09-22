using UnityEngine;

public class CellSpawner : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private int cellsToSpawn = 5;

    [Header("Área de aparición")]
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 4f;

    private void Start()
    {
        SpawnCells();
    }

    public void SpawnCells()
    {
        for (int i = 0; i < cellsToSpawn; i++)
        {
            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);

            Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);

            Instantiate(cellPrefab, spawnPosition, Quaternion.identity);
        }
    }
}