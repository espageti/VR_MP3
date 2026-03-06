using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Donut Settings")]
    [SerializeField] private GameObject donutPrefab;
    [SerializeField] private Transform donutSpawnPoint;
    [SerializeField] private TMP_Text donutCountText;
    [SerializeField] private float donutLifetime = 3f;

    [Header("Coffee Settings")]
    [SerializeField] private TMP_Text coffeeCountText;
    [SerializeField] private TMP_Text coffeeMultiplierText;
    [SerializeField] private float coffeeBaseRate = 0.1f;
    [SerializeField] private float coffeeMultiplierGainPerCoffee = 0.01f;
    private float coffeeCount;

    [Header("Coffee Unlock Panel")]
    [SerializeField] private GameObject coffeePanel;
    [SerializeField] private TMP_Text coffeeUnlockCostText;
    [SerializeField] private int coffeeUnlockCost = 100;
    [SerializeField] private Vector3 coffeePanelHiddenLocalPosition = new Vector3(0f, -3f, 0f);
    [SerializeField] private Vector3 coffeePanelShownLocalPosition = Vector3.zero;
    [SerializeField] private float coffeePanelRaiseSpeed = 3f;
    private bool coffeeUnlocked;

    [Header("Oompa Loompa Settings")]
    [SerializeField] private TMP_Text oompaCountText;
    [SerializeField] private TMP_Text oompaCostText;
    [SerializeField] private int oompaLoompaCount = 1;
    [SerializeField] private int oompaLoompaPrice = 3;
    [SerializeField] private float oompaLoompaPriceMultiplier = 10f;
    [SerializeField] private float baseProductionRate = 1f;

    [Header("Prestige Settings")]
    [SerializeField] private TMP_Text prestigeCountText;
    [SerializeField] private int prestigePrice = 3000;

    [Header("Power-ups")]
    [SerializeField] private float rateMultiplier = 1f;
    [SerializeField] private float coffeeMultiplier = 1f;

    private float donutCount;
    private string prestigeStars = "";
    private int defaultPrestigePrice;

    public int DonutCountInt => Mathf.FloorToInt(donutCount);
    public int CoffeeCountInt => Mathf.FloorToInt(coffeeCount);
    public int IceCreamCountInt => CoffeeCountInt;
    public int OompaLoompaCount => oompaLoompaCount;
    public int OompaLoompaPrice => oompaLoompaPrice;
    public int PrestigeCount => prestigeStars.Length;
    public int PrestigePrice => prestigePrice;
    public float RateMultiplier => rateMultiplier;
    public float CoffeeMultiplier => coffeeMultiplier;
    public bool CoffeeUnlocked => coffeeUnlocked;

    private void Awake()
    {
        defaultPrestigePrice = prestigePrice;

        if (oompaLoompaCount <= 0)
        {
            oompaLoompaCount = 1;
        }

        if (prestigePrice <= 0)
        {
            prestigePrice = 1;
        }
    }

    private void Start()
    {
        LoadGame();
        ApplyCoffeePanelStateInstant();
    }

    private void Update()
    {
        ProduceDonutsFromOompaLoompas();
        ProduceCoffee();
        UpdateCoffeePanelAnimation();
        UpdateUI();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveGame();
    }

    private void ProduceDonutsFromOompaLoompas()
    {
        if (oompaLoompaCount <= 0) return;

        // Euler integration: value += rate * deltaTime
        float rate = oompaLoompaCount * baseProductionRate * rateMultiplier * coffeeMultiplier;
        donutCount += rate * Time.deltaTime;
    }

    private void ProduceCoffee()
    {
        // Euler integration: value += rate * deltaTime
        float produced = coffeeBaseRate * coffeeMultiplier * Time.deltaTime;
        coffeeCount += produced;

        if (produced > 0f && coffeeMultiplierGainPerCoffee > 0f)
        {
            coffeeMultiplier += produced * coffeeMultiplierGainPerCoffee;
        }
    }

    private void UpdateUI()
    {
        if (donutCountText != null)
        {
            donutCountText.text = Mathf.FloorToInt(donutCount).ToString();
        }

        if (coffeeCountText != null)
        {
            coffeeCountText.text = Mathf.FloorToInt(coffeeCount).ToString();
        }

        if (coffeeMultiplierText != null)
        {
            coffeeMultiplierText.text = coffeeMultiplier.ToString("F2") + "x";
        }

        if (coffeeUnlockCostText != null)
        {
            coffeeUnlockCostText.text = coffeeUnlocked ? "Unlocked" : coffeeUnlockCost.ToString();
        }

        if (oompaCountText != null)
        {
            oompaCountText.text = oompaLoompaCount.ToString();
        }

        if (oompaCostText != null)
        {
            oompaCostText.text = oompaLoompaPrice.ToString();
        }

        if (prestigeCountText != null)
        {
            prestigeCountText.text = $"Prestige (Cost {prestigePrice:N0}): {prestigeStars}";
        }
    }

    public void SpawnDonut()
    {
        // if (donutPrefab == null)
        // {
        //     Debug.LogWarning("Donut prefab is not assigned.");
        //     return;
        // }

        // Vector3 spawnPosition = donutSpawnPoint != null ? donutSpawnPoint.position : transform.position;
        // Quaternion spawnRotation = donutSpawnPoint != null ? donutSpawnPoint.rotation : Quaternion.identity;

        // GameObject donut = Instantiate(donutPrefab, spawnPosition, spawnRotation);
        donutCount += 1;
    }

    public void BuyOompaLoompa()
    {
        if (Mathf.FloorToInt(donutCount) < oompaLoompaPrice) return;

        donutCount -= oompaLoompaPrice;
        oompaLoompaCount += 1;
        oompaLoompaPrice = Mathf.CeilToInt(oompaLoompaPrice * oompaLoompaPriceMultiplier);
    }

    public void BuyPrestige()
    {
        if (Mathf.FloorToInt(donutCount) < prestigePrice) return;

        prestigeStars += "*";
        ResetGame(true);
    }

    public void ApplyRateMultiplier(float multiplier)
    {
        rateMultiplier *= multiplier;
    }

    public void IncreaseCoffeeMultiplierGainPerCoffee(float amount)
    {
        if (amount <= 0f) return;
        coffeeMultiplierGainPerCoffee += amount;
    }

    public void TryUnlockCoffee()
    {
        Debug.Log("Attempting to unlock coffee... + Current donuts: " + Mathf.FloorToInt(donutCount) + ", Cost: " + coffeeUnlockCost);

        if (coffeeUnlocked) return;
        if (!TrySpend(coffeeUnlockCost)) return;
        
        Debug.Log("Coffee unlocked!");
        coffeeUnlocked = true;
    }


    public bool TrySpend(int amount)
    {
        if (Mathf.FloorToInt(donutCount) < amount) return false;
        donutCount -= amount;
        return true;
    }

    // ---- Save / Load / Idle Progress ----

    private void SaveGame()
    {
        PlayerPrefs.SetFloat("donutCount", donutCount);
        PlayerPrefs.SetFloat("coffeeCount", coffeeCount);
        PlayerPrefs.SetInt("oompaLoompaCount", oompaLoompaCount);
        PlayerPrefs.SetInt("oompaLoompaPrice", oompaLoompaPrice);
        PlayerPrefs.SetString("prestigeStars", prestigeStars);
        PlayerPrefs.SetInt("prestigePrice", prestigePrice);
        PlayerPrefs.SetFloat("rateMultiplier", rateMultiplier);
        PlayerPrefs.SetFloat("coffeeMultiplier", coffeeMultiplier);
        PlayerPrefs.SetInt("coffeeUnlocked", coffeeUnlocked ? 1 : 0);
        PlayerPrefs.SetString("lastSaveTime", System.DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
    }

    private void LoadGame()
    {
        if (!PlayerPrefs.HasKey("lastSaveTime")) return;

        donutCount = PlayerPrefs.GetFloat("donutCount", 0f);
        coffeeCount = PlayerPrefs.GetFloat("coffeeCount", PlayerPrefs.GetFloat("iceCreamCount", 0f));
        oompaLoompaCount = PlayerPrefs.GetInt("oompaLoompaCount", 1);
        oompaLoompaPrice = PlayerPrefs.GetInt("oompaLoompaPrice", 3);
        prestigeStars = PlayerPrefs.GetString("prestigeStars", "");
        if (string.IsNullOrEmpty(prestigeStars))
        {
            int legacyPrestigeCount = PlayerPrefs.GetInt("prestigeCount", 0);
            if (legacyPrestigeCount > 0)
            {
                prestigeStars = new string('*', legacyPrestigeCount);
            }
        }
        prestigePrice = PlayerPrefs.GetInt("prestigePrice", defaultPrestigePrice);
        rateMultiplier = PlayerPrefs.GetFloat("rateMultiplier", 1f);
        coffeeMultiplier = PlayerPrefs.GetFloat("coffeeMultiplier", 1f);
        coffeeUnlocked = PlayerPrefs.GetInt("coffeeUnlocked", 0) == 1;

        // Idle Progress: one Euler step for time away
        string lastTime = PlayerPrefs.GetString("lastSaveTime", "");
        if (System.DateTime.TryParse(lastTime, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out System.DateTime lastSave))
        {
            float secondsAway = (float)(System.DateTime.UtcNow - lastSave).TotalSeconds;

            float donutRate = oompaLoompaCount * baseProductionRate * rateMultiplier * coffeeMultiplier;
            donutCount += donutRate * secondsAway;

            float coffeeProduced = coffeeBaseRate * coffeeMultiplier * secondsAway;
            coffeeCount += coffeeProduced;
            coffeeMultiplier += coffeeProduced * coffeeMultiplierGainPerCoffee;
        }
    }

    private void ApplyCoffeePanelStateInstant()
    {
        if (coffeePanel == null) return;

        coffeePanel.SetActive(true);
        Transform panelTransform = coffeePanel.transform;
        panelTransform.localPosition = coffeeUnlocked ? coffeePanelShownLocalPosition : coffeePanelHiddenLocalPosition;
    }

    private void UpdateCoffeePanelAnimation()
    {
        if (coffeePanel == null) return;

        coffeePanel.SetActive(true);
        Transform panelTransform = coffeePanel.transform;
        Vector3 target = coffeeUnlocked ? coffeePanelShownLocalPosition : coffeePanelHiddenLocalPosition;

        panelTransform.localPosition = Vector3.MoveTowards(
            panelTransform.localPosition,
            target,
            coffeePanelRaiseSpeed * Time.deltaTime
        );
    }

    // ---- Reset ----

    public void ResetGame()
    {
        ResetGame(false);
    }

    private void ResetGame(bool preservePrestigeStars)
    {
        string savedPrestigeStars = preservePrestigeStars ? prestigeStars : "";

        donutCount = 0f;
        coffeeCount = 0f;
        oompaLoompaCount = 1;
        oompaLoompaPrice = 3;
        prestigePrice = defaultPrestigePrice;
        rateMultiplier = 1f;
        coffeeMultiplier = 1f;
        coffeeUnlocked = false;
        prestigeStars = savedPrestigeStars;

        PlayerPrefs.DeleteAll();
        SaveGame();

        ApplyCoffeePanelStateInstant();
    }
}
