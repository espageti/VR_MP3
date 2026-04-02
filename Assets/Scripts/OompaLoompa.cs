using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class OompaLoompa : MonoBehaviour
{
    [Header("Spawn Area (World Space)")]
    [SerializeField] private Vector2 spawnSquareCenter = Vector2.zero;
    [SerializeField] private Vector2 spawnSquareSize = new Vector2(4f, 4f);

    [Header("Pop In")]
    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private float popOvershootScale = 1.15f;

    [Header("Loop Animation Clips")]
    [SerializeField] private Animator animatorComponent;
    [SerializeField] private Animation animationComponent;
    [SerializeField] private AnimationClip loopClipA;
    [SerializeField] private AnimationClip loopClipB;
    [SerializeField] private AnimationClip loopClipC;

    [Header("Eye Tracking")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private bool autoFindMainCameraTarget = true;
    [SerializeField] private float eyeTurnSpeed = 12f;

    [Header("Eye Blink")]
    [SerializeField] private bool enableBlinking = true;
    [SerializeField] private float blinkMinInterval = 4.5f;
    [SerializeField] private float blinkMaxInterval = 4.5f;
    [SerializeField] private float blinkDuration = 0.08f;

    [Header("Eye Widen")]
    [SerializeField] private float widenedScaleMultiplier = 2f;
    [SerializeField] private float widenInSpeed = 12f;
    [SerializeField] private float widenOutSpeed = 8f;
    [SerializeField] private float widenHoldDuration = 1f;

    private Vector3 baseScale;
    private PlayableGraph loopClipGraph;
    private bool hasLoopClipGraph;
    private Transform[] eyeTransforms;
    private Renderer[] eyeRenderers;
    private Transform[] eyeScaleTransforms;
    private Vector3[] baseEyeScales;
    private Coroutine blinkCoroutine;
    private float currentEyeScaleMultiplier = 1f;
    private bool eyeWidenSignalActive;
    private float widenHoldTimer;

    private void OnEnable()
    {
        GameManager.BuyActionTriggered += HandleGlobalBuyAction;
    }

    private void Awake()
    {
        baseScale = transform.localScale;

        if (animatorComponent == null)
        {
            animatorComponent = GetComponent<Animator>();
        }

        if (animationComponent == null)
        {
            animationComponent = GetComponent<Animation>();
        }
    }

    private void Start()
    {
        CacheEyeTransforms();
        CacheEyeRenderersAndScales();
        TryAssignDefaultEyeTarget();
        StartCoroutine(PopThenPlayRandomLoopAnimation());

        if (enableBlinking)
        {
            blinkCoroutine = StartCoroutine(BlinkRoutine());
        }
    }

    private void LateUpdate()
    {
        UpdateEyeTracking();
        UpdateEyeWiden();
    }

    public void SetEyeTarget(Transform target)
    {
        playerTarget = target;
    }

    public void SignalEyeWiden()
    {
        widenHoldTimer = Mathf.Max(0f, widenHoldDuration);
    }

    public void SetEyeWidenSignal(bool active)
    {
        eyeWidenSignalActive = active;
        if (active)
        {
            widenHoldTimer = Mathf.Max(0f, widenHoldDuration);
        }
    }

    private void CacheEyeTransforms()
    {
        List<Transform> eyes = new List<Transform>();
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform child = allChildren[i];
            if (child == transform) continue;

            if (child.tag == "Eye")
            {
                eyes.Add(child);
            }
        }

        eyeTransforms = eyes.ToArray();
    }

    private void CacheEyeRenderersAndScales()
    {
        if (eyeTransforms == null || eyeTransforms.Length == 0)
        {
            eyeRenderers = new Renderer[0];
            eyeScaleTransforms = new Transform[0];
            baseEyeScales = new Vector3[0];
            return;
        }

        List<Renderer> rendererList = new List<Renderer>();
        List<Transform> scaleTransformList = new List<Transform>();
        HashSet<Transform> uniqueScaleTransforms = new HashSet<Transform>();

        for (int i = 0; i < eyeTransforms.Length; i++)
        {
            Transform eye = eyeTransforms[i];
            if (eye == null) continue;

            Renderer[] childRenderers = eye.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < childRenderers.Length; rendererIndex++)
            {
                Renderer eyeRenderer = childRenderers[rendererIndex];
                if (eyeRenderer == null) continue;
                rendererList.Add(eyeRenderer);
            }

            Transform[] childTransforms = eye.GetComponentsInChildren<Transform>(true);
            for (int transformIndex = 0; transformIndex < childTransforms.Length; transformIndex++)
            {
                Transform childTransform = childTransforms[transformIndex];
                if (childTransform == null) continue;

                if (uniqueScaleTransforms.Add(childTransform))
                {
                    scaleTransformList.Add(childTransform);
                }
            }
        }

        eyeRenderers = rendererList.ToArray();
        eyeScaleTransforms = scaleTransformList.ToArray();
        baseEyeScales = new Vector3[eyeScaleTransforms.Length];

        for (int i = 0; i < eyeScaleTransforms.Length; i++)
        {
            baseEyeScales[i] = eyeScaleTransforms[i].localScale;
        }
    }

    private void TryAssignDefaultEyeTarget()
    {
        if (playerTarget != null) return;
        if (!autoFindMainCameraTarget) return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerTarget = mainCamera.transform;
        }
    }

    private void UpdateEyeTracking()
    {
        if (playerTarget == null) return;
        if (eyeScaleTransforms == null || eyeScaleTransforms.Length == 0) return;

        float rotationStep = Mathf.Max(0f, eyeTurnSpeed) * Time.deltaTime;

        for (int i = 0; i < eyeTransforms.Length; i++)
        {
            Transform eye = eyeTransforms[i];
            if (eye == null) continue;

            Vector3 directionToTarget = playerTarget.position - eye.position;
            if (directionToTarget.sqrMagnitude < 0.000001f) continue;

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
            eye.rotation = Quaternion.RotateTowards(eye.rotation, targetRotation, rotationStep * 360f);
        }
    }

    private void UpdateEyeWiden()
    {
        if (eyeTransforms == null || eyeTransforms.Length == 0) return;

        bool shouldWiden = eyeWidenSignalActive || widenHoldTimer > 0f;
        float targetMultiplier = shouldWiden ? Mathf.Max(1f, widenedScaleMultiplier) : 1f;
        float speed = shouldWiden ? Mathf.Max(0f, widenInSpeed) : Mathf.Max(0f, widenOutSpeed);

        currentEyeScaleMultiplier = Mathf.MoveTowards(currentEyeScaleMultiplier, targetMultiplier, speed * Time.deltaTime);

        for (int i = 0; i < eyeScaleTransforms.Length; i++)
        {
            Transform eye = eyeScaleTransforms[i];
            if (eye == null) continue;

            Vector3 baseEyeScale = i < baseEyeScales.Length ? baseEyeScales[i] : eye.localScale;
            eye.localScale = baseEyeScale * currentEyeScaleMultiplier;
        }

        if (widenHoldTimer > 0f)
        {
            widenHoldTimer -= Time.deltaTime;
            if (widenHoldTimer <= 0f)
            {
                widenHoldTimer = 0f;
                if (!eyeWidenSignalActive)
                {
                    currentEyeScaleMultiplier = Mathf.Max(1f, currentEyeScaleMultiplier);
                }
            }
        }
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float minInterval = Mathf.Max(0.05f, blinkMinInterval);
            float maxInterval = Mathf.Max(minInterval, blinkMaxInterval);
            float waitTime = UnityEngine.Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            SetEyesVisible(false);
            yield return new WaitForSeconds(Mathf.Max(0.01f, blinkDuration));
            SetEyesVisible(true);
        }
    }

    private void SetEyesVisible(bool visible)
    {
        if (eyeRenderers == null || eyeRenderers.Length == 0) return;

        for (int i = 0; i < eyeRenderers.Length; i++)
        {
            Renderer eyeRenderer = eyeRenderers[i];
            if (eyeRenderer == null) continue;
            eyeRenderer.enabled = visible;
        }
    }

    public void PlaceAtRandomSpawnPoint()
    {
        float halfWidth = Mathf.Max(0f, spawnSquareSize.x) * 0.5f;
        float halfDepth = Mathf.Max(0f, spawnSquareSize.y) * 0.5f;

        float x = UnityEngine.Random.Range(spawnSquareCenter.x - halfWidth, spawnSquareCenter.x + halfWidth);
        float z = UnityEngine.Random.Range(spawnSquareCenter.y - halfDepth, spawnSquareCenter.y + halfDepth);

        Vector3 position = transform.position;
        position.x = x;
        position.y = 0f;
        position.z = z;
        transform.position = position;
    }

    private IEnumerator PopThenPlayRandomLoopAnimation()
    {
        transform.localScale = Vector3.zero;

        float duration = Mathf.Max(0.01f, popDuration);
        float elapsed = 0f;
        Vector3 overshootScale = baseScale * Mathf.Max(1f, popOvershootScale);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (t < 0.7f)
            {
                float firstPhase = t / 0.7f;
                float easedOut = EaseOutCubic(firstPhase);
                transform.localScale = Vector3.LerpUnclamped(Vector3.zero, overshootScale, easedOut);
            }
            else
            {
                float secondPhase = (t - 0.7f) / 0.3f;
                float easedInOut = EaseInOutCubic(secondPhase);
                transform.localScale = Vector3.LerpUnclamped(overshootScale, baseScale, easedInOut);
            }

            yield return null;
        }

        transform.localScale = baseScale;
        PlayRandomLoopClip();
    }

    private void PlayRandomLoopClip()
    {
        AnimationClip[] clips = { loopClipA, loopClipB, loopClipC };
        AnimationClip selectedClip = GetRandomAssignedClip(clips);

        if (selectedClip == null)
        {
            Debug.LogWarning("OompaLoompa: Assign at least one loop clip (A/B/C).", this);
            return;
        }

        if (animatorComponent != null)
        {
            PlayWithAnimator(selectedClip);
            return;
        }

        if (animationComponent == null)
        {
            Debug.LogWarning("OompaLoompa: No Animator or Animation component found to play loop clips.", this);
            return;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null) continue;

            if (!clip.legacy) continue;

            if (animationComponent.GetClip(clip.name) == null)
            {
                animationComponent.AddClip(clip, clip.name);
            }

            AnimationState state = animationComponent[clip.name];
            if (state != null)
            {
                state.wrapMode = WrapMode.Loop;
            }
        }

        if (!selectedClip.legacy)
        {
            Debug.LogWarning("OompaLoompa: Selected clip is not legacy. Add an Animator component, or mark clips as Legacy for Animation component playback.", this);
            return;
        }

        animationComponent.wrapMode = WrapMode.Loop;
        animationComponent.Play(selectedClip.name);
    }

    private AnimationClip GetRandomAssignedClip(AnimationClip[] clips)
    {
        int assignedCount = 0;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                assignedCount++;
            }
        }

        if (assignedCount == 0)
        {
            return null;
        }

        int selection = UnityEngine.Random.Range(0, assignedCount);
        int current = 0;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null) continue;

            if (current == selection)
            {
                return clips[i];
            }

            current++;
        }

        return null;
    }

    private void PlayWithAnimator(AnimationClip clip)
    {
        StopLoopClipGraph();

        loopClipGraph = PlayableGraph.Create("OompaLoopClipGraph");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(loopClipGraph, "OompaAnimation", animatorComponent);
        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(loopClipGraph, clip);
        output.SetSourcePlayable(clipPlayable);

        animatorComponent.Rebind();
        animatorComponent.Update(0f);

        loopClipGraph.Play();
        hasLoopClipGraph = true;
    }

    private void OnDisable()
    {
        GameManager.BuyActionTriggered -= HandleGlobalBuyAction;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        SetEyesVisible(true);
        StopLoopClipGraph();
    }

    private void OnDestroy()
    {
        GameManager.BuyActionTriggered -= HandleGlobalBuyAction;
        StopLoopClipGraph();
    }

    private void HandleGlobalBuyAction()
    {
        Debug.Log("OompaLoompa: Received global buy action signal. Triggering eye widen.", this);
        SignalEyeWiden();
    }

    private void StopLoopClipGraph()
    {
        if (!hasLoopClipGraph) return;

        if (loopClipGraph.IsValid())
        {
            loopClipGraph.Destroy();
        }

        hasLoopClipGraph = false;
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseInOutCubic(float t)
    {
        if (t < 0.5f)
        {
            return 4f * t * t * t;
        }

        float adjusted = -2f * t + 2f;
        return 1f - (adjusted * adjusted * adjusted) / 2f;
    }

    private void OnDrawGizmosSelected()
    {
        float width = Mathf.Max(0f, spawnSquareSize.x);
        float depth = Mathf.Max(0f, spawnSquareSize.y);

        Vector3 center = new Vector3(spawnSquareCenter.x, 0f, spawnSquareCenter.y);
        Vector3 size = new Vector3(width, 0.01f, depth);

        Gizmos.color = new Color(1f, 0.55f, 0f, 0.35f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(1f, 0.55f, 0f, 1f);
        Gizmos.DrawWireCube(center, size);
    }
}
