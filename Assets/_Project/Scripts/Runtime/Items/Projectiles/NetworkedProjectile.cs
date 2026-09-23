using System.Collections;
using Interfaces;
using UnityEngine;

// Plain MonoBehaviour for now - becomes a PurrNet NetworkBehaviour once networking is wired up
// (Phase 3), which is out of scope here.
public class NetworkedProjectile : MonoBehaviour
{
    public float lifeTime = 3f;
    public int Damage;

    /// <summary>Who fired this, for damage feedback and so a shooter is never hit by their own shot.</summary>
    public GameObject Instigator;

    private void Start()
    {
        StartCoroutine(DelayedDestroy());
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.TryGetComponent(out IHealth hit))
        {
            Vector3 point = other.contactCount > 0 ? other.GetContact(0).point : transform.position;
            Interfaces.Damage.Apply(hit, Damage, gameObject, Instigator, point, DamageKind.Projectile);
        }

        DestroySelf();
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }
}
