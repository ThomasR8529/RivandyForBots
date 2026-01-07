using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Lightbug.CharacterControllerPro.Core;
using Unity.Netcode;

/// Plays Timeline clips when the player jumps and when they land.
/// Attach to the player GameObject. Assign the two Timeline assets in the inspector.
public class PlayerTimelineDirector : NetworkBehaviour
{
    [Header("References")]
    private PlayableDirector director;
    [SerializeField] private TimelineAsset jumpTimeline;
    [SerializeField] private TimelineAsset groundTimeline;
    [SerializeField] private TimelineAsset dashTimeline;
    [SerializeField] private TimelineAsset dashEndTimeline;

    [Header("Options")]
    [Tooltip("Only drive timelines for the local player instance.")]
    [SerializeField] private bool onlyLocalPlayer = true;
    [Tooltip("Minimum upward velocity to consider a takeoff as a jump.")]
    [SerializeField] private float minJumpVelocityY = 0.5f;

    private CharacterActor characterActor;
    private PlayerReference playerRef;
    private bool wasGrounded;

    private void Awake()
    {
        playerRef = GetComponent<PlayerReference>();
        if (playerRef != null)
            characterActor = playerRef.characterActor;

        if (director == null)
            director = cooldownUI.instance.interactiveDirector;
    }

    private void Start()
    {
        if (characterActor != null)
            wasGrounded = characterActor.IsGrounded;
        SubscribeDashEvents();
    }

    private void Update()
    {
        if (onlyLocalPlayer && (!IsOwner && !IsLocalPlayer))
            return;
        if (characterActor == null || director == null)
            return;

        bool grounded = characterActor.IsGrounded;
        float vy = characterActor.Velocity.y;

        // Detect jump: transition Grounded -> NotGrounded with upward velocity
        if (wasGrounded && !grounded && vy > minJumpVelocityY)
        {
            Play(jumpTimeline);
        }

        // Detect landing: transition NotGrounded -> Grounded
        if (!wasGrounded && grounded)
        {
            Play(groundTimeline);
        }

        wasGrounded = grounded;
    }

    private void OnDisable()
    {
        UnsubscribeDashEvents();
    }

    private void SubscribeDashEvents()
    {
        if (playerRef == null)
            playerRef = GetComponent<PlayerReference>();
        if (playerRef != null && playerRef.playerDash != null)
        {
            playerRef.playerDash.OnDashStartEvent += HandleDashStart;
            playerRef.playerDash.OnDashEndEvent += HandleDashEnd;
        }
    }

    private void UnsubscribeDashEvents()
    {
        if (playerRef != null && playerRef.playerDash != null)
        {
            playerRef.playerDash.OnDashStartEvent -= HandleDashStart;
            playerRef.playerDash.OnDashEndEvent -= HandleDashEnd;
        }
    }

    private void HandleDashStart()
    {
        if (onlyLocalPlayer && (!IsOwner && !IsLocalPlayer))
            return;
        Play(dashTimeline);
    }

    private void HandleDashEnd()
    {
        if (onlyLocalPlayer && (!IsOwner && !IsLocalPlayer))
            return;
        Play(dashEndTimeline);
    }

    private void Play(TimelineAsset asset)
    {
        if (asset == null) return;
        director.time = 0;
        director.playableAsset = asset;
        director.Evaluate();
        director.Play();
    }
}
