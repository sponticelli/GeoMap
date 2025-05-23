using UnityEngine;
using UnityEngine.UI;

namespace GeoMap.UI
{
    public class RotateUI : MonoBehaviour
    {
        [SerializeField] private Vector2 speedRange;
        [SerializeField] private float changeDirectionTime;

        [SerializeField] private Image target;

        private float _currentRotationSpeed;
        private float _timer;

        private void Start()
        {
            if (target == null)
            {
                target = GetComponent<Image>();
            }
            SetRandomRotationSpeed();
            _timer = 0f;
        }

        private void Update()
        {
            // Update timer and check if it's time to change speed
            _timer += Time.deltaTime;
            if (_timer >= changeDirectionTime)
            {
                SetRandomRotationSpeed();
                _timer = 0f;
            }

            // Rotate the target image
            if (target != null)
            {
                target.transform.Rotate(0f, 0f, _currentRotationSpeed * Time.deltaTime);
            }
        }

        private void SetRandomRotationSpeed()
        {

            var direction = Random.Range(0, 2);
            var minRotationSpeed = direction == 0 ? speedRange.x : -1f * speedRange.x;
            var maxRotationSpeed = direction == 0 ? speedRange.y : -1f * speedRange.y;
            
            _currentRotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        }
    }
}