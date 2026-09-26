using Code.Scripts.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class ItemManager : SingletonBase<ItemManager>
{
    [SerializeField] private LayerMask _itemLayerMask = -1; // Default to all layers, should be set to "Item" layer
    [SerializeField] private float _dragDistance = 5f;
    [SerializeField] private float _scrollSpeed = 25f;
    [SerializeField] private float _minDragDepth = 0.5f;
    [SerializeField] private float _maxDragDepth = 10f;
    [SerializeField] private float _raycastDistance = 100f;

    [SerializeField] private float _rotationSensitivity = 0.5f;
    [SerializeField] private float _throwForce = 15f;

    private Item _hoveredItem;
    private Vector3 _hoveredPoint;
    private Item _draggedItem;

    // The local player's grab beam (#144). Other players' beams are drawn by the carry relay.
    private GrabBeam _beam;
    private Camera _mainCamera;
    private float _currentDragDepth;

    protected override bool PersistBetweenScenes => false;

    protected override void Awake()
    {
        base.Awake();
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            _mainCamera = FindFirstObjectByType<Camera>();
        }
    }

    public bool IsRotatingObject => _draggedItem != null && !_draggedItem.IsInHand
        && Mouse.current != null && Mouse.current.middleButton.isPressed;

    /// <summary>Weapons are held rigidly in the hand, not hung on the beam: a crossbow on the beam
    /// hung on the crosshair line and its own bolt hit it.</summary>
    private static bool IsHeldInHand(Item item) =>
        item.TryGetComponent(out RangedWeapon _) || item.TryGetComponent(out MeleeWeapon _);

    private void OnEnable() => RenderPipelineManager.beginCameraRendering += PoseHeldWeapon;

    private void OnDisable() => RenderPipelineManager.beginCameraRendering -= PoseHeldWeapon;

    /// <summary>Stands an in-hand weapon in the hand just before the view renders, after the
    /// camera has moved this frame, so it never lags or jitters against the view.</summary>
    private void PoseHeldWeapon(ScriptableRenderContext context, Camera camera)
    {
        if (camera != _mainCamera || _draggedItem == null || !_draggedItem.IsInHand)
            return;
        Transform view = camera.transform;
        _draggedItem.SetHandPose(WeaponHand(view), view.rotation);
    }

    /// <summary>Where a held weapon's grip sits: low and to the right of the view.</summary>
    private static Vector3 WeaponHand(Transform view) =>
        view.position + view.right * 0.2f - view.up * 0.22f + view.forward * 0.3f;

    private void Update()
    {
        // In a session the player, and with it the camera, is spawned after this wakes.
        if (_mainCamera == null || !_mainCamera.isActiveAndEnabled)
            _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        if (_pendingDrag != null && _draggedItem == null && Item.CanDriveHere(_pendingDrag))
            StartDragging(_pendingDrag, _pendingDrag.LocalToWorldPoint(_pendingGrabLocal));

        HandleHover();

        // Shattered in your hands (fragile loot turns its collisions off when it breaks): let go.
        if (_draggedItem != null && _draggedItem.TryGetComponent(out Rigidbody heldBody) && !heldBody.detectCollisions)
            StopDragging();

        if (_draggedItem != null)
        {
            // A held ranged weapon aims on right-click-and-hold instead of throwing on right-click:
            // nobody throws away the crossbow they are trying to fire.
            if (_draggedItem.TryGetComponent(out RangedWeapon rangedWeapon))
            {
                rangedWeapon.SetAiming(Mouse.current != null && Mouse.current.rightButton.isPressed);
            }
            else if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                ThrowDraggedItem();
                return;
            }

            _draggedItem.SetViewYaw(_mainCamera.transform.eulerAngles.y);
            if (_draggedItem.IsInHand)
                return;

            // The target keeps following the crosshair while rotating too: a target left standing
            // still would let the item drift off the beam.
            bool rotating = Mouse.current != null && Mouse.current.middleButton.isPressed;
            _draggedItem.SetRotating(rotating);
            if (rotating)
                HandleRotation();
            else
                HandleScrollDepth();
            UpdateDraggedItemPosition();
        }
    }

    private void ThrowDraggedItem()
    {
        if (_draggedItem != null)
        {
            Vector3 throwDirection = _mainCamera.transform.forward;
            _draggedItem.Throw(throwDirection, _throwForce);
            _draggedItem = null;
        }
    }

    private void HandleRotation()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        if (delta.sqrMagnitude > 0.01f)
        {
            Vector3 camRight = _mainCamera.transform.right;
            Vector3 camUp = _mainCamera.transform.up;

            Quaternion rotX = Quaternion.AngleAxis(-delta.y * _rotationSensitivity, camRight);
            Quaternion rotY = Quaternion.AngleAxis(-delta.x * _rotationSensitivity, camUp);

            _draggedItem.UpdateRotation(rotX * rotY * _draggedItem.TargetRotation);
        }
    }

    private void HandleScrollDepth()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float normalizedScroll = scroll / 120f;
            _currentDragDepth += normalizedScroll * _scrollSpeed;
            _currentDragDepth = Mathf.Clamp(_currentDragDepth, _minDragDepth, _maxDragDepth);
        }
    }

    private void HandleHover()
    {
        if (_draggedItem != null)
        {
            _hoveredItem = null;
            return;
        }

        if (Mouse.current == null) return;
        Ray ray = CrosshairRay();

        // Reach, not line of sight: the hover ray used to be 100 m long, so anything visible
        // could be yanked across the room.
        float reach = Mathf.Min(_raycastDistance, _maxDragDepth);
        if (Physics.Raycast(ray, out RaycastHit hit, reach, _itemLayerMask))
        {
            if (hit.collider.TryGetComponent(out Item item))
            {
                _hoveredItem = item;
                _hoveredPoint = hit.point;
            }
            else
            {
                _hoveredItem = null;
            }
        }
        else
        {
            _hoveredItem = null;
        }
    }

    private void UpdateDraggedItemPosition()
    {
        // Too heavy to lift (a two-person piece held by one): towed behind the holder on a rope
        // instead of pulled toward the crosshair, so they can walk forward, looking where they
        // go, while it scrapes along behind.
        if (_draggedItem.IsTooHeavyToLift)
        {
            Vector3 feet = _mainCamera.transform.root.position;
            Vector3 held = _draggedItem.GripWorldPosition;
            Vector3 tow = TowTarget(feet, held, _towRope);
            // A slack rope pulls nothing, and brakes the piece: without that it coasted on past the
            // holder.
            if (tow == held)
                _draggedItem.UpdateTarget(held, Vector3.zero);
            else
                _draggedItem.UpdateTargetPosition(tow);
            return;
        }

        if (Mouse.current == null) return;
        Ray ray = CrosshairRay();

        Vector3 targetPoint = ray.GetPoint(_currentDragDepth);
        _draggedItem.UpdateTargetPosition(targetPoint);
    }

    // Length of the tow rope for a piece too heavy to lift, set at pickup from how far away it was.
    private float _towRope = 2f;

    /// <summary>
    /// Where a towed piece is pulled to: nowhere while it is within <paramref name="rope"/> of the
    /// holder (measured across the floor), else to the rope's length from them, at its own height.
    /// </summary>
    public static Vector3 TowTarget(Vector3 holderFeet, Vector3 heldPoint, float rope)
    {
        Vector3 away = heldPoint - holderFeet;
        away.y = 0f;
        float distance = away.magnitude;
        if (distance <= rope || distance < 1e-4f)
            return heldPoint;
        Vector3 target = holderFeet + away / distance * rope;
        target.y = heldPoint.y;
        return target;
    }

    /// <summary>
    /// Straight out through the crosshair. The mouse pointer is locked there in play anyway; aiming
    /// through the camera centre means grabbing and holding never depend on the cursor state.
    /// </summary>
    private Ray CrosshairRay() => _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

    public void OnInventoryClicked(InputAction.CallbackContext context)
    {
        if (context.started && _hoveredItem != null)
        {
            StartDragging(_hoveredItem, _hoveredPoint);
        }
        else if (context.canceled)
        {
            StopDragging();
        }
    }

    public void ForceRelease()
    {
        StopDragging();
    }

    /// <summary>Picks up <paramref name="item"/> by <paramref name="grabPoint"/>, the point on it
    /// the crosshair was on: it hangs from there, as in R.E.P.O. (#144).</summary>
    private void StartDragging(Item item, Vector3 grabPoint)
    {
        // In a session another machine may be driving this body; ask for it and pick it up once
        // granted (Update), rather than fighting the replicated position meanwhile. The grab point
        // is kept in the item's own frame, since it may move before the answer comes.
        if (!Item.CanDriveHere(item))
        {
            _pendingDrag = item;
            _pendingGrabLocal = Quaternion.Inverse(item.transform.rotation) * (grabPoint - item.transform.position);
            Item.RequestDrive?.Invoke(item);
            return;
        }

        _pendingDrag = null;
        _draggedItem = item;
        _draggedItem.SetViewYaw(_mainCamera.transform.eulerAngles.y);
        _draggedItem.StartDragging(_mainCamera.transform.root.gameObject, grabPoint);
        if (IsHeldInHand(item))
        {
            _draggedItem.HoldInHand(true);
            _draggedItem.SetHandPose(WeaponHand(_mainCamera.transform), _mainCamera.transform.rotation);
        }

        _currentDragDepth = Vector3.Distance(_mainCamera.transform.position, grabPoint);
        _currentDragDepth = Mathf.Clamp(_currentDragDepth, _minDragDepth, _maxDragDepth);
        Vector3 across = grabPoint - _mainCamera.transform.root.position;
        across.y = 0f;
        _towRope = Mathf.Clamp(across.magnitude, 1.5f, 3f);
    }

    private void StopDragging()
    {
        _pendingDrag = null;
        if (_draggedItem != null)
        {
            _draggedItem.StopDragging();
            _draggedItem = null;
        }
    }

    private Item _pendingDrag;
    private Vector3 _pendingGrabLocal;

    /// <summary>Where the local player's beam leaves from: low and to the right of the view, where
    /// a hand would be. The carry relay sends the same point to other players.</summary>
    public Vector3 BeamHand { get; private set; }

    /// <summary>Draws the local beam after the camera has moved for this frame.</summary>
    private void LateUpdate()
    {
        if (_draggedItem == null || _mainCamera == null || _draggedItem.IsInHand)
        {
            if (_beam != null)
                _beam.Hide();
            return;
        }

        if (_beam == null)
            _beam = GrabBeam.Create("LocalGrabBeam");

        Transform view = _mainCamera.transform;
        BeamHand = view.position + view.right * 0.22f - view.up * 0.2f + view.forward * 0.35f;
        _beam.Show(BeamHand, _draggedItem.TargetPosition, _draggedItem.GripWorldPosition, _draggedItem.Load);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_beam != null)
            Destroy(_beam.gameObject);
    }

    public Item HoveredItem => _hoveredItem;

    /// <summary>The item currently held, or null. The HUD reads this to name what is being carried.</summary>
    public Item CarriedItem => _draggedItem;

    /// <summary>Swings the currently held item if it is a <see cref="MeleeWeapon"/>. Returns whether a swing happened.</summary>
    public bool TryMeleeSwing(Vector3 origin, Vector3 forward)
    {
        return _draggedItem != null
            && _draggedItem.TryGetComponent(out MeleeWeapon weapon)
            && weapon.TrySwing(origin, forward);
    }

    /// <summary>Fires the currently held item if it is a <see cref="RangedWeapon"/>. Returns whether a shot was fired.</summary>
    public bool TryFireRanged(Vector3 origin, Vector3 direction)
    {
        return _draggedItem != null
            && _draggedItem.TryGetComponent(out RangedWeapon weapon)
            && weapon.TryFire(origin, direction);
    }

    /// <summary>The currently held item's ranged-weapon component, or null. The HUD reads this to
    /// show ammo/reload status.</summary>
    public RangedWeapon CarriedRangedWeapon =>
        _draggedItem != null && _draggedItem.TryGetComponent(out RangedWeapon weapon) ? weapon : null;
}
