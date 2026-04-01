using UnityEngine;
using System.Collections;
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

    private Vector3 baseScale;
    private PlayableGraph loopClipGraph;
    private bool hasLoopClipGraph;

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
        PlaceAtRandomSpawnPoint();
        StartCoroutine(PopThenPlayRandomLoopAnimation());
    }

    private void PlaceAtRandomSpawnPoint()
    {
        float halfWidth = Mathf.Max(0f, spawnSquareSize.x) * 0.5f;
        float halfDepth = Mathf.Max(0f, spawnSquareSize.y) * 0.5f;

        float x = Random.Range(spawnSquareCenter.x - halfWidth, spawnSquareCenter.x + halfWidth);
        float z = Random.Range(spawnSquareCenter.y - halfDepth, spawnSquareCenter.y + halfDepth);

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

        int selection = Random.Range(0, assignedCount);
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
        StopLoopClipGraph();
    }

    private void OnDestroy()
    {
        StopLoopClipGraph();
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
