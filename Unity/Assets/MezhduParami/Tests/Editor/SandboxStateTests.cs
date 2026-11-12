using NUnit.Framework;

namespace MezhduParami.Tests
{
    public sealed class SandboxStateTests
    {
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

        [Test] public void EnergyDrinkCrossesTheLimitDecaysAndSpendingItAddsFatigue()
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
            Assert.That(state.Fatigue, Is.EqualTo(45));
            Assert.That(state.TickEnergy(9f), Is.True);
            Assert.That(state.BonusEnergy, Is.Zero);
            Assert.That(state.Sleep(out _), Is.True);
            Assert.That(state.Fatigue, Is.Zero);
            Assert.That(state.Energy, Is.EqualTo(100));
        }

        [Test] public void ExpiredBonusCannotPayForAnOpenOrder()
        {
            var state = new SandboxState();
            for (int i = 0; i < 4; i++) Assert.That(state.Study(out _), Is.True);
            Assert.That(state.BuyEnergyDrink(out _), Is.True);
            Assert.That(state.DrinkEnergyDrink(out _), Is.True);
            Assert.That(state.BeginWork(SandboxJob.Sorting, 7, out _), Is.True);
            var order = state.ActiveOrder;
            Assert.That(state.TickEnergy(30f), Is.True);
            Assert.That(state.Sort(order.Id, order.Target(0), out _), Is.True);
            Assert.That(state.Sort(order.Id, order.Target(1), out _), Is.True);
            Assert.That(state.Sort(order.Id, order.Target(2), out _), Is.False);
            Assert.That(state.ActiveOrder, Is.Null);
            Assert.That(state.Money, Is.EqualTo(95));
            Assert.That(state.Energy, Is.Zero);
            Assert.That(state.SortingCompleted, Is.Zero);
        }
    }
}
