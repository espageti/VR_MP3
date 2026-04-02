using UnityEngine;
using UnityEngine.XR;
using TMPro;
using System.Collections.Generic;

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
    [SerializeField] private TMP_Text coffeePerCoffeeMultiplierText;
    [SerializeField] private TMP_Text coffeeProductionText;
    [SerializeField] private TMP_Text coffeeMultiplierUpgradeCostText;
    [SerializeField] private TMP_Text coffeeProductionUpgradeCostText;
    [SerializeField] private float coffeeProductionRate = 0.1f;
    [SerializeField] private float coffeeMultiplierGainPerCoffee = 0.01f;
    private float coffeeCount;

    [Header("Coffee Upgrade Costs")]
    [SerializeField] private int coffeeMultiplierUpgradeCost = 10;
    [SerializeField] private float coffeeMultiplierUpgradeCostMultiplier = 2f;
    [SerializeField] private int coffeeProductionUpgradeCost = 10;
    [SerializeField] private float coffeeProductionUpgradeCostMultiplier = 2f;

    [Header("Coffee Unlock Panel")]
    [SerializeField] private GameObject coffeePanel;
    [SerializeField] private TMP_Text coffeeUnlockCostText;
    [SerializeField] private int coffeeUnlockCost = 100;
    [SerializeField] private Vector3 coffeePanelHiddenLocalPosition = new Vector3(0f, -3f, 0f);
    [SerializeField] private Vector3 coffeePanelShownLocalPosition = Vector3.zero;
    [SerializeField] private float coffeePanelRaiseSpeed = 1f;
    [SerializeField] private float coffeePanelMoveDuration = 1f;
    [SerializeField] private float coffeePanelOvershootDistance = 0.4f;
    private bool coffeeUnlocked;

    [Header("Oompa Loompa Settings")]
    [SerializeField] private TMP_Text oompaCountText;
    [SerializeField] private TMP_Text oompaCostText;
    [SerializeField] private GameObject oompaLoompaPrefab;
    [SerializeField] private Transform oompaLoompaSpawnRoot;
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
    [SerializeField] private float coffeeProductionMultiplier = 1f;

    private float donutCount;
    private string prestigeStars = "";
    private int defaultPrestigePrice;
    private float defaultCoffeeProductionRate;
    private int defaultCoffeeMultiplierUpgradeCost;
    private int defaultCoffeeProductionUpgradeCost;

    public int DonutCountInt => Mathf.FloorToInt(donutCount);
    public int CoffeeCountInt => Mathf.FloorToInt(coffeeCount);
    public int IceCreamCountInt => CoffeeCountInt;
    public int OompaLoompaCount => oompaLoompaCount;
    public int OompaLoompaPrice => oompaLoompaPrice;
    public int PrestigeCount => prestigeStars.Length;
    public int PrestigePrice => prestigePrice;
    public float RateMultiplier => rateMultiplier;
    public float CoffeeProductionMultiplier => coffeeProductionMultiplier;
    public bool CoffeeUnlocked => coffeeUnlocked;

    private void Awake()
    {
        defaultPrestigePrice = prestigePrice;
        defaultCoffeeProductionRate = coffeeProductionRate;
        defaultCoffeeMultiplierUpgradeCost = coffeeMultiplierUpgradeCost;
        defaultCoffeeProductionUpgradeCost = coffeeProductionUpgradeCost;
        
        if (oompaLoompaCount <= 0)
        {
            oompaLoompaCount = 1;
        }

        if (prestigePrice <= 0)
        {
            prestigePrice = 1;
        }

        if (coffeeMultiplierUpgradeCost <= 0)
        {
            coffeeMultiplierUpgradeCost = 1;
        }

        if (coffeeProductionUpgradeCost <= 0)
        {
            coffeeProductionUpgradeCost = 1;
        }
    }

    private void Start()
    {
        bool loadedGame = LoadGame();

        if (loadedGame)
        {
            SpawnMissingLoadedOompaLoompas();
        }

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
        float rate = oompaLoompaCount * baseProductionRate * rateMultiplier * GetCoffeeMultiplierGain();
        donutCount += rate * Time.deltaTime;
    }

    private float GetCoffeeMultiplierGain()
    {
        return CoffeeCountInt * coffeeMultiplierGainPerCoffee * coffeeMultiplier + 1f;
    }

    private void ProduceCoffee()
    {
        // Euler integration: value += rate * deltaTime
        if (!coffeeUnlocked) return;
        float produced = coffeeProductionRate * coffeeProductionMultiplier * Time.deltaTime;
        coffeeCount += produced;

    }

    private void UpdateUI()
    {
        if (donutCountText != null)
        {
            donutCountText.text = Mathf.FloorToInt(donutCount).ToString();
        }

        if (coffeeCountText != null)
        {
            coffeeCountText.text = CoffeeCountInt.ToString();
        }

        if (coffeeMultiplierText != null)
        {
            coffeeMultiplierText.text = GetCoffeeMultiplierGain().ToString("F2") + "x";
        }

        if (coffeePerCoffeeMultiplierText != null)
        {
            float perCoffeeMultiplierValue = coffeeMultiplierGainPerCoffee * coffeeMultiplier;
            coffeePerCoffeeMultiplierText.text = perCoffeeMultiplierValue.ToString("F3") + "x/coffee";
        }

        if (coffeeProductionText != null)
        {
            float coffeePerSecond = coffeeUnlocked
                ? coffeeProductionRate * coffeeProductionMultiplier
                : 0f;
            coffeeProductionText.text = coffeePerSecond.ToString("F2") + "/s";
        }

        if (coffeeMultiplierUpgradeCostText != null)
        {
            coffeeMultiplierUpgradeCostText.text = coffeeMultiplierUpgradeCost.ToString();
        }

        if (coffeeProductionUpgradeCostText != null)
        {
            coffeeProductionUpgradeCostText.text = coffeeProductionUpgradeCost.ToString();
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

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SendHapticPulse()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        foreach (InputDevice device in devices)
        {
            HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, hapticIntensity, hapticDuration);
            }
        }
    }

    private void PlayParticles(ParticleSystem particles, int count)
    {
        if (particles != null)
        {
            particles.Emit(count);
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
        PlaySound(spawnDonutSound);
        SendHapticPulse();
        PlayParticles(spawnDonutParticles, 1);
    }

    public void BuyOompaLoompa()
    {
        if (Mathf.FloorToInt(donutCount) < oompaLoompaPrice) return;

        donutCount -= oompaLoompaPrice;
        oompaLoompaCount += 1;
        SpawnOompaLoompa();
        oompaLoompaPrice = Mathf.CeilToInt(oompaLoompaPrice * oompaLoompaPriceMultiplier);
        PlaySound(buyOompaLoompaSound);
        SendHapticPulse();
        PlayParticles(buyOompaLoompaParticles, 1);
    }

    private void SpawnOompaLoompa()
    {
        if (oompaLoompaPrefab == null)
        {
            Debug.LogWarning("GameManager: Oompa Loompa prefab is not assigned.", this);
            return;
        }

        Vector3 spawnPosition = oompaLoompaSpawnRoot != null
            ? oompaLoompaSpawnRoot.position
            : transform.position;

        Quaternion spawnRotation = oompaLoompaSpawnRoot != null
            ? oompaLoompaSpawnRoot.rotation
            : Quaternion.identity;

        Instantiate(oompaLoompaPrefab, spawnPosition, spawnRotation);
    }

    public void BuyPrestige()
    {
        if (Mathf.FloorToInt(donutCount) < prestigePrice) return;

        prestigeStars += "*";
        PlaySound(buyPrestigeSound);
        SendHapticPulse();
        PlayParticles(buyPrestigeParticles, 10);
        ResetGame(true);
    }

    public void BuyCoffeeMultiplierUpgrade()
    {
        if (!TrySpendCoffee(coffeeMultiplierUpgradeCost)) return;

        coffeeMultiplierGainPerCoffee += 0.01f;
        coffeeMultiplierUpgradeCost = Mathf.CeilToInt(coffeeMultiplierUpgradeCost * coffeeMultiplierUpgradeCostMultiplier);
        PlaySound(buyCoffeeMultiplierUpgradeSound);
        SendHapticPulse();
        PlayParticles(buyCoffeeMultiplierUpgradeParticles, 3);
    }

    public void BuyCoffeeProductionUpgrade()
    {
        if (!TrySpendCoffee(coffeeProductionUpgradeCost)) return;

        coffeeProductionRate += 0.1f;
        coffeeProductionUpgradeCost = Mathf.CeilToInt(coffeeProductionUpgradeCost * coffeeProductionUpgradeCostMultiplier);
        PlaySound(buyCoffeeProductionUpgradeSound);
        SendHapticPulse();
        PlayParticles(buyCoffeeProductionUpgradeParticles, 3);
    }

    public void IncreaseCoffeeMultiplierGainPerCoffee(float amount)
    {
        if (amount <= 0f) return;
        
        coffeeMultiplierGainPerCoffee += amount;
    }

    public void IncreaseCoffeeProductionPerSecond(float amount)
    {
        if (amount <= 0f) return;
        coffeeProductionRate += amount;
    }

    public void TryUnlockCoffee()
    {
        Debug.Log("Attempting to unlock coffee... + Current donuts: " + Mathf.FloorToInt(donutCount) + ", Cost: " + coffeeUnlockCost);

        if (coffeeUnlocked) return;
        if (!TrySpend(coffeeUnlockCost)) return;
        
        Debug.Log("Coffee unlocked!");
        coffeeUnlocked = true;
        PlaySound(unlockCoffeeSound);
        SendHapticPulse();
        PlayParticles(unlockCoffeeParticles, 30);
    }


    public bool TrySpend(int amount)
    {
        if (Mathf.FloorToInt(donutCount) < amount) return false;
        donutCount -= amount;
        return true;
    }

    public bool TrySpendCoffee(int amount)
    {
        if (Mathf.FloorToInt(coffeeCount) < amount) return false;
        coffeeCount -= amount;
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
        PlayerPrefs.SetFloat("coffeeProductionMultiplier", coffeeProductionMultiplier);
        PlayerPrefs.SetFloat("coffeeProductionRate", coffeeProductionRate);
        PlayerPrefs.SetInt("coffeeMultiplierUpgradeCost", coffeeMultiplierUpgradeCost);
        PlayerPrefs.SetInt("coffeeProductionUpgradeCost", coffeeProductionUpgradeCost);
        PlayerPrefs.SetInt("coffeeUnlocked", coffeeUnlocked ? 1 : 0);
        PlayerPrefs.SetString("lastSaveTime", System.DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
    }

    private bool LoadGame()
    {
        if (!PlayerPrefs.HasKey("lastSaveTime")) return false;

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
        coffeeProductionMultiplier = PlayerPrefs.GetFloat("coffeeProductionMultiplier", 1f);
        coffeeProductionRate = PlayerPrefs.GetFloat("coffeeProductionRate", defaultCoffeeProductionRate);
        coffeeMultiplierUpgradeCost = PlayerPrefs.GetInt("coffeeMultiplierUpgradeCost", defaultCoffeeMultiplierUpgradeCost);
        coffeeProductionUpgradeCost = PlayerPrefs.GetInt("coffeeProductionUpgradeCost", defaultCoffeeProductionUpgradeCost);
        coffeeUnlocked = PlayerPrefs.GetInt("coffeeUnlocked", 0) == 1;

        // Idle Progress: one Euler step for time away
        string lastTime = PlayerPrefs.GetString("lastSaveTime", "");
        if (System.DateTime.TryParse(lastTime, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out System.DateTime lastSave))
        {
            float secondsAway = (float)(System.DateTime.UtcNow - lastSave).TotalSeconds;

            float donutRate = oompaLoompaCount * baseProductionRate * rateMultiplier * coffeeMultiplier;
            donutCount += donutRate * secondsAway;

            float coffeeProduced = coffeeProductionRate * coffeeProductionMultiplier * secondsAway;
            coffeeCount += coffeeProduced;
        }

        return true;
    }

    private void SpawnMissingLoadedOompaLoompas()
    {
        if (oompaLoompaPrefab == null)
        {
            Debug.LogWarning("GameManager: Oompa Loompa prefab is not assigned.", this);
            return;
        }

        int existingOompas = FindObjectsOfType<OompaLoompa>().Length;
        int oompasToSpawn = Mathf.Max(0, oompaLoompaCount - existingOompas);

        for (int i = 0; i < oompasToSpawn; i++)
        {
            SpawnOompaLoompa();
        }
    }

    private void ApplyCoffeePanelStateInstant()
    {
        if (coffeePanel == null) return;

        coffeePanel.SetActive(true);
        Transform panelTransform = coffeePanel.transform;
        panelTransform.localPosition = coffeeUnlocked ? coffeePanelShownLocalPosition : coffeePanelHiddenLocalPosition;
    }

    [Header("Coffee Panel Sound Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip panelMoveSound;
    [SerializeField] private AudioClip panelFinishSound;

    [Header("Button Sound Effects")]
    [SerializeField] private AudioClip spawnDonutSound;
    [SerializeField] private AudioClip buyOompaLoompaSound;
    [SerializeField] private AudioClip buyPrestigeSound;
    [SerializeField] private AudioClip buyCoffeeMultiplierUpgradeSound;
    [SerializeField] private AudioClip buyCoffeeProductionUpgradeSound;
    [SerializeField] private AudioClip unlockCoffeeSound;

    [Header("Haptic Feedback")]
    [SerializeField] private float hapticIntensity = 0.3f;
    [SerializeField] private float hapticDuration = 0.1f;

    [Header("Button Particle Effects")]
    [SerializeField] private ParticleSystem spawnDonutParticles;
    [SerializeField] private ParticleSystem buyOompaLoompaParticles;
    [SerializeField] private ParticleSystem buyPrestigeParticles;
    [SerializeField] private ParticleSystem buyCoffeeMultiplierUpgradeParticles;
    [SerializeField] private ParticleSystem buyCoffeeProductionUpgradeParticles;
    [SerializeField] private ParticleSystem unlockCoffeeParticles;

    private bool wasUnlockedLastFrame;
    private bool isMoving;
    private Vector3 panelMoveStartPosition;
    private Vector3 panelMoveTargetPosition;
    private Vector3 panelMoveOvershootPosition;
    private float panelMoveElapsed;
    private bool panelMoveSettling;

    private void UpdateCoffeePanelAnimation()
    {
        if (coffeePanel == null) return;

        coffeePanel.SetActive(true);

        Transform panelTransform = coffeePanel.transform;
        Vector3 target = coffeeUnlocked ? coffeePanelShownLocalPosition : coffeePanelHiddenLocalPosition;

        // Detect movement start (state change)
        if (coffeeUnlocked != wasUnlockedLastFrame)
        {
            if (audioSource != null && panelMoveSound != null)
            {
                audioSource.PlayOneShot(panelMoveSound);
            }

            wasUnlockedLastFrame = coffeeUnlocked;
            isMoving = true;

            panelMoveStartPosition = panelTransform.localPosition;
            panelMoveTargetPosition = target;
            panelMoveElapsed = 0f;
            panelMoveSettling = false;

            Vector3 moveDirection = panelMoveTargetPosition - panelMoveStartPosition;
            if (moveDirection.sqrMagnitude > 0.000001f)
            {
                moveDirection.Normalize();
                panelMoveOvershootPosition = panelMoveTargetPosition + (moveDirection * coffeePanelOvershootDistance);
            }
            else
            {
                panelMoveOvershootPosition = panelMoveTargetPosition;
            }
        }

        if (isMoving)
        {
            float duration = Mathf.Max(0.01f, coffeePanelMoveDuration / Mathf.Max(0.01f, coffeePanelRaiseSpeed));
            panelMoveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(panelMoveElapsed / duration);

            if (!panelMoveSettling)
            {
                float easedOut = EaseOutCubic(t);
                panelTransform.localPosition = Vector3.LerpUnclamped(panelMoveStartPosition, panelMoveOvershootPosition, easedOut);

                if (t >= 1f)
                {
                    panelMoveSettling = true;
                    panelMoveStartPosition = panelTransform.localPosition;
                    panelMoveElapsed = 0f;
                }
            }
            else
            {
                float easedInOut = EaseInOutCubic(t);
                panelTransform.localPosition = Vector3.LerpUnclamped(panelMoveStartPosition, panelMoveTargetPosition, easedInOut);

                if (t >= 1f)
                {
                    panelTransform.localPosition = panelMoveTargetPosition;
                    isMoving = false;

                    if (audioSource != null && panelFinishSound != null)
                    {
                        audioSource.PlayOneShot(panelFinishSound);
                    }
                }
            }
        }
        else
        {
            panelTransform.localPosition = target;
        }
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }

    private static float EaseInOutCubic(float t)
    {
        if (t < 0.5f)
        {
            return 4f * t * t * t;
        }

        float adjusted = -2f * t + 2f;
        return 1f - ((adjusted * adjusted * adjusted) / 2f);
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
        coffeeProductionMultiplier = 1f;
        coffeeProductionRate = defaultCoffeeProductionRate;
        coffeeMultiplierUpgradeCost = defaultCoffeeMultiplierUpgradeCost;
        coffeeProductionUpgradeCost = defaultCoffeeProductionUpgradeCost;
        coffeeUnlocked = false;
        prestigeStars = savedPrestigeStars;

        PlayerPrefs.DeleteAll();
        SaveGame();

        ApplyCoffeePanelStateInstant();
    }
}
