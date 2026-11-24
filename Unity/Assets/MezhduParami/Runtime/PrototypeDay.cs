using UnityEngine;

namespace MezhduParami
{
    // The slice uses three representative days. Its length is explicit so it
    // cannot silently become the complete twelve-day first-year calendar.
    public sealed class PrototypeDay
    {
        public const int PeriodsPerDay = 3;
        private readonly EnergyReserve energy = new EnergyReserve();
        public int TotalDays { get; }
        public int DayNumber { get; private set; } = 1;
        public int Period { get; private set; }
        public int Money { get; private set; } = 120;
        public int Energy => energy.Current;
        public int RecoveryLimit => energy.Limit;
        public int Fatigue => energy.Fatigue;
        public int RestsRemaining => energy.RestsRemaining;
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
            else if (activity.Id == "rest" && RestsRemaining == 0)
                reason = "Оба отдыха на сегодня использованы. Можно лечь спать.";
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
            if (activity.EnergyDelta < 0) energy.Spend(-activity.EnergyDelta);
            else if (activity.Id == "rest") energy.Rest(activity.EnergyDelta);
            else energy.Recover(activity.EnergyDelta);
            IntelligenceExperience += Mathf.Max(0, activity.IntelligenceExperience);
            Readiness = Mathf.Clamp(Readiness + activity.ReadinessDelta, 0, 6);
            if (activity.IsWork) HasWorked = true;
            Period++;
            return true;
        }

        public bool TrySleep(out string reason)
        {
            if (IsFinished)
            {
                reason = "Тестовый сценарий завершён.";
                return false;
            }
            energy.Sleep();
            if (DayNumber == TotalDays) Period = PeriodsPerDay;
            else StartMorning();
            reason = string.Empty;
            return true;
        }

        public bool TrySkipSleep(out string reason)
        {
            if (!IsComplete || IsFinished)
            {
                reason = "Пропустить сон можно в конце дня.";
                return false;
            }
            energy.SkipSleep();
            StartMorning();
            reason = string.Empty;
            return true;
        }

        private void StartMorning()
        {
            DayNumber++;
            Period = 0;
            HasWorked = false;
        }
    }
}
