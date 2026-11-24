using NUnit.Framework;

namespace MezhduParami.Tests
{
    public sealed class SandboxStateTests
    {
        [Test] public void FirstShiftAndReportRewardOnceAndTheLevelUpOpensTheNewSkillCapImmediately()
        {
            var state = new SandboxState();
            Assert.That(state.SubmitPracticeReport(out _), Is.False);
            Assert.That(state.BuyBook(out _), Is.True);
            Assert.That(state.ReadBook(out _), Is.True);
            Assert.That(state.Experience, Is.Zero);
            Assert.That(state.Sleep(out _), Is.True);
            for (int i = 0; i < 3; i++) Assert.That(state.Train(out _), Is.True);
            Assert.That(state.Experience, Is.EqualTo(12));
            Assert.That(state.SkillExperienceLimit, Is.EqualTo(3));
            Assert.That(state.Train(out _), Is.False);
            FinishSorting(state);
            Assert.That(state.Rest(out _), Is.True);
            Assert.That(state.QuickWork(SandboxJob.Sorting, out _), Is.True);
            Assert.That(state.Experience, Is.EqualTo(18));
            Assert.That(state.SubmitPracticeReport(out _), Is.True);
            Assert.That(state.SubmitPracticeReport(out _), Is.False);
            Assert.That(state.Experience, Is.EqualTo(28));
            Assert.That(state.CharacterLevel, Is.EqualTo(1));
            Assert.That(state.Sleep(out _), Is.True);
            Assert.That(state.Study(out var result), Is.True);
            Assert.That(result, Does.Contain("Уровень героя 2"));
            Assert.That(state.CharacterLevel, Is.EqualTo(2));
            Assert.That(state.SkillExperienceLimit, Is.EqualTo(8));
            Assert.That(state.IntelligenceExperience, Is.EqualTo(4));
            Assert.That(state.Experience, Is.EqualTo(32));
        }

        [Test] public void BookStaysUnreadUntilItsFullRewardFitsUnderTheCap()
        {
            var state = new SandboxState();
            Assert.That(state.Study(out _), Is.True);
            Assert.That(state.BuyBook(out _), Is.True);
            Assert.That(state.ReadBook(out _), Is.False);
            Assert.That(state.BookRead, Is.False);
            Assert.That(state.Energy, Is.EqualTo(60));
            Assert.That(state.IntelligenceExperience, Is.EqualTo(1));
        }

        private static void FinishSorting(SandboxState state)
        {
            Assert.That(state.BeginWork(SandboxJob.Sorting, 12, out _), Is.True);
            var order = state.ActiveOrder;
            for (int i = 0; i < 3; i++) Assert.That(state.Sort(order.Id, order.Target(i), out _), Is.True);
        }

        [Test] public void RehearsalAndReadingAreAlternativeWaysToOpenVisitorWork()
        {
            var rehearsal = new SandboxState();
            Assert.That(rehearsal.CanWork(SandboxJob.VisitorDesk, out _), Is.False);
            for (int i = 0; i < 3; i++) Assert.That(rehearsal.Rehearse(out _), Is.True);
            Assert.That(rehearsal.CharismaLevel, Is.EqualTo(1));
            Assert.That(rehearsal.Energy, Is.EqualTo(35));
            Assert.That(rehearsal.Money, Is.EqualTo(120));
            Assert.That(rehearsal.CanWork(SandboxJob.VisitorDesk, out _), Is.True);

            var reading = new SandboxState();
            Assert.That(reading.BuyCommunicationGuide(out _), Is.True);
            Assert.That(reading.CharismaExperience, Is.Zero);
            Assert.That(reading.ReadCommunicationGuide(out _), Is.True);
            Assert.That(reading.ReadCommunicationGuide(out _), Is.False);
            Assert.That(reading.CharismaExperience, Is.EqualTo(3));
            Assert.That(reading.Energy, Is.EqualTo(65));
            Assert.That(reading.Money, Is.EqualTo(60));
            Assert.That(reading.CanWork(SandboxJob.VisitorDesk, out _), Is.True);
        }

        [Test] public void VisitorShiftPaysOnlyOnceAfterBothVisitorsAreHelped()
        {
            var state = new SandboxState();
            for (int i = 0; i < 3; i++) Assert.That(state.Rehearse(out _), Is.True);
            Assert.That(state.BeginWork(SandboxJob.VisitorDesk, 13, out _), Is.True);
            var order = state.ActiveOrder;
            Assert.That(order.Target(0), Is.Not.EqualTo(order.Target(1)));
            Assert.That(state.DirectVisitor(order.Id, order.Target(0), out _), Is.True);
            Assert.That(state.DirectVisitor(order.Id, order.Target(0), out _), Is.False);
            Assert.That(state.Money, Is.EqualTo(120));
            Assert.That(state.DirectVisitor(order.Id, order.Target(1), out _), Is.True);
            Assert.That(state.DirectVisitor(order.Id, order.Target(1), out _), Is.False);
            Assert.That(state.Money, Is.EqualTo(160));
            Assert.That(state.Energy, Is.EqualTo(20));
            Assert.That(state.QuickWork(SandboxJob.VisitorDesk, out _), Is.True);
            Assert.That(state.Money, Is.EqualTo(200));
            Assert.That(state.Energy, Is.EqualTo(5));
        }

        [Test] public void TrainingOpensWarehouseAtThreeExperienceWithoutBuyingTheCourse()
        {
            var state = new SandboxState();
            Assert.That(state.BeginWork(SandboxJob.Warehouse, 5, out _), Is.False);
            Assert.That(state.Train(out _), Is.True);
            Assert.That(state.Train(out _), Is.True);
            Assert.That(state.BeginWork(SandboxJob.Warehouse, 5, out _), Is.False);
            Assert.That(state.Train(out _), Is.True);
            Assert.That(state.StrengthLevel, Is.EqualTo(1));
            Assert.That(state.Energy, Is.EqualTo(5));
            Assert.That(state.Money, Is.EqualTo(120));
            Assert.That(state.CanWork(SandboxJob.Warehouse, out var reason), Is.False);
            Assert.That(reason, Does.Contain("энергии"));
            Assert.That(state.Rest(out _), Is.True);
            Assert.That(state.CanWork(SandboxJob.Warehouse, out _), Is.True);
        }

        [Test] public void PaidCourseAppliesToExactlyThreeSuccessfulTrainingSessions()
        {
            var state = new SandboxState();
            for (int i = 0; i < 3; i++) Assert.That(state.Study(out _), Is.True);
            Assert.That(state.BuyTrainingCourse(out _), Is.True);
            Assert.That(state.Train(out _), Is.False);
            Assert.That(state.TrainingCourseRemaining, Is.EqualTo(3));
            Assert.That(state.StrengthExperience, Is.Zero);
            Assert.That(state.Sleep(out _), Is.True);
            FinishSorting(state);
            Assert.That(state.SubmitPracticeReport(out _), Is.True);
            Assert.That(state.Rest(out _), Is.True);
            Assert.That(state.Train(out _), Is.True);
            Assert.That(state.StrengthLevel, Is.Zero);
            Assert.That(state.Train(out _), Is.True);
            Assert.That(state.StrengthLevel, Is.EqualTo(1));
            Assert.That(state.Train(out _), Is.True);
            Assert.That(state.TrainingCourseRemaining, Is.Zero);
            Assert.That(state.StrengthExperience, Is.EqualTo(6));
            Assert.That(state.BuyTrainingCourse(out _), Is.False);
            Assert.That(state.Money, Is.EqualTo(105));
            Assert.That(state.Rest(out _), Is.True);
            Assert.That(state.Train(out _), Is.True);
            Assert.That(state.StrengthExperience, Is.EqualTo(7));
        }

        [Test] public void WarehousePaysOnceForAnEqualSplitAndUnlocksQuickWork()
        {
            var state = new SandboxState();
            Assert.That(state.BuyTrainingCourse(out _), Is.True);
            Assert.That(state.Train(out _), Is.True);
            Assert.That(state.Train(out _), Is.True);
            Assert.That(state.QuickWork(SandboxJob.Warehouse, out _), Is.False);
            Assert.That(state.BeginWork(SandboxJob.Warehouse, 17, out _), Is.True);
            var order = state.ActiveOrder;
            int same = System.Linq.Enumerable.Single(System.Linq.Enumerable.Range(1, 3), i => order.Target(i) == order.Target(0));
            int different = System.Linq.Enumerable.First(System.Linq.Enumerable.Range(1, 3), i => order.Target(i) != order.Target(0));
            Assert.That(state.LoadCrate(order.Id, 0, out _), Is.True);
            Assert.That(state.LoadCrate(order.Id, same, out _), Is.False);
            Assert.That(state.Money, Is.EqualTo(80));
            Assert.That(state.Energy, Is.EqualTo(30));
            Assert.That(state.LoadCrate(order.Id, 0, out _), Is.True);
            Assert.That(state.LoadCrate(order.Id, different, out _), Is.True);
            Assert.That(state.LoadCrate(order.Id, different, out _), Is.False);
            Assert.That(state.Money, Is.EqualTo(135));
            Assert.That(state.Energy, Is.EqualTo(5));
            Assert.That(state.WarehouseCompleted, Is.EqualTo(1));
            Assert.That(state.Rest(out _), Is.True);
            Assert.That(state.QuickWork(SandboxJob.Warehouse, out _), Is.True);
            Assert.That(state.WarehouseCompleted, Is.EqualTo(2));
            Assert.That(state.Money, Is.EqualTo(190));
        }

        [Test] public void BuyingDoesNotGrantExperienceAndReadingGrantsItOnce()
        {
            var state = new SandboxState();
            Assert.That(state.ReadBook(out _), Is.False);
            Assert.That(state.BuyBook(out _), Is.True);
            Assert.That(state.Money, Is.EqualTo(60));
            Assert.That(state.IntelligenceExperience, Is.Zero);
            Assert.That(state.BuyBook(out _), Is.False);
            Assert.That(state.ReadBook(out _), Is.True);
            Assert.That(state.ReadBook(out _), Is.False);
            Assert.That(state.IntelligenceExperience, Is.EqualTo(3));
            Assert.That(state.IntelligenceLevel, Is.EqualTo(1));
            Assert.That(state.Energy, Is.EqualTo(65));
        }
        [Test] public void SnackRespectsRecoveryLimitAndIsKeptAtTheLimit()
        {
            var state = new SandboxState();
            Assert.That(state.Sleep(out _), Is.True);
            Assert.That(state.BuyBook(out _), Is.True);
            Assert.That(state.ReadBook(out _), Is.True);
            Assert.That(state.BeginWork(SandboxJob.Sorting, 10, out _), Is.True);
            var order = state.ActiveOrder;
            for (int i = 0; i < 3; i++) state.Sort(order.Id, order.Target(i), out _);
            Assert.That(state.QuickWork(SandboxJob.Sorting, out _), Is.True);
            Assert.That(state.RecoveryLimit, Is.EqualTo(75));
            Assert.That(state.BuySnack(out _), Is.True);
            Assert.That(state.BuySnack(out _), Is.True);
            Assert.That(state.BuySnack(out _), Is.False);
            Assert.That(state.EatSnack(out _), Is.True);
            Assert.That(state.Energy, Is.EqualTo(75));
            Assert.That(state.EatSnack(out _), Is.False);
            Assert.That(state.Snacks, Is.EqualTo(1));
            Assert.That(state.SnacksUsed, Is.EqualTo(1));
            Assert.That(state.Rest(out _), Is.True);
            Assert.That(state.RecoveryLimit, Is.EqualTo(100));
        }
        [Test] public void ReadingAndWorkCannotSpendMissingEnergy()
        {
            var state = new SandboxState();
            state.BuyBook(out _);
            for (int i = 0; i < 4; i++) Assert.That(state.Study(out _), Is.True);
            Assert.That(state.Energy, Is.Zero);
            Assert.That(state.ReadBook(out _), Is.False);
            Assert.That(state.BookRead, Is.False);
            Assert.That(state.BeginWork(SandboxJob.Sorting, 1, out _), Is.False);
            Assert.That(state.ActiveOrder, Is.Null);
            Assert.That(state.Money, Is.EqualTo(60));
        }
        [Test] public void CopyCenterRequiresSkillWhichCanAlsoBeEarnedByStudy()
        {
            var state = new SandboxState();
            Assert.That(state.BeginWork(SandboxJob.CopyCenter, 1, out _), Is.False);
            for (int i = 0; i < 3; i++) state.Study(out _);
            Assert.That(state.BookOwned, Is.False);
            Assert.That(state.BeginWork(SandboxJob.CopyCenter, 1, out _), Is.True);
        }
        [Test] public void PartialOrderCancellationAndStaleInputNeverPay()
        {
            var state = new SandboxState(); state.BeginWork(SandboxJob.Sorting, 11, out _);
            var order = state.ActiveOrder;
            state.Sort(order.Id, order.Target(0), out _); state.Sort(order.Id, order.Target(1), out _);
            state.CancelWork();
            Assert.That(state.Sort(order.Id, order.Target(2), out _), Is.False);
            Assert.That(state.Money, Is.EqualTo(120)); Assert.That(state.Energy, Is.EqualTo(80));
            state.BeginWork(SandboxJob.Sorting, 11, out _);
            Assert.That(state.Sort(order.Id, order.Target(2), out _), Is.False);
            Assert.That(state.ActiveOrder.Step, Is.Zero);
        }
        [Test] public void SortingHasNoMistakePenaltyAndRewardsExactlyOnce()
        {
            var state = new SandboxState(); state.BeginWork(SandboxJob.Sorting, 12, out _);
            var order = state.ActiveOrder;
            Assert.That(state.Sort(order.Id, (order.Target(0) + 1) % 3, out _), Is.False);
            Assert.That(state.ActiveOrder.Step, Is.Zero);
            Assert.That(state.Energy, Is.EqualTo(80));
            for (int i = 0; i < 3; i++) Assert.That(state.Sort(order.Id, order.Target(i), out _), Is.True);
            Assert.That(state.Sort(order.Id, order.Target(2), out _), Is.False);
            Assert.That(state.Money, Is.EqualTo(145)); Assert.That(state.Energy, Is.EqualTo(65));
            Assert.That(state.SortingCompleted, Is.EqualTo(1)); Assert.That(state.ActiveOrder, Is.Null);
        }
        [Test] public void CopyChecksAllSettingsBeforePayingAndCannotPayAgain()
        {
            var state = new SandboxState(); state.BuyBook(out _); state.ReadBook(out _);
            state.BeginWork(SandboxJob.CopyCenter, 13, out _); var order = state.ActiveOrder;
            Assert.That(state.Copy(order.Id, (order.Target(0) + 1) % 3, order.Target(1), order.Target(2), out _), Is.False);
            Assert.That(state.Money, Is.EqualTo(60)); Assert.That(state.Energy, Is.EqualTo(65));
            Assert.That(state.Copy(order.Id, order.Target(0), order.Target(1), order.Target(2), out _), Is.True);
            Assert.That(state.Copy(order.Id, order.Target(0), order.Target(1), order.Target(2), out _), Is.False);
            Assert.That(state.Money, Is.EqualTo(105)); Assert.That(state.Energy, Is.EqualTo(45));
            Assert.That(state.CopyCompleted, Is.EqualTo(1));
        }
        [Test] public void QuickWorkUnlocksOnlyAfterSuccessAndUsesSamePrice()
        {
            var state = new SandboxState(); Assert.That(state.QuickWork(SandboxJob.Sorting, out _), Is.False);
            state.BeginWork(SandboxJob.Sorting, 4, out _); var order = state.ActiveOrder;
            for (int i = 0; i < 3; i++) state.Sort(order.Id, order.Target(i), out _);
            Assert.That(state.QuickWork(SandboxJob.Sorting, out _), Is.True);
            Assert.That(state.Money, Is.EqualTo(170)); Assert.That(state.Energy, Is.EqualTo(50));
            state.BeginWork(SandboxJob.Sorting, 5, out _);
            Assert.That(state.QuickWork(SandboxJob.Sorting, out _), Is.False);
            Assert.That(state.BuyBook(out _), Is.False);
            Assert.That(state.Rest(out _), Is.False);
        }
        [Test] public void RepeatedActionsWithoutSleepLimitRestUntilThePlayerSleeps()
        {
            var state = new SandboxState();
            for (int i = 0; i < 6; i++)
            {
                if (i == 3) Assert.That(state.Rest(out _), Is.True);
                Assert.That(state.Study(out _), Is.True);
            }
            Assert.That(state.Study(out _), Is.False);
            Assert.That(state.Rest(out _), Is.True);
            Assert.That(state.Rest(out _), Is.False);
            Assert.That(state.Energy, Is.EqualTo(40));
            Assert.That(state.RecoveryLimit, Is.EqualTo(75));
            Assert.That(state.Sleep(out _), Is.True);
            Assert.That(state.Energy, Is.EqualTo(100)); Assert.That(state.Readiness, Is.EqualTo(6));
            Assert.That(state.RecoveryLimit, Is.EqualTo(100));
            Assert.That(state.RestsRemaining, Is.EqualTo(2));
            Assert.That(state.DayNumber, Is.EqualTo(2));
        }

        [Test] public void EnergyDrinkCrossesTheLimitAndCrashesOnceAfterDecay()
        {
            var state = new SandboxState();
            for (int i = 0; i < 3; i++) Assert.That(state.Study(out _), Is.True);
            for (int i = 0; i < 2; i++)
            {
                Assert.That(state.BuySnack(out _), Is.True);
                Assert.That(state.EatSnack(out _), Is.True);
            }
            Assert.That(state.RegularEnergy, Is.EqualTo(60));
            Assert.That(state.BuyEnergyDrink(out _), Is.True);
            Assert.That(state.DrinkEnergyDrink(out _), Is.True);
            Assert.That(state.Energy, Is.EqualTo(90));
            Assert.That(state.RecoveryLimit, Is.EqualTo(75));
            Assert.That(state.TickEnergy(.5f), Is.False);
            Assert.That(state.TickEnergy(.5f), Is.True);
            Assert.That(state.BonusEnergy, Is.EqualTo(29));
            Assert.That(state.Fatigue, Is.EqualTo(25));
            Assert.That(state.BuyEnergyDrink(out _), Is.True);
            Assert.That(state.DrinkEnergyDrink(out _), Is.False);
            Assert.That(state.HasEnergyDrink, Is.True);
            Assert.That(state.Study(out _), Is.True);
            Assert.That(state.RegularEnergy, Is.EqualTo(60));
            Assert.That(state.BonusEnergy, Is.EqualTo(9));
            Assert.That(state.Fatigue, Is.EqualTo(25));
            Assert.That(state.TickEnergy(9f), Is.True);
            Assert.That(state.BonusEnergy, Is.Zero);
            Assert.That(state.RegularEnergy, Is.EqualTo(55));
            Assert.That(state.Fatigue, Is.EqualTo(30));
            Assert.That(state.TickEnergy(30f), Is.False);
            Assert.That(state.RegularEnergy, Is.EqualTo(55));
            Assert.That(state.Fatigue, Is.EqualTo(30));
            Assert.That(state.Sleep(out _), Is.True);
            Assert.That(state.Fatigue, Is.Zero);
            Assert.That(state.Energy, Is.EqualTo(100));
        }

        [Test] public void SpendingTheLastBonusEnergyTriggersTheSameCrash()
        {
            var state = new SandboxState();
            Assert.That(state.BuyEnergyDrink(out _), Is.True);
            Assert.That(state.DrinkEnergyDrink(out _), Is.True);
            Assert.That(state.Fatigue, Is.Zero);
            Assert.That(state.Study(out _), Is.True);
            Assert.That(state.BonusEnergy, Is.Zero);
            Assert.That(state.RegularEnergy, Is.EqualTo(75));
            Assert.That(state.Fatigue, Is.EqualTo(13), "8 from study plus 5 from the crash");
            Assert.That(state.TickEnergy(1f), Is.False);
            Assert.That(state.RegularEnergy, Is.EqualTo(75));
        }

        [Test] public void EnergyDrinkIsKeptWhenRegularEnergyIsZero()
        {
            var state = new SandboxState();
            for (int i = 0; i < 4; i++) Assert.That(state.Study(out _), Is.True);
            Assert.That(state.BuyEnergyDrink(out _), Is.True);
            Assert.That(state.DrinkEnergyDrink(out var reason), Is.False);
            Assert.That(reason, Does.Contain("нулевой"));
            Assert.That(state.HasEnergyDrink, Is.True);
            Assert.That(state.Energy, Is.Zero);
        }

        [Test] public void EnergyDrinkIsAvailableAtSixtyFatigueAndTotalFatigueIsCappedAtSeventy()
        {
            var state = new SandboxState();
            Assert.That(state.BeginWork(SandboxJob.Sorting, 7, out _), Is.True);
            var order = state.ActiveOrder;
            for (int i = 0; i < 3; i++) Assert.That(state.Sort(order.Id, order.Target(i), out _), Is.True);
            for (int day = 0; day < 2; day++)
            {
                for (int i = 0; i < 4; i++) Assert.That(state.QuickWork(SandboxJob.Sorting, out _), Is.True);
                Assert.That(state.Sleep(out _), Is.True);
            }
            for (int i = 0; i < 3; i++) Assert.That(state.QuickWork(SandboxJob.Sorting, out _), Is.True);
            for (int i = 0; i < 7; i++)
            {
                Assert.That(state.BuyEnergyDrink(out _), Is.True);
                Assert.That(state.DrinkEnergyDrink(out _), Is.True);
                Assert.That(state.TickEnergy(30f), Is.True);
            }

            Assert.That(state.Fatigue, Is.EqualTo(60));
            Assert.That(state.BuyEnergyDrink(out _), Is.True);
            Assert.That(state.DrinkEnergyDrink(out _), Is.True);
            Assert.That(state.TickEnergy(30f), Is.True);
            Assert.That(state.Fatigue, Is.EqualTo(65));
            Assert.That(state.Energy, Is.GreaterThan(0));
            Assert.That(state.BuyEnergyDrink(out _), Is.True);
            Assert.That(state.DrinkEnergyDrink(out var reason), Is.False);
            Assert.That(reason, Does.Contain("выше 60"));
            Assert.That(state.HasEnergyDrink, Is.True);

            Assert.That(state.Rest(out _), Is.True);
            for (int i = 0; i < 5; i++)
            {
                if (!state.HasEnergyDrink) Assert.That(state.BuyEnergyDrink(out _), Is.True);
                Assert.That(state.DrinkEnergyDrink(out _), Is.True);
                Assert.That(state.TickEnergy(30f), Is.True);
            }
            Assert.That(state.Fatigue, Is.EqualTo(65));
            Assert.That(state.QuickWork(SandboxJob.Sorting, out _), Is.True);
            Assert.That(state.Fatigue, Is.EqualTo(70));
        }

        [Test] public void ExpiredBonusCannotPayForAnOpenOrder()
        {
            var state = new SandboxState();
            Assert.That(state.BuyBook(out _), Is.True);
            Assert.That(state.ReadBook(out _), Is.True);
            for (int i = 0; i < 3; i++) Assert.That(state.Study(out _), Is.True);
            Assert.That(state.Energy, Is.EqualTo(5));
            Assert.That(state.BuyEnergyDrink(out _), Is.True);
            Assert.That(state.DrinkEnergyDrink(out _), Is.True);
            Assert.That(state.BeginWork(SandboxJob.Sorting, 7, out _), Is.True);
            var order = state.ActiveOrder;
            Assert.That(state.TickEnergy(30f), Is.True);
            Assert.That(state.Sort(order.Id, order.Target(0), out _), Is.True);
            Assert.That(state.Sort(order.Id, order.Target(1), out _), Is.True);
            Assert.That(state.Sort(order.Id, order.Target(2), out _), Is.False);
            Assert.That(state.ActiveOrder, Is.Null);
            Assert.That(state.Money, Is.EqualTo(35));
            Assert.That(state.Energy, Is.Zero);
            Assert.That(state.Fatigue, Is.EqualTo(30));
            Assert.That(state.SortingCompleted, Is.Zero);
        }
    }
}
