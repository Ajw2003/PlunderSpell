using System.Collections;
using UnityEngine;

public class SpellBook : MonoBehaviour
{
    private float _fireRate;
    private float _minSpread;
    private float _maxSpread;
    private float _reloadTime;
    private float _projectileForce;
    private float _projectileSize;

    private int _damage;
    private int _numberOfProjectiles;

    private bool _isHoming;
    private bool _isReloading;

    private Collider[] _playerColliders;

    private GameObject _projectilePrefab;

    public SpellStats spellStats;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform shootPoint;

    private void Start()
    {
        AssignStats();
        _playerColliders = gameObject.GetComponentsInChildren<Collider>();

        // Self-wire what the raid's player rig does not author, so dropping this component on a
        // player is enough on its own. See docs/systems/spells.md, "Two ways to cast".
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>() ?? Camera.main;
        if (shootPoint == null)
            shootPoint = playerCamera != null ? playerCamera.transform : transform;
    }

    /// <summary>
    /// Copies the stats asset onto this book. No asset is a valid state -- the component simply
    /// cannot fire -- and is reported once rather than thrown, so an unconfigured spellbook does not
    /// take the whole player down with it in Start().
    /// </summary>
    public void AssignStats()
    {
        if (spellStats == null)
        {
            Debug.LogWarning($"[SpellBook] {name} has no SpellStats assigned, so it cannot fire. " +
                             "Voice casting (hold V) is unaffected.", this);
            return;
        }

        _fireRate = spellStats.fireRate;
        _minSpread = spellStats.minSpread;
        _maxSpread = spellStats.maxSpread;
        _reloadTime = spellStats.reloadTime;
        _projectileForce = spellStats.projectileForce;
        _damage = spellStats.damage;
        _numberOfProjectiles = spellStats.numberOfProjectiles;
        _projectilePrefab = spellStats.projectilePrefab;
        _isHoming = spellStats.isHoming;
        _projectileSize = spellStats.projectileSize;

        if (_projectilePrefab == null)
        {
            Debug.LogWarning($"[SpellBook] {name}'s SpellStats has no projectile prefab.", this);
            return;
        }

        // These write to the PREFAB ASSET, not to an instance: the edits persist into the project
        // and outlive play mode. Left as-is because the stats asset is the authored source of these
        // numbers, but it is the reason a spell's size can appear to change between sessions.
        if (_projectilePrefab.TryGetComponent(out NetworkedProjectile projectile))
        {
            projectile.lifeTime = spellStats.lifeTime;
            projectile.Damage = spellStats.damage;
        }

        _projectilePrefab.transform.localScale = Vector3.one * spellStats.projectileSize;
    }

    public void CastSpell()
    {
        if (_isReloading) return;
        if (spellStats == null || _projectilePrefab == null || playerCamera == null || shootPoint == null)
            return;
        _isReloading = true;

        StartCoroutine(ReloadRoutine());

        // Ray cast from screen center for aiming so the crosshair is always where the shot will go.
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100f);
        }

        Vector3 shootDirection = (targetPoint - shootPoint.position).normalized;
        int projectileCount = Mathf.Max(_numberOfProjectiles, 1);

        for (int i = 0; i < projectileCount; i++)
        {
            FireProjectile(shootDirection);
        }
    }

    private void FireProjectile(Vector3 baseDirection)
    {
        float currentSpread = Random.Range(_minSpread, _maxSpread);
        Vector3 spreadOffset = Random.insideUnitSphere * currentSpread;
        Vector3 finalDirection = (baseDirection + spreadOffset).normalized;

        var projectileInstance = Instantiate(_projectilePrefab, shootPoint.position, Quaternion.LookRotation(finalDirection));
        var projectileCollider = projectileInstance.GetComponent<Collider>();
        foreach (var playerCollider in _playerColliders)
        {
            Physics.IgnoreCollision(projectileCollider, playerCollider);
        }

        var projectileRb = projectileInstance.GetComponent<Rigidbody>();
        projectileRb.AddForce(_projectileForce * finalDirection, ForceMode.Impulse);
    }

    private IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(_reloadTime);
        _isReloading = false;
    }
}
