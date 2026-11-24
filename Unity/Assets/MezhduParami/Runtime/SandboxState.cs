using System;

namespace MezhduParami
{
    public enum SandboxJob { Sorting, CopyCenter, Warehouse, VisitorDesk }
    public enum SandboxSkill { Intelligence, Strength, Charisma }

    // The fixture counts mornings for rest allowances without spending periods.
    // All prices/effects are commands here, never button-side mutations.
    public sealed class SandboxState
    {
        public const int EnergyDrinkPrice = 25;
        public const int EnergyDrinkFatigueThreshold = 60;
        public const int TrainingCost = 25;
        public const int TrainingCoursePrice = 40;
        public const int TrainingCourseSessions = 3;
        public const int RehearsalCost = 15;
        public const int CommunicationGuidePrice = 60;
        public const int CommunicationGuideCost = 15;
        public const int StudyCost = 20;
        public const int FirstShiftExperience = 6;
        public const int PracticeReportExperience = 10;
        private const int EarlyPracticeExperience = 4;
        private const int RewardedPracticeCount = 3;
        private readonly int[] practiceCounts = new int[3];
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
        public int IntelligenceLevel => LevelForExperience(IntelligenceExperience);
        public int StrengthExperience { get; private set; }
        public int StrengthLevel => LevelForExperience(StrengthExperience);
        public bool TrainingCourseOwned { get; private set; }
        public int TrainingCourseRemaining { get; private set; }
        public int TrainingExperienceGain => PreviewPracticeGain(SandboxSkill.Strength, TrainingCourseRemaining > 0 ? 2 : 1);
        public int CharismaExperience { get; private set; }
        public int CharismaLevel => LevelForExperience(CharismaExperience);
        public bool CommunicationGuideOwned { get; private set; }
        public bool CommunicationGuideRead { get; private set; }
        public int Readiness { get; private set; }
        public int Experience { get; private set; }
        public int CharacterLevel => CharacterLevelFor(Experience);
        public int NextCharacterThreshold => CharacterLevel == 1 ? 30 : CharacterLevel == 2 ? 90 : 0;
        public int SkillExperienceLimit => SkillLimitFor(CharacterLevel);
        public bool PracticeReportSubmitted { get; private set; }
        public bool CanSubmitPracticeReport => !PracticeReportSubmitted && ActiveOrder == null
            && (IntelligenceLevel > 0 || StrengthLevel > 0 || CharismaLevel > 0)
            && (SortingCompleted > 0 || CopyCompleted > 0 || WarehouseCompleted > 0 || VisitorDeskCompleted > 0);
        public bool HasStudied => practiceCounts[(int)SandboxSkill.Intelligence] > 0;
        public int StudyReadinessGain => Math.Min(6 - Readiness, IntelligenceLevel >= 2 ? 2 : 1);
        public int StudyExperienceGain => PreviewPracticeGain(SandboxSkill.Intelligence, 1);
        public int RehearsalExperienceGain => PreviewPracticeGain(SandboxSkill.Charisma, 1);
        public string SkillLimitHint => CharacterLevel == 3 ? "Достигнут максимальный уровень навыка."
            : $"Для роста нужен уровень героя {CharacterLevel + 1}.";
        public string BookReadBlock => BookRead ? "Учебник прочитан. Опыт уже получен."
            : ReadingBlock(IntelligenceExperience, 15);
        public string GuideReadBlock => CommunicationGuideRead ? "Практикум прочитан. Опыт уже получен."
            : ReadingBlock(CharismaExperience, CommunicationGuideCost);
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
        public int WarehouseCompleted { get; private set; }
        public int VisitorDeskCompleted { get; private set; }
        public WorkOrder ActiveOrder { get; private set; }
        private int nextOrder;

        public sealed class WorkOrder
        {
            public int Id { get; }
            public SandboxJob Job { get; }
            public int Step { get; internal set; }
            public int SelectedCrate { get; internal set; } = -1;
            public int WarehouseLoad => (targets[0] + targets[1] + targets[2] + targets[3]) / 2;
            private readonly int[] targets;
            internal WorkOrder(int id, SandboxJob job, int seed)
            {
                Id = id; Job = job;
                var random = new Random(seed);
                if (job == SandboxJob.Warehouse)
                {
                    // Two light and two heavy crates guarantee a balanced split.
                    int light = random.Next(2, 5);
                    int heavy = light + random.Next(2, 5);
                    targets = new[] { light, light, heavy, heavy };
                    for (int i = targets.Length - 1; i > 0; i--)
                    {
                        int other = random.Next(i + 1);
                        (targets[i], targets[other]) = (targets[other], targets[i]);
                    }
                }
                else if (job == SandboxJob.VisitorDesk)
                {
                    int first = random.Next(3);
                    targets = new[] { first, (first + random.Next(1, 3)) % 3 };
                }
                else targets = new[] { random.Next(3), random.Next(3), random.Next(3) };
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
            if (BookReadBlock.Length > 0) return Fail(BookReadBlock, out reason);
            bool copyWasLocked = IntelligenceLevel == 0;
            BookRead = true; energy.Spend(15); IntelligenceExperience += 3;
            reason = "Учебник прочитан: опыт интеллекта +3."
                + (copyWasLocked ? " Доступна работа в копицентре." : "");
            return true;
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
        public bool BuyTrainingCourse(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (TrainingCourseOwned) return Fail("Курс уже куплен.", out reason);
            if (Money < TrainingCoursePrice) return Fail($"На курс нужно {TrainingCoursePrice}.", out reason);
            Money -= TrainingCoursePrice;
            TrainingCourseOwned = true;
            TrainingCourseRemaining = TrainingCourseSessions;
            reason = $"Курс тренировок куплен. Следующие {TrainingCourseSessions} тренировки дадут дополнительный опыт силы.";
            return true;
        }
        public bool Train(out string reason)
        {
            if (!CanTrain(out reason)) return false;
            int gain = TrainingExperienceGain;
            bool warehouseWasLocked = StrengthLevel == 0;
            energy.Spend(TrainingCost);
            string reward = CompletePractice(SandboxSkill.Strength);
            StrengthExperience += gain;
            if (gain > 1) TrainingCourseRemaining--;
            reason = "Тренировка завершена." + SkillReward("силы", gain) + reward
                + (warehouseWasLocked && StrengthLevel > 0 ? " Открыта подработка на складе." : "");
            return true;
        }
        public bool Rehearse(out string reason)
        {
            if (!CanRehearse(out reason)) return false;
            int gain = RehearsalExperienceGain;
            reason = GainCharisma(RehearsalCost, gain, "Репетиция завершена.", CompletePractice(SandboxSkill.Charisma));
            return true;
        }
        public bool BuyCommunicationGuide(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (CommunicationGuideOwned) return Fail("Практикум уже куплен.", out reason);
            if (Money < CommunicationGuidePrice) return Fail($"На практикум нужно {CommunicationGuidePrice}.", out reason);
            Money -= CommunicationGuidePrice;
            CommunicationGuideOwned = true;
            reason = "Практикум общения добавлен в рюкзак.";
            return true;
        }
        public bool ReadCommunicationGuide(out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (!CommunicationGuideOwned || CommunicationGuideRead) return Fail("Практикум недоступен для чтения.", out reason);
            if (GuideReadBlock.Length > 0) return Fail(GuideReadBlock, out reason);
            CommunicationGuideRead = true;
            reason = GainCharisma(CommunicationGuideCost, 3, "Практикум прочитан.");
            return true;
        }
        private string GainCharisma(int cost, int experience, string message, string characterReward = "")
        {
            bool deskWasLocked = CharismaLevel == 0;
            energy.Spend(cost);
            CharismaExperience += experience;
            return message + SkillReward("харизмы", experience) + characterReward
                + (deskWasLocked && CharismaLevel > 0 ? " Открыта смена за стойкой информации." : "");
        }
        public bool Study(out string reason)
        {
            if (!CanStudy(out reason)) return false;
            int gain = StudyExperienceGain;
            int readiness = StudyReadinessGain;
            energy.Spend(StudyCost);
            string reward = CompletePractice(SandboxSkill.Intelligence);
            IntelligenceExperience += gain;
            Readiness += readiness;
            reason = "Учёба завершена." + SkillReward("интеллекта", gain)
                + (readiness > 0 ? $" Готовность +{readiness}." : "") + reward;
            return true;
        }
        public bool CanStudy(out string reason) => CanPractice(SandboxSkill.Intelligence, StudyCost,
            StudyExperienceGain + StudyReadinessGain, "Готовность уже 6/6. " + SkillLimitHint, out reason);
        public bool CanTrain(out string reason) => CanPractice(SandboxSkill.Strength, TrainingCost,
            TrainingExperienceGain, SkillLimitHint, out reason);
        public bool CanRehearse(out string reason) => CanPractice(SandboxSkill.Charisma, RehearsalCost,
            RehearsalExperienceGain, SkillLimitHint, out reason);

        private bool CanPractice(SandboxSkill skill, int cost, int gain, string blocked, out string reason)
        {
            if (ActiveOrder != null) return Fail("Сначала закончи или отмени заказ.", out reason);
            if (Energy < cost) return Fail($"Нужно {cost} энергии. Можно отдохнуть или поспать.", out reason);
            if (gain == 0 && PracticeExperience(skill) == 0) return Fail(blocked, out reason);
            reason = "";
            return true;
        }
        public bool SubmitPracticeReport(out string reason)
        {
            if (PracticeReportSubmitted) return Fail("Лист практики уже сдан.", out reason);
            if (!CanSubmitPracticeReport) return Fail("Нужны любой навык уровня 1 и одна завершённая смена.", out reason);
            PracticeReportSubmitted = true;
            reason = "Лист практики сдан." + GainExperience(PracticeReportExperience);
            return true;
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
        public static int Pay(SandboxJob job) => job switch
        {
            SandboxJob.Sorting => 25,
            SandboxJob.Warehouse => 55,
            SandboxJob.VisitorDesk => 40,
            _ => 45
        };
        public static int Cost(SandboxJob job) => job switch
        {
            SandboxJob.Sorting or SandboxJob.VisitorDesk => 15,
            SandboxJob.Warehouse => 25,
            _ => 20
        };
        public bool IsMastered(SandboxJob job) => job switch
        {
            SandboxJob.Sorting => SortingCompleted > 0,
            SandboxJob.CopyCenter => CopyCompleted > 0,
            SandboxJob.Warehouse => WarehouseCompleted > 0,
            SandboxJob.VisitorDesk => VisitorDeskCompleted > 0,
            _ => false
        };
        public bool CanWork(SandboxJob job, out string reason)
        {
            if (ActiveOrder != null) return Fail("Заказ уже открыт.", out reason);
            if (job != SandboxJob.Sorting && job != SandboxJob.CopyCenter && job != SandboxJob.Warehouse && job != SandboxJob.VisitorDesk)
                return Fail("Неизвестная работа.", out reason);
            if (job == SandboxJob.CopyCenter && IntelligenceLevel < 1) return Fail("Нужен интеллект 1: прочитай учебник или трижды позанимайся.", out reason);
            if (job == SandboxJob.Warehouse && StrengthLevel < 1) return Fail("Нужна сила 1. Её можно развить на площадке тренировок.", out reason);
            if (job == SandboxJob.VisitorDesk && CharismaLevel < 1) return Fail("Нужна харизма 1. Помогут репетиции или практикум общения.", out reason);
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
            if (ActiveOrder.Step == 3) return CompleteWork(SandboxJob.Sorting, "Три посылки разобраны.", out reason);
            reason = "Верно! Следующая посылка.";
            return true;
        }
        public bool Copy(int orderId, int format, int copies, int mode, out string reason)
        {
            if (ActiveOrder == null || ActiveOrder.Id != orderId || ActiveOrder.Job != SandboxJob.CopyCenter)
                return Fail("Этот заказ уже закрыт.", out reason);
            if (format != ActiveOrder.Target(0) || copies != ActiveOrder.Target(1) || mode != ActiveOrder.Target(2))
                return Fail("Параметры не совпали с заказом. Исправь настройки — штрафа нет.", out reason);
            return CompleteWork(SandboxJob.CopyCenter, "Заказ выдан.", out reason);
        }
        public bool LoadCrate(int orderId, int crate, out string reason)
        {
            var order = ActiveOrder;
            if (order == null || order.Id != orderId || order.Job != SandboxJob.Warehouse)
                return Fail("Эта смена уже закрыта.", out reason);
            if (crate < 0 || crate >= 4) return Fail("Такого ящика нет.", out reason);
            if (order.SelectedCrate < 0)
            {
                order.SelectedCrate = crate;
                reason = "Первый ящик выбран. Найди второй.";
                return true;
            }
            if (order.SelectedCrate == crate)
            {
                order.SelectedCrate = -1;
                reason = "Выбор снят.";
                return true;
            }
            if (order.Target(order.SelectedCrate) + order.Target(crate) != order.WarehouseLoad)
            {
                order.SelectedCrate = -1;
                return Fail("Тележки получились разного веса. Попробуй другую пару.", out reason);
            }
            return CompleteWork(SandboxJob.Warehouse, "Тележки загружены.", out reason);
        }
        public bool DirectVisitor(int orderId, int destination, out string reason)
        {
            var order = ActiveOrder;
            if (order == null || order.Id != orderId || order.Job != SandboxJob.VisitorDesk)
                return Fail("Эта смена уже закрыта.", out reason);
            if (destination != order.Target(order.Step))
                return Fail("Посетителю нужно другое место. Попробуй ещё раз.", out reason);
            order.Step++;
            if (order.Step == 2) return CompleteWork(SandboxJob.VisitorDesk, "Посетители получили помощь.", out reason);
            reason = "Верно. Подошёл следующий посетитель.";
            return true;
        }
        public bool QuickWork(SandboxJob job, out string reason)
        {
            if (!IsMastered(job)) return Fail("Сначала выполни один заказ вручную.", out reason);
            if (!CanWork(job, out reason)) return false;
            return CompleteWork(job, "Знакомая работа выполнена.", out reason);
        }
        private bool CompleteWork(SandboxJob job, string message, out string reason)
        {
            // ActiveOrder is cleared in the same transaction as the reward.
            ActiveOrder = null;
            if (Energy < Cost(job)) return Fail("Энергии не хватило для завершения. Заказ закрыт без оплаты и затрат.", out reason);
            bool first = !IsMastered(job);
            Money += Pay(job); energy.Spend(Cost(job));
            switch (job)
            {
                case SandboxJob.Sorting: SortingCompleted++; break;
                case SandboxJob.CopyCenter: CopyCompleted++; break;
                case SandboxJob.Warehouse: WarehouseCompleted++; break;
                case SandboxJob.VisitorDesk: VisitorDeskCompleted++; break;
            }
            reason = $"{message} Деньги +{Pay(job)}, энергия -{Cost(job)}."
                + (first ? GainExperience(FirstShiftExperience) + " Открыто быстрое выполнение." : "");
            return true;
        }
        public int PracticeExperience(SandboxSkill skill) => practiceCounts[(int)skill] < RewardedPracticeCount
            ? EarlyPracticeExperience : 0;

        private int PreviewPracticeGain(SandboxSkill skill, int requested)
        {
            int current = skill switch
            {
                SandboxSkill.Intelligence => IntelligenceExperience,
                SandboxSkill.Strength => StrengthExperience,
                _ => CharismaExperience
            };
            int nextLimit = SkillLimitFor(CharacterLevelFor(Experience + PracticeExperience(skill)));
            return Math.Min(requested, nextLimit - current);
        }
        private string CompletePractice(SandboxSkill skill)
        {
            int reward = PracticeExperience(skill);
            if (reward == 0) return "";
            practiceCounts[(int)skill]++;
            return GainExperience(reward);
        }
        private string GainExperience(int amount)
        {
            int previous = CharacterLevel;
            Experience += amount;
            return $" Общий опыт +{amount}." + (CharacterLevel > previous ? $" Уровень героя {CharacterLevel}!" : "");
        }
        private string ReadingBlock(int experience, int cost)
        {
            if (SkillExperienceLimit - experience < 3)
                return SkillLimitHint + " Для чтения нужно место для +3 опыта.";
            return Energy < cost ? $"Для чтения нужно {cost} энергии." : "";
        }
        private static string SkillReward(string skill, int gain) => gain > 0 ? $" Опыт {skill} +{gain}." : "";
        private static int CharacterLevelFor(int experience) => experience >= 90 ? 3 : experience >= 30 ? 2 : 1;
        private static int SkillLimitFor(int level) => level == 3 ? 15 : level == 2 ? 8 : 3;
        private static int LevelForExperience(int experience) => experience >= 15 ? 3 : experience >= 8 ? 2 : experience >= 3 ? 1 : 0;
        private static bool Fail(string text, out string reason) { reason = text; return false; }
    }
}
