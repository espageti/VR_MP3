using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject donutPrefab;
    [SerializeField] private Transform donutSpawnPoint;
    [SerializeField] private int donutCount;
    [SerializeField] private TMP_Text donutCountText;
    [SerializeField] private TMP_Text oompaCountText;
    [SerializeField] private int oompaLoompaCount;
    [SerializeField] private int oompaLoompaPrice = 3;
    [SerializeField] private float oompaLoompaPriceMultiplier = 1.1f;

    private float donutProductionTimer;
    private const float DonutProductionInterval = 1f;

    public int DonutCount => donutCount;
    public int OompaLoompaCount => oompaLoompaCount;
    public int OompaLoompaPrice => oompaLoompaPrice;

    private void Update()
    {
        ProduceDonutsFromOompaLoompas();

        if (donutCountText != null)
        {
            donutCountText.text = donutCount.ToString();
        }

        if (oompaCountText != null)
        {
            oompaCountText.text = oompaLoompaCount.ToString();
        }
    }

    private void ProduceDonutsFromOompaLoompas()
    {
        if (oompaLoompaCount <= 0)
        {
            return;
        }

        donutProductionTimer += Time.deltaTime;

        while (donutProductionTimer >= DonutProductionInterval)
        {
            donutCount += oompaLoompaCount;
            donutProductionTimer -= DonutProductionInterval;
        }
    }

    public void SpawnDonut()
    {
        if (donutPrefab == null)
        {
            Debug.LogWarning("Donut prefab is not assigned.");
            return;
        }

        Vector3 spawnPosition = donutSpawnPoint != null ? donutSpawnPoint.position : transform.position;
        Quaternion spawnRotation = donutSpawnPoint != null ? donutSpawnPoint.rotation : Quaternion.identity;

        Instantiate(donutPrefab, spawnPosition, spawnRotation);
        donutCount += 1;
    }

    public void BuyOompaLoompa()
    {
        if (donutCount < oompaLoompaPrice)
        {
            return;
        }

        donutCount -= oompaLoompaPrice;
        oompaLoompaCount += 1;
        oompaLoompaPrice = Mathf.CeilToInt(oompaLoompaPrice * oompaLoompaPriceMultiplier);
    }
}
