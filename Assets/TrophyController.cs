using UnityEngine;
using TMPro; // Essential for TMP references

public class TrophyController : MonoBehaviour
{
    public enum TrophyType { OompaMaster, CoffeeAddict, PrestigeLegend }
    
    [Header("Threshold Settings")]
    [SerializeField] private TrophyType type;
    [SerializeField] private int threshold;

    [Header("Individual References")]
    [SerializeField] private GameObject trophyMesh; 
    // Using TMP_Text allows you to drag ANY TextMeshPro object here
    [SerializeField] private TMP_Text trophyText; 

    [Header("Juice & Feedback")]
    [SerializeField] private ParticleSystem unlockParticles;
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private AudioSource spatialAudioSource;
    
    [Header("Ease Settings")]
    [SerializeField] private float easeK = 5f; 

    [Header("Game Manager")]
    [SerializeField] private GameManager gm;
    
    private bool isUnlocked = false;
    private bool isScaling = false;
    
    private Vector3 meshTargetScale;
    private Vector3 textTargetScale;

    void Start()
    {
        gm = Object.FindFirstObjectByType<GameManager>();
        
        if (trophyMesh != null) meshTargetScale = trophyMesh.transform.localScale;
        if (trophyText != null) textTargetScale = trophyText.transform.localScale;

        InitialHide();
    }

    void Update()
    {
        if (!isUnlocked) CheckUnlockCondition();
        if (isScaling) ApplyJuicyEase();
    }

    private void CheckUnlockCondition()
    {
        bool met = false;
        if (gm == null) return;

        switch (type)
        {
            case TrophyType.OompaMaster: met = gm.OompaLoompaCount >= threshold; break;
            case TrophyType.CoffeeAddict: met = gm.CoffeeCountInt >= threshold; break;
            case TrophyType.PrestigeLegend: met = gm.PrestigeCount >= threshold; break;
        }

        if (met) TriggerUnlock();
    }

    private void TriggerUnlock()
    {
        isUnlocked = true;
        isScaling = true;

        if (trophyMesh != null) trophyMesh.SetActive(true);
        if (trophyText != null) trophyText.gameObject.SetActive(true);

        if (unlockParticles != null) unlockParticles.Play();
        if (unlockSound != null && spatialAudioSource != null)
        {
            spatialAudioSource.PlayOneShot(unlockSound);
        }
    }

    private void InitialHide()
    {
        if (trophyMesh != null)
        {
            trophyMesh.transform.localScale = Vector3.zero;
            trophyMesh.SetActive(false);
        }
        if (trophyText != null)
        {
            trophyText.transform.localScale = Vector3.zero;
            trophyText.gameObject.SetActive(false);
        }
    }

    private void ApplyJuicyEase()
    {
        bool meshDone = true;
        bool textDone = true;

        if (trophyMesh != null)
        {
            Vector3 mScale = trophyMesh.transform.localScale;
            trophyMesh.transform.localScale += easeK * (meshTargetScale - mScale) * Time.deltaTime;
            if (Vector3.Distance(trophyMesh.transform.localScale, meshTargetScale) > 0.001f) meshDone = false;
        }

        if (trophyText != null)
        {
            Vector3 tScale = trophyText.transform.localScale;
            // Using trophyText.transform.localScale works for both UI and 3D
            trophyText.transform.localScale += easeK * (textTargetScale - tScale) * Time.deltaTime;
            if (Vector3.Distance(trophyText.transform.localScale, textTargetScale) > 0.001f) textDone = false;
        }

        if (meshDone && textDone) isScaling = false;
    }
}