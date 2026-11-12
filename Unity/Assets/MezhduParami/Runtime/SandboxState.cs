using System;

namespace MezhduParami
{
    public enum SandboxJob { Sorting, CopyCenter }

    // The fixture counts mornings for rest allowances without spending periods.
    // All prices/effects are commands here, never button-side mutations.
    public sealed class SandboxState
    {
        public const int EnergyDrinkPrice = 25;
        public const int EnergyDrinkFatigueThreshold = 60;
        public int Money { get; private set; } = 120;
        public int DayNumber { get; private set; } = 1;
        private readonly EnergyReserve energy = new EnergyReserve();
        public int Energy => energy.Total;
        public int RegularEnergy => energy.Current;
        public int BonusEnergy => energy.Bonus;
        public int Fatigue => energy.Fatigue;
        public int RecoveryLimit => energy.Limit;
        public int RestsRemaining => energy.RestsRemaining;
        public bool CanRest => RestsRemaining > 0;
        public int IntelligenceExperience { get; private set; }
        public int IntelligenceLevel => IntelligenceExperience >= 15 ? 3 : IntelligenceExperience >= 8 ? 2 : IntelligenceExperience >= 3 ? 1 : 0;
        public int Readiness { get; private set; }
        public bool BookOwned { get; private set; }
        public bool BookRead { get; private set; }
        public int Snacks { get; private set; }
        public int SnacksBought { get; private set; }
        public int SnacksUsed { get; private set; }
        public int SnackRecovery => energy.AvailableRecovery(20);
        public bool HasEnergyDrink { get; private set; }
        public int DrinkBonus => energy.AvailableDrinkEnergy;
        public string DrinkBlockReason
        {
            get
            {
                if (BonusEnergy > 0) return "Предыдущий энергетик ещё действует.";
                if (RegularEnergy == 0) return "При нулевой энергии нужен отдых или сон.";
                if (Fatigue > EnergyDrinkFatigueThreshold) return $"Усталость выше {EnergyDrinkFatigueThreshold}. Поможет сон.";
                return DrinkBonus == 0 ? "Энергия уже полная." : string.Empty;
            }
        }
        public int SortingCompleted { get; private set; }
        public int CopyCompleted { get; private set; }
        public WorkOrder ActiveOrder { get; private set; }
        private int nextOrder;

        public sealed class WorkOrder
        {
            public int Id { get; }
            public SandboxJob Job { get; }
            public int Step { get; internal set; }
            private readonly int[] targets;
            internal WorkOrder(int id, SandboxJob job, int seed)
            {
                Id = id; Job = job;
                var random = new Random(seed);
                targets = new[] { random.Next(3), random.Next(3), random.Next(3) };
            }
            public int Target(int step) => targets[step];
        }

        public bool BuyBook(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (BookOwned) return Fail("Учебник уже куплен. Повторный опыт за него не выдаётся.", out reason);
            if (Money < 60) return Fail("Для учебника нужно 60.", out reason);
            Money -= 60; BookOwned = true;
            reason = "Учебник добавлен в рюкзак."; return true;
        }
        public bool ReadBook(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (!BookOwned || BookRead) return Fail(BookRead ? "Этот учебник уже прочитан." : "Сначала купи учебник.", out reason);
            if (Energy < 15) return Fail("Для чтения нужно 15 энергии.", out reason);
            BookRead = true; energy.Spend(15); IntelligenceExperience += 3;
            reason = "Учебник прочитан: опыт интеллекта +3. Доступна работа в копицентре."; return true;
        }
        public bool BuySnack(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (SnacksBought >= 2) return Fail("Оба перекуса уже куплены.", out reason);
            if (Money < 20) return Fail("На перекус нужно 20.", out reason);
            Money -= 20;
            Snacks++;
            SnacksBought++;
            reason = "Перекус куплен. Он восстанавливает до 20 энергии.";
            return true;
        }
        public bool EatSnack(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (Snacks <= 0) return Fail("В рюкзаке нет перекуса.", out reason);
            int restored = SnackRecovery;
            if (restored == 0) return Fail("Достигнут предел восстановления. Перекус остался в рюкзаке.", out reason);
            Snacks--;
            SnacksUsed++;
            energy.Recover(restored);
            reason = $"Перекус съеден. Энергия +{restored}.";
            return true;
        }
        public bool BuyEnergyDrink(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (HasEnergyDrink) return Fail("В рюкзаке уже есть энергетик.", out reason);
            if (Money < EnergyDrinkPrice) return Fail($"На энергетик нужно {EnergyDrinkPrice}.", out reason);
            Money -= EnergyDrinkPrice;
            HasEnergyDrink = true;
            reason = "Энергетик добавлен в рюкзак.";
            return true;
        }
        public bool DrinkEnergyDrink(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (!HasEnergyDrink) return Fail("В рюкзаке нет энергетика.", out reason);
            string blocked = DrinkBlockReason;
            if (blocked.Length > 0) return Fail(blocked, out reason);
            energy.Drink();
            HasEnergyDrink = false;
            reason = $"Временная энергия +{BonusEnergy}. Потрать её, пока она не исчезла.";
            return true;
        }
        public bool TickEnergy(float seconds) => energy.Tick(seconds);
        public bool Study(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (Readiness >= 6) return Fail("Готовность уже 6/6. Для этой проверки достаточно.", out reason);
            if (Energy < 20) return Fail("Для учёбы нужно 20 энергии.", out reason);
            energy.Spend(20); IntelligenceExperience++; Readiness++;
            reason = "Учёба завершена: опыт интеллекта +1, готовность +1."; return true;
        }
        public bool Rest(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (!CanRest) return Fail("Оба отдыха на сегодня использованы. Можно лечь спать.", out reason);
            int before = Energy;
            energy.Rest(40);
            reason = $"Отдых. Энергия +{Energy - before}. Осталось отдыхов {RestsRemaining}/2.";
            return true;
        }
        public bool Sleep(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            energy.Sleep();
            DayNumber++;
            reason = $"День {DayNumber}. Энергия восстановлена до 100.";
            return true;
        }
        public static int Pay(SandboxJob job) => job == SandboxJob.Sorting ? 25 : 45;
        public static int Cost(SandboxJob job) => job == SandboxJob.Sorting ? 15 : 20;
        public bool IsMastered(SandboxJob job) => job == SandboxJob.Sorting ? SortingCompleted > 0 : CopyCompleted > 0;
        public bool CanWork(SandboxJob job, out string reason)
        {
            if (ActiveOrder != null) return Fail("Заказ уже открыт.", out reason);
            if (job != SandboxJob.Sorting && job != SandboxJob.CopyCenter) return Fail("Неизвестная работа.", out reason);
            if (job == SandboxJob.CopyCenter && IntelligenceLevel < 1) return Fail("Нужен интеллект 1: прочитай учебник или трижды позанимайся.", out reason);
            if (Energy < Cost(job)) return Fail("Недостаточно энергии. Можно отдохнуть.", out reason);
            reason = ""; return true;
        }
        public bool BeginWork(SandboxJob job, int seed, out string reason)
        {
            if (!CanWork(job, out reason)) return false;
            ActiveOrder = new WorkOrder(++nextOrder, job, seed);
            reason = "Заказ открыт. Оплата и расход энергии — после выполнения. Отмена бесплатна."; return true;
        }
        public void CancelWork() => ActiveOrder = null;
        public bool Sort(int orderId, int bin, out string reason)
        {
            if (ActiveOrder == null || ActiveOrder.Id != orderId || ActiveOrder.Job != SandboxJob.Sorting)
                return Fail("Этот заказ уже закрыт.", out reason);
            if (bin != ActiveOrder.Target(ActiveOrder.Step)) return Fail("Эта посылка для другой полки. Попробуй ещё раз — штрафа нет.", out reason);
            ActiveOrder.Step++;
            if (ActiveOrder.Step == 3 && !CompleteWork(SandboxJob.Sorting, out reason)) return false;
            reason = ActiveOrder == null ? "Три посылки разобраны. Деньги +25, энергия -15. Открыто быстрое выполнение." : "Верно! Следующая посылка.";
            return true;
        }
        public bool Copy(int orderId, int format, int copies, int mode, out string reason)
        {
            if (ActiveOrder == null || ActiveOrder.Id != orderId || ActiveOrder.Job != SandboxJob.CopyCenter)
                return Fail("Этот заказ уже закрыт.", out reason);
            if (format != ActiveOrder.Target(0) || copies != ActiveOrder.Target(1) || mode != ActiveOrder.Target(2))
                return Fail("Параметры не совпали с заказом. Исправь настройки — штрафа нет.", out reason);
            if (!CompleteWork(SandboxJob.CopyCenter, out reason)) return false;
            reason = "Заказ выдан. Деньги +45, энергия -20. Открыто быстрое выполнение."; return true;
        }
        public bool QuickWork(SandboxJob job, out string reason)
        {
            if (!IsMastered(job)) return Fail("Сначала выполни один заказ вручную.", out reason);
            if (!CanWork(job, out reason)) return false;
            if (!CompleteWork(job, out reason)) return false;
            reason = $"Знакомая работа выполнена: деньги +{Pay(job)}, энергия -{Cost(job)}."; return true;
        }
        private bool CompleteWork(SandboxJob job, out string reason)
        {
            // ActiveOrder is cleared in the same transaction as the reward.
            ActiveOrder = null;
            if (Energy < Cost(job)) return Fail("Энергии не хватило для завершения. Заказ закрыт без оплаты и затрат.", out reason);
            Money += Pay(job); energy.Spend(Cost(job));
            if (job == SandboxJob.Sorting) SortingCompleted++; else CopyCompleted++;
            reason = string.Empty;
            return true;
        }
        private static bool Fail(string text, out string reason) { reason = text; return false; }
    }
}
