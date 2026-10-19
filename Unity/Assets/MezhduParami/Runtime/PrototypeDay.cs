using UnityEngine;

namespace MezhduParami
{
    // One day is deliberately bounded. Night, rent, saves and the yearly story
    // belong to later milestones, not to this control/interaction prototype.
    public sealed class PrototypeDay
    {
        public const int PeriodsPerDay = 3;
        public int Period { get; private set; }
        public int Money { get; private set; } = 120;
        public int Energy { get; private set; } = 80;
        public int IntelligenceExperience { get; private set; }
        public bool HasWorked { get; private set; }
        public bool IsComplete => Period >= PeriodsPerDay;
        public int IntelligenceLevel => IntelligenceExperience >= 15 ? 3
            : IntelligenceExperience >= 8 ? 2 : IntelligenceExperience >= 3 ? 1 : 0;
        public string PeriodLabel => IsComplete ? "День завершён"
            : new[] { "Утро", "День", "Вечер" }[Period];

        public bool CanPerform(ActivityDefinition activity, out string reason)
        {
            if (activity == null || string.IsNullOrWhiteSpace(activity.Id))
                reason = "Занятие не настроено.";
            else if (IsComplete)
                reason = "Все три периода использованы.";
            else if (activity.IsWork && HasWorked)
                reason = "Сегодня уже была оплачиваемая смена.";
            else if (Money + activity.MoneyDelta < 0)
                reason = "Недостаточно денег.";
            else if (Energy + activity.EnergyDelta < 0)
                reason = "Недостаточно энергии. Можно отдохнуть.";
            else
            {
                reason = string.Empty;
                return true;
            }
            return false;
        }

        public bool TryPerform(ActivityDefinition activity, out string reason)
        {
            if (!CanPerform(activity, out reason)) return false;
            Money += activity.MoneyDelta;
            Energy = Mathf.Clamp(Energy + activity.EnergyDelta, 0, 100);
            IntelligenceExperience += Mathf.Max(0, activity.IntelligenceExperience);
            if (activity.IsWork) HasWorked = true;
            Period++;
            return true;
        }
    }
}
