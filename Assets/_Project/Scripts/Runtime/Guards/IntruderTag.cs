using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Marks a player as something guards look for, for as long as it is enabled, by registering it
    /// with the <see cref="EnemyDirector"/>.
    ///
    /// Registration has to happen at runtime: anything a scene-building tool registers at edit time
    /// is gone by the time the scene is played, and a guard would then never see anybody. The
    /// director may not exist yet when this enables, so it retries in Start.
    /// </summary>
    [DisallowMultipleComponent]
    public class IntruderTag : MonoBehaviour
    {
        private EnemyDirector _director;

        private void OnEnable() => Register();

        private void Start()
        {
            if (_director == null)
                Register();
        }

        private void Register()
        {
            EnemyDirector director = EnemyDirector.Current != null ? EnemyDirector.Current : FindObjectOfType<EnemyDirector>();
            if (director == null)
                return;
            _director = director;
            _director.RegisterIntruder(transform);
        }

        private void OnDisable()
        {
            if (_director != null)
                _director.UnregisterIntruder(transform);
            _director = null;
        }
    }
}
