using System;
using UnityEngine;

namespace Game.Core
{
    public class WorldTime : MonoBehaviour
    {
        [Header("Cycle")]
        [SerializeField] private float dayLengthSeconds = 600f;
        [SerializeField, Range(0f, 1f)] private float timeOfDay = 0.3f;
        [SerializeField] private int dayCount = 1;

        public float DayLengthSeconds => dayLengthSeconds;
        public float TimeOfDay => timeOfDay;
        public int DayCount => dayCount;
        public float Hours => timeOfDay * 24f;
        public bool IsNight => timeOfDay < 0.25f || timeOfDay >= 0.75f;
        public bool IsPaused { get; set; }

        public static event Action<int> DayStarted;
        public static event Action<int> NightStarted;

        private bool _wasNight;

        public void SetDayLength(float seconds) => dayLengthSeconds = Mathf.Max(1f, seconds);

        public void SetTimeOfDay(float value)
        {
            timeOfDay = Mathf.Repeat(value, 1f);
            _wasNight = IsNight;
        }

        public void SetDay(int day)
        {
            dayCount = Mathf.Max(1, day);
        }

        public string GetTimeString()
        {
            int totalMinutes = Mathf.FloorToInt(timeOfDay * 24f * 60f);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours:00}:{minutes:00}";
        }

        private void Update()
        {
            if (IsPaused || dayLengthSeconds <= 0f)
                return;

            timeOfDay += Time.deltaTime / dayLengthSeconds;

            if (timeOfDay >= 1f)
            {
                timeOfDay -= 1f;
                dayCount++;
                EventBus.PublishDayChanged(dayCount);
                DayStarted?.Invoke(dayCount);
            }

            bool night = IsNight;
            if (night != _wasNight)
            {
                _wasNight = night;
                EventBus.PublishNightToggled(night);
                if (night)
                    NightStarted?.Invoke(dayCount);
            }

        }
    }
}
