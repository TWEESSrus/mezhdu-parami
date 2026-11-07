using System;

namespace MezhduParami
{
    public enum SandboxJob { Sorting, CopyCenter }

    // A clock-free mechanics fixture. Campaign rules remain in PrototypeDay.
    // All prices/effects are commands here, never button-side mutations.
    public sealed class SandboxState
    {
        public int Money { get; private set; } = 120;
        public int Energy { get; private set; } = 80;
        public int Health { get; private set; } = 80;
        public int IntelligenceExperience { get; private set; }
        public int IntelligenceLevel => IntelligenceExperience >= 15 ? 3 : IntelligenceExperience >= 8 ? 2 : IntelligenceExperience >= 3 ? 1 : 0;
        public int Readiness { get; private set; }
        public bool BookOwned { get; private set; }
        public bool BookRead { get; private set; }
        public int Kits { get; private set; }
        public int KitsBought { get; private set; }
        public int KitsUsed { get; private set; }
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
            reason = "Учебник куплен. Прочитай его в вещах [I]: энергия -15, опыт интеллекта +3 один раз."; return true;
        }
        public bool ReadBook(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (!BookOwned || BookRead) return Fail(BookRead ? "Этот учебник уже прочитан." : "Сначала купи учебник.", out reason);
            if (Energy < 15) return Fail("Для чтения нужно 15 энергии.", out reason);
            BookRead = true; Energy -= 15; IntelligenceExperience += 3;
            reason = "Учебник прочитан: опыт интеллекта +3. Доступна работа в копицентре."; return true;
        }
        public bool BuyKit(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (KitsBought >= 2) return Fail("Оба набора из тестового запаса уже куплены.", out reason);
            if (Money < 20) return Fail("Для набора восстановления нужно 20.", out reason);
            Money -= 20; Kits++; KitsBought++;
            reason = "Набор куплен. Примени его в вещах [I]: здоровье +20, до 100."; return true;
        }
        public bool UseKit(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (Kits <= 0 || Health >= 100) return Fail(Kits <= 0 ? "Нет набора восстановления." : "Здоровье уже полное. Набор сохранён.", out reason);
            Kits--; KitsUsed++; Health = Math.Min(100, Health + 20);
            reason = "Набор применён: здоровье +20, до 100."; return true;
        }
        public bool Study(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (Readiness >= 6) return Fail("Готовность уже 6/6. Для этой проверки достаточно.", out reason);
            if (Energy < 20) return Fail("Для учёбы нужно 20 энергии.", out reason);
            Energy -= 20; IntelligenceExperience++; Readiness++;
            reason = "Учёба завершена: опыт интеллекта +1, готовность +1."; return true;
        }
        public bool Rest(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (Energy >= 100) return Fail("Силы уже восстановлены.", out reason);
            Energy = Math.Min(100, Energy + 40);
            reason = "Отдых: энергия +40, до 100. В полигоне нет календаря."; return true;
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
            if (ActiveOrder.Step == 3) CompleteWork(SandboxJob.Sorting);
            reason = ActiveOrder == null ? "Три посылки разобраны. Деньги +25, энергия -15. Открыто быстрое выполнение." : "Верно! Следующая посылка.";
            return true;
        }
        public bool Copy(int orderId, int format, int copies, int mode, out string reason)
        {
            if (ActiveOrder == null || ActiveOrder.Id != orderId || ActiveOrder.Job != SandboxJob.CopyCenter)
                return Fail("Этот заказ уже закрыт.", out reason);
            if (format != ActiveOrder.Target(0) || copies != ActiveOrder.Target(1) || mode != ActiveOrder.Target(2))
                return Fail("Параметры не совпали с заказом. Исправь настройки — штрафа нет.", out reason);
            CompleteWork(SandboxJob.CopyCenter);
            reason = "Заказ выдан. Деньги +45, энергия -20. Открыто быстрое выполнение."; return true;
        }
        public bool QuickWork(SandboxJob job, out string reason)
        {
            if (!IsMastered(job)) return Fail("Сначала выполни один заказ вручную.", out reason);
            if (!CanWork(job, out reason)) return false;
            CompleteWork(job);
            reason = $"Знакомая работа выполнена: деньги +{Pay(job)}, энергия -{Cost(job)}."; return true;
        }
        private void CompleteWork(SandboxJob job)
        {
            // ActiveOrder is cleared in the same transaction as the reward.
            ActiveOrder = null;
            Money += Pay(job); Energy -= Cost(job);
            if (job == SandboxJob.Sorting) SortingCompleted++; else CopyCompleted++;
        }
        private static bool Fail(string text, out string reason) { reason = text; return false; }
    }
}
