using UnityEngine;
using UnityEngine.UI;

namespace GeoMap.UI
{
    public class PulsateUI : MonoBehaviour
    {
        [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f); // Min and max scale values
        [SerializeField] private float pulsationTime = 1.0f; // Time for one complete pulse cycle
        [SerializeField] private Image target;

        private float _currentTime;
        private Vector3 _originalScale;

        private void Start()
        {
            if (target == null)
            {
                target = GetComponent<Image>();
            }
            
            if (target != null)
            {
                _originalScale = target.transform.localScale;
            }
            
            _currentTime = 0f;
        }

        private void Update()
        {
            if (target == null) return;
            
            // Update time
            _currentTime += Time.deltaTime;
            
            // Calculate scale factor using a sine wave to smoothly oscillate between min and max
            float t = (Mathf.Sin(_currentTime * (2 * Mathf.PI / pulsationTime)) + 1) / 2; // Range 0-1
            float scaleFactor = Mathf.Lerp(scaleRange.x, scaleRange.y, t);
            
            // Apply scale
            target.transform.localScale = _originalScale * scaleFactor;
        }
    }
}