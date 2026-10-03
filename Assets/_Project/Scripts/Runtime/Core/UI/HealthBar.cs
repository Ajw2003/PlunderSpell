using Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>Deprecated: kept as reference, not live.</summary>
    [System.Obsolete("Deprecated 2026-10-01: unused by any prefab, scene or code. See docs/reference/deprecated-code.md")]
    public class HealthBar : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Slider _slider;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Gradient _healthGradient;

        [Header("Tracking Settings")]
        [SerializeField] private GameObject _targetObject;
        [SerializeField] private bool _isWorldSpace = true;
        [SerializeField] private Vector3 _offset = new Vector3(0, 2.5f, 0);

        private IHealth _healthProvider;
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;

            if (_targetObject != null)
            {
                _healthProvider = _targetObject.GetComponent<IHealth>();
            }

            if (_healthProvider == null)
            {
                _healthProvider = GetComponentInParent<IHealth>();
            }

            if (_slider == null)
            {
                _slider = GetComponent<Slider>();
            }
        }

        private void LateUpdate()
        {
            if (_healthProvider == null) return;

            float healthPercent = _healthProvider.CurrentHealth / _healthProvider.MaxHealth;

            if (_slider != null)
            {
                _slider.value = healthPercent;
            }

            if (_fillImage != null && _healthGradient != null)
            {
                _fillImage.color = _healthGradient.Evaluate(healthPercent);
            }

            if (_isWorldSpace && _targetObject != null && _mainCamera != null)
            {
                transform.position = _targetObject.transform.position + _offset;
                transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
            }
        }

        public void SetTarget(GameObject target)
        {
            _targetObject = target;
            _healthProvider = target.GetComponent<IHealth>();
        }
    }
}
