# Eurydice gameplay foundation

Core loop: explore -> collect memory fragments -> light braziers -> unlock portal.

## Player setup

1. Use a player root with `CharacterController`, `PlayerInput`, `EurydiceInputReader`, `EurydicePlayerController`, and `PlayerInteractor`.
2. Assign `Assets/InputSystem_Actions.inputactions` to `PlayerInput`; it already contains Move, Look, Jump, and Interact.
3. Remove or disable `StarterAssets.ThirdPersonController` and `StarterAssetsInputs` on the same root to prevent double movement.
4. Assign the Cinemachine follow target to `cameraTarget` and the character animator to `animator`.
5. Animator state names default to the existing controller states: `Idle Walk Run Blend`, `JumpStart`, `InAir`, and `Fly`. The interaction state is empty by default; assign it after adding an interaction clip.

## Chapter setup

1. Add one `ChapterProgress` to the chapter root and set required fragment/brazier counts.
2. Add `MemoryFragment` to trigger colliders and assign the chapter progress.
3. Add `Brazier` to brazier roots, assign lit/unlit visuals and the same chapter progress.
4. Add `PortalUnlocker`, assign the chapter progress and portal object.

All animation changes are issued only when the logical state changes and use `Animator.CrossFadeInFixedTime`.
