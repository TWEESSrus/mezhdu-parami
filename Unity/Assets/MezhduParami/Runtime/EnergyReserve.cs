using System;

namespace MezhduParami
{
    internal sealed class EnergyReserve
    {
        public const int Full = 100;
        public const int MaximumRests = 2;
        public const int DrinkEnergy = 30;
        private const int FatigueFromRoutine = 25;
        private const int MaximumFatigue = 60;
        private const int ActionsBeforeRest = 3;
        private const int ActionsBeforeSleep = 6;

        private int actionsSinceRest;
        private int actionsSinceSleep;
        private int drinkFatigue;
        private float decayTime;

        public int Current { get; private set; } = 80;
        public int Bonus { get; private set; }
        public int Total => Current + Bonus;
        public int RestsRemaining { get; private set; } = MaximumRests;
        public int Fatigue => Math.Min(MaximumFatigue, drinkFatigue + (actionsSinceSleep >= ActionsBeforeSleep
            ? FatigueFromRoutine : actionsSinceRest * FatigueFromRoutine / ActionsBeforeRest));
        public int Limit => Full - Fatigue;
        public int AvailableDrinkEnergy => Bonus == 0 ? Math.Min(DrinkEnergy, Full - Current) : 0;

        public void Spend(int amount)
        {
            if (amount <= 0) return;
            int usedBonus = Math.Min(Bonus, amount);
            Bonus -= usedBonus;
            Current -= amount - usedBonus;
            drinkFatigue = Math.Min(MaximumFatigue, drinkFatigue + usedBonus);
            actionsSinceRest = Math.Min(ActionsBeforeRest, actionsSinceRest + 1);
            actionsSinceSleep = Math.Min(ActionsBeforeSleep, actionsSinceSleep + 1);
        }

        public int AvailableRecovery(int amount) => Math.Min(amount, Math.Max(0, Math.Min(Limit, Full - Bonus) - Current));

        public void Recover(int amount) => Current += AvailableRecovery(amount);

        public void Rest(int amount)
        {
            RestsRemaining--;
            actionsSinceRest = 0;
            Recover(amount);
        }

        public void Sleep()
        {
            actionsSinceRest = 0;
            actionsSinceSleep = 0;
            drinkFatigue = 0;
            Bonus = 0;
            Current = Full;
            RestsRemaining = MaximumRests;
        }

        public void SkipSleep()
        {
            actionsSinceSleep = ActionsBeforeSleep;
            Bonus = 0;
            RestsRemaining = MaximumRests;
        }

        public void Drink()
        {
            Bonus = AvailableDrinkEnergy;
            decayTime = 0;
        }

        public bool Tick(float seconds)
        {
            if (Bonus == 0 || seconds <= 0) return false;
            decayTime += seconds;
            int lost = Math.Min(Bonus, (int)decayTime);
            Bonus -= lost;
            decayTime -= lost;
            return lost > 0;
        }
    }
}
