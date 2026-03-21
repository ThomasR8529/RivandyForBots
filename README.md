# Rivandy

## 📁 Scripts Overview

### 📜 CombatMonster
- File: `CombatMonster.cs`
- Methods:
  - Awake
  - CheckSpellExecution
  - TryCastSpell
  - CastWithHumanDelay
  - TryAutoAttack
  - ApplyCibleClientRpc
  - ApplyNullCibleClientRpc
  - AskNearestTargetServerRpc

### 📜 DashBots
- File: `DashBots.cs`
- Methods:
  - DashAwayFromTarget
  - Update
  - Update
  - ShouldDash
  - Dash
  - CanDash
  - DashAction
  - DashAction
  - PlayAnimationClientRpc

### 📜 DodgeMonster
- File: `DodgeMonster.cs`
- Methods:
  - Awake
  - TriggerSmartRetreat
  - SmoothRetreatRoutine
  - CanLateralDodge
  - LateralDodgeRoutine
  - ChooseLateralDirection
  - CanDash
  - DashDodgeRoutine
  - TriggerDashAnimClientRpc
  - PredictiveScanRoutine
  - GetAwayDirection
  - OnDrawGizmosSelected

### 📜 Follow
- File: `Follow.cs`
- Methods:
  - GetAgent
  - Awake
  - ResyncMonsterPositionRoutine
  - Update
  - AttemptRepositionToNavMesh
  - RetreatFromTarget
  - StartChargeImpactWatch
  - ChargeImpactRoutine
  - FaceTarget
  - FaceBack
  - OnTriggerEnter
  - OnTriggerStay
  - OnTriggerExit
  - PrepareFaceBack
  - FindNearestTarget
  - SpeedStateChanging
  - IsMovementBlocked

### 📜 PhysicsMonster
- File: `PhysicsMonster.cs`
- Methods:
  - Awake
  - TriggerPush
  - TriggerPushClientRpc
  - EnqueuePush
  - PushController
  - GetCapsuleWorld
  - IsGrounded
  - MoveTowardsFromPoint
  - MoveTowardsCaster
  - StopCharge
  - SyncMonsterPositionClientRpc

### 📜 PlayerClasses
- File: `PlayerClasses.cs`
- Methods:
  - SelectClassInGameSelection
  - ChangeClass
  - RequestEquippedItemsServerRpc
  - SendEquippedItemsClientRpc
  - SetMonsterNameClientRpc
  - UpdatePlayerSpellServerRpc
  - UpdatePlayerSpellClientRpc
  - MeshActivation
  - SetCharacterMeshVisible
  - CastAutoAttack

### 📜 PlayerDash
- File: `PlayerDash.cs`
- Methods:
  - GetSmearHelperCurrent
  - OnDisable
  - DashActionCanceled
  - JumpAction
  - DashAction
  - Update
  - Dash
  - PlayAnimationServerRpc
  - PlayAnimationClientRpc

### 📜 PlayerMovement
- File: `PlayerMovement.cs`
- Methods:
  - Awake
  - PlayFootSounds
  - MoveStateChanging
  - StayBehindPlayer
  - StayBehindMonster
  - JumpStateChanging
  - IsJumpBlocked
  - IsDashBlocked
  - ShieldChanging
  - DisableFront
  - DisableBack
  - DisableLeft
  - DisableRight
  - ChangeAnimationServerRpc
  - ApplyAnimationClientRpc
  - ApplyAnimationMonsterClientRpc

### 📜 PlayerReference
- File: `PlayerReference.cs`
- Methods:
  - Awake
  - ServerTeleport
  - RecreateSmearEffect

### 📜 PlayerReincarnation
- File: `PlayerReincarnation.cs`
- Methods:
  - OnDisable
  - ReincarnationAction
  - Transformation
  - TransformationClientRpc
  - DeTransformation
  - DeTransformationClientRpc
  - AskForReincarnationServerRpc

### 📜 PlayerShooting
- File: `PlayerShooting.cs`
- Methods:
  - OnStateChanged
  - Awake
  - OnDisable
  - SetFastCast
  - StartIt
  - Spell4Action
  - Spell3Action
  - Spell2Action
  - Spell1Action
  - RotateToMouseDirection
  - ShouldUseCast
  - ApplyCooldownValue
  - PerformSpellLaunch
  - LogSpellBlocked
  - LogCastCancel
  - ExecuteSpell
  - CastSpellRoutine
  - ShouldCancelCast
  - GetOrCreateCastBar
  - ClearCastState
  - RefundCooldown
  - IsJumpPressed
  - StopCastAnimation
  - OnLocalHealthChangedDuringCast
  - MarkPushedDuringCast
  - NotifyAutoAttackDuringCast
  - HandleTransformationEvent
  - HandleTransformationCleanup
  - TryCancelActiveSpellsOnTransform
  - TemporarilyFlagCancel
  - CleanupCasterControlTimelinesOnTransform
  - StopActivePushesOnTransform
  - LaunchAutoAttack
  - LaunchSpellServerRpc
  - LaunchSpellClientRpc
  - LaunchSpellGlobal
  - BlockMoveSeconds
  - BlockCastingSeconds
  - CancelSpellPriority
  - DelayedTriggerMonsterPush
  - CancelSpell
  - CancelSpellServerRpc
  - CancelSpellClientRpc
  - PushTarget
  - MoveTowardsTargetClientRpc
  - MoveTowards
  - PushTargetOverTime
  - PushOverTimeCoroutine

### 📜 PlayerStatistics
- File: `PlayerStatistics.cs`
- Methods:
  - Awake
  - UpdateStatDataClientRpc
  - SetAttacker
  - RefreshName
  - Update
  - SetPlayerDataLocal
  - SetStatDataLocal
  - IsSameTeam
  - ApplyDamage
  - ApplyDamage
  - ApplyHeal
  - ApplyHeal
  - ApplyDamageServer
  - OnHealthChangedClientRpc
  - OnAutoHealthClientRpc
  - DealDamageWith
  - UpdateUiAndPlayerDataClientRpc
  - GetKills
  - GetPoints
  - GetAccountId
  - UpdateSoulHealthClientRpc
  - ApplyStunClientRpc
  - RecordKill

### 📜 TeleguidanceSegment
- File: `Spell.cs`
- Methods:
  - SpawnTargetPrefabIfAllowed
  - Start
  - ApplySelfHeal
  - SetCaster
  - ApplyCasterControlTimeline
  - CancelCasterTimelineImmediate
  - StopCasterTimelineAndClearEffects
  - LateUpdate
  - Update
  - GetCasterName
  - GetCaster
  - FixedUpdate
  - OnDestroy
  - SpawnObjectsPeriodically
  - SpawnObject

### 📜 StartServerLocal
- File: `StartServerLocal.cs`
- Methods:
  - startServerInSeconds
