using Code.Scripts.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private Item _draggedItem;
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

    public bool IsRotatingObject => _draggedItem != null && Mouse.current != null && Mouse.current.middleButton.isPressed;

    private void Update()
    {
        if (_mainCamera == null) return;

        HandleHover();

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

            if (Mouse.current != null && Mouse.current.middleButton.isPressed)
            {
                HandleRotation();
            }
            else
            {
                HandleScrollDepth();
                UpdateDraggedItemPosition();
            }
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
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _itemLayerMask))
        {
            if (hit.collider.TryGetComponent(out Item item))
            {
                _hoveredItem = item;
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
        if (Mouse.current == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);

        Vector3 targetPoint = ray.GetPoint(_currentDragDepth);
        _draggedItem.UpdateTargetPosition(targetPoint);
    }

    public void OnInventoryClicked(InputAction.CallbackContext context)
    {
        if (context.started && _hoveredItem != null)
        {
            StartDragging(_hoveredItem);
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

    private void StartDragging(Item item)
    {
        _draggedItem = item;
        _draggedItem.StartDragging();

        _currentDragDepth = Vector3.Distance(_mainCamera.transform.position, item.transform.position);
        _currentDragDepth = Mathf.Clamp(_currentDragDepth, _minDragDepth, _maxDragDepth);
    }

    private void StopDragging()
    {
        if (_draggedItem != null)
        {
            _draggedItem.StopDragging();
            _draggedItem = null;
        }
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
