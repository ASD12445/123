using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Core
{
    public class SunLightDriver : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private float dayIntensity = 130000f;
        [SerializeField] private float nightIntensity = 200f;
        [SerializeField] private float sunYaw = 30f;

        private HDAdditionalLightData _hdLight;

        public void SetSun(Light lightSource)
        {
            sun = lightSource;
            _hdLight = null;
        }

        private void LateUpdate()
        {
            if (sun == null)
                return;

            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.WorldTime == null)
                return;

            if (_hdLight == null || _hdLight.gameObject != sun.gameObject)
                _hdLight = sun.GetComponent<HDAdditionalLightData>();

            float timeOfDay = gameManager.WorldTime.TimeOfDay;
            float xAngle = (timeOfDay - 0.25f) * 360f;
            sun.transform.rotation = Quaternion.Euler(xAngle, sunYaw, 0f);

            float elevation = Mathf.Sin((timeOfDay - 0.25f) * Mathf.PI * 2f);
            float intensity = Mathf.Lerp(nightIntensity, dayIntensity, Mathf.Clamp01(elevation));

            if (_hdLight != null)
            {
                _hdLight.lightUnit = UnityEngine.Rendering.LightUnit.Lux;
                _hdLight.intensity = intensity;
            }

            sun.intensity = intensity > 0f ? 1f : 0f;
        }
    }
}
