using System;

namespace MezhduParami
{
    internal sealed class EnergyReserve
    {
        public const int Full = 100;
        public const int Limited = 75;
        private const int ActionsBeforeRest = 3;
        private const int ActionsBeforeSleep = 6;

        private int actionsSinceRest;
        private int actionsSinceSleep;

        public int Current { get; private set; } = 80;
        public bool NeedsRest => actionsSinceRest >= ActionsBeforeRest;
        public bool NeedsSleep => actionsSinceSleep >= ActionsBeforeSleep;
        public int Limit => NeedsSleep || NeedsRest ? Limited : Full;
        public string Status => NeedsSleep ? "Недосып" : NeedsRest ? "Усталость" : "Свежие силы";

        public void Spend(int amount)
        {
            if (amount <= 0) return;
            Current -= amount;
            actionsSinceRest = Math.Min(ActionsBeforeRest, actionsSinceRest + 1);
            actionsSinceSleep = Math.Min(ActionsBeforeSleep, actionsSinceSleep + 1);
        }

        public int AvailableRecovery(int amount) => Math.Min(amount, Math.Max(0, Limit - Current));

        public void Recover(int amount) => Current += AvailableRecovery(amount);

        public void Rest(int amount)
        {
            actionsSinceRest = 0;
            Recover(amount);
        }

        public void Sleep(int amount)
        {
            actionsSinceRest = 0;
            actionsSinceSleep = 0;
            Recover(amount);
        }

        public void SkipSleep() => actionsSinceSleep = ActionsBeforeSleep;
    }
}
