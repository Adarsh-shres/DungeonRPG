using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class FirstPersonAttack : MonoBehaviour
{
    [SerializeField] private Animator armsAnimator;
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(1)] private int damage = 25;
    [SerializeField, Min(0.1f)] private float attackRange = 2f;
    [SerializeField, Range(0f, 1f)] private float impactTime = 0.35f;

    private static readonly int Relax = Animator.StringToHash("ArmsRig|relax");
    private static readonly int KnifeIdle = Animator.StringToHash("ArmsRig|knife_idle");
    private static readonly int KnifeHit = Animator.StringToHash("ArmsRig|knife_hit_01");
    private static readonly int Equip = Animator.StringToHash("Equip");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private bool impactPending;

    private void Update()
    {
        if (armsAnimator == null || !armsAnimator.isActiveAndEnabled || playerCamera == null) return;

        bool transitioning = armsAnimator.IsInTransition(0);
        AnimatorStateInfo state = armsAnimator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo strike = transitioning ? armsAnimator.GetNextAnimatorStateInfo(0) : state;
        if (strike.shortNameHash != KnifeHit) strike = state;
        if (impactPending && strike.shortNameHash == KnifeHit && strike.normalizedTime >= impactTime)
        {
            impactPending = false;
            ApplyHit();
        }

        if (transitioning || Cursor.lockState != CursorLockMode.Locked || !Application.isFocused) return;
        ReadInput(out bool equipPressed, out bool attackPressed);
        if (equipPressed && state.shortNameHash == Relax)
            armsAnimator.SetTrigger(Equip);
        else if (attackPressed && state.shortNameHash == KnifeIdle)
        {
            impactPending = true;
            armsAnimator.SetTrigger(Attack);
        }
    }

    private void OnDisable() => impactPending = false;

    private void ApplyHit()
    {
        Ray aim = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        // Start the gameplay ray at the camera, rather than its near plane.
        aim.origin = playerCamera.transform.position;
        float closestDistance = attackRange;
        EnemyHealth victim = null;
        foreach (RaycastHit hit in Physics.RaycastAll(aim, attackRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            Collider collider = hit.collider;
            if (hit.distance > closestDistance || collider.transform.IsChildOf(transform)) continue;
            EnemyHealth health = collider.GetComponentInParent<EnemyHealth>();
            if (collider.isTrigger && health == null) continue;
            closestDistance = hit.distance;
            victim = health;
        }
        if (victim != null) victim.TakeDamage(damage);
    }

    private static void ReadInput(out bool equipPressed, out bool attackPressed)
    {
#if ENABLE_INPUT_SYSTEM
        equipPressed = Keyboard.current?.rKey.wasPressedThisFrame ?? false;
        attackPressed = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
#elif ENABLE_LEGACY_INPUT_MANAGER
        equipPressed = Input.GetKeyDown(KeyCode.R);
        attackPressed = Input.GetMouseButtonDown(0);
#else
        equipPressed = attackPressed = false;
#endif
    }
}
