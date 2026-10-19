using UnityEngine;

namespace MezhduParami
{
    // The slice uses three representative days. Its length is explicit so it
    // cannot silently become the complete twelve-day first-year calendar.
    public sealed class PrototypeDay
    {
        public const int PeriodsPerDay = 3;
        public const int NormalFoodPrice = 8;
        public int TotalDays { get; }
        public int DayNumber { get; private set; } = 1;
        public int Period { get; private set; }
        public int Money { get; private set; } = 120;
        public int Energy { get; private set; } = 80;
        public int Health { get; private set; } = 80;
        public int IntelligenceExperience { get; private set; }
        public int Readiness { get; private set; }
        public bool HasWorked { get; private set; }
        public bool IsComplete => Period >= PeriodsPerDay;
        public bool IsFinished => IsComplete && DayNumber == TotalDays;
        public int IntelligenceLevel => IntelligenceExperience >= 15 ? 3
            : IntelligenceExperience >= 8 ? 2 : IntelligenceExperience >= 3 ? 1 : 0;
        public string PeriodLabel => IsComplete ? "День завершён"
            : new[] { "Утро", "День", "Вечер" }[Period];

        public PrototypeDay(int totalDays = 3)
        {
            if (totalDays < 1) throw new System.ArgumentOutOfRangeException(nameof(totalDays));
            TotalDays = totalDays;
        }

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
            Readiness = Mathf.Clamp(Readiness + activity.ReadinessDelta, 0, 6);
            if (activity.IsWork) HasWorked = true;
            Period++;
            return true;
        }

        public bool TryStartNextDay(bool buyFood, out string reason)
        {
            if (!IsComplete) reason = "Сначала используй три периода дня.";
            else if (IsFinished) reason = "Тестовый сценарий завершён.";
            else if (buyFood && Money < NormalFoodPrice) reason = "На обычную еду нужно 8. Можно пропустить питание с потерей здоровья.";
            else
            {
                if (buyFood) Money -= NormalFoodPrice;
                else Health = Mathf.Max(0, Health - 8);
                int recovery = buyFood ? (Health < 40 ? 35 : 50) : 30;
                Energy = Mathf.Min(100, Energy + recovery);
                DayNumber++;
                Period = 0;
                HasWorked = false;
                reason = string.Empty;
                return true;
            }
            return false;
        }
    }
}
