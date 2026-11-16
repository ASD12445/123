using System;

namespace Game.Core
{
    public static class EventBus
    {
        public static event Action<int> DayChanged;
        public static event Action<bool> NightToggled;
        public static event Action PlayerDied;
        public static event Action<int> SkillXpGained;

        public static void PublishDayChanged(int day) => DayChanged?.Invoke(day);
        public static void PublishNightToggled(bool isNight) => NightToggled?.Invoke(isNight);
        public static void PublishPlayerDied() => PlayerDied?.Invoke();
        public static void PublishSkillXpGained(int amount) => SkillXpGained?.Invoke(amount);
    }
}
