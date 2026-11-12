using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MezhduParami.Tests
{
    public sealed class PrototypeStoryTests
    {
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        private SashaQuestDefinition definition;
        private ActivityDefinition rest;

        [SetUp] public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<SashaQuestDefinition>();
            assets.Add(definition);
            definition.Consultation = Activity("consult", -15, 0);
            definition.Consultation.ReadinessDelta = 2;
            definition.Help = Activity("help", -20, 0);
            definition.SpecialShift = Activity("shift", -20, 80, true);
            rest = Activity("rest", 40, 0);
        }

        [TearDown] public void TearDown()
        {
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        private ActivityDefinition Activity(string id, int energy, int money, bool work = false)
        {
            var activity = ScriptableObject.CreateInstance<ActivityDefinition>();
            assets.Add(activity);
            activity.Id = id;
            activity.EnergyDelta = energy;
            activity.MoneyDelta = money;
            activity.IsWork = work;
            return activity;
        }

        private void EndDay(PrototypeDay day)
        {
            while (!day.IsComplete) Assert.That(day.TryPerform(rest, out _), Is.True);
        }

        private void ReachDeadline(PrototypeDay day)
        {
            while (day.DayNumber < definition.DueDay)
            {
                EndDay(day);
                Assert.That(day.TryStartNextDay(true, out _), Is.True);
            }
            while (day.Period < definition.DuePeriod) Assert.That(day.TryPerform(rest, out _), Is.True);
        }

        [Test] public void NightChargesOnceAndPreservesProgressWhileResettingDailyWork()
        {
            var day = new PrototypeDay();
            var work = Activity("work", -15, 25, true);
            work.IntelligenceExperience = 1;
            day.TryPerform(work, out _);
            EndDay(day);
            Assert.That(day.TryStartNextDay(true, out _), Is.True);
            Assert.That(day.TryStartNextDay(true, out _), Is.False);
            Assert.That(day.DayNumber, Is.EqualTo(2));
            Assert.That(day.Money, Is.EqualTo(137));
            Assert.That(day.Energy, Is.EqualTo(100));
            Assert.That(day.RecoveryLimit, Is.EqualTo(100));
            Assert.That(day.IntelligenceExperience, Is.EqualTo(1));
            Assert.That(day.TryPerform(work, out _), Is.True, "Daily shift limit resets next morning");
        }

        [Test] public void UnaffordableFoodDoesNotMutateAndSkippingFoodIsAnExit()
        {
            var day = new PrototypeDay();
            day.TryPerform(Activity("spend", -80, -120), out _);
            var idle = Activity("idle", 0, 0);
            day.TryPerform(idle, out _);
            day.TryPerform(idle, out _);
            Assert.That(day.TryStartNextDay(true, out _), Is.False);
            Assert.That(day.DayNumber, Is.EqualTo(1));
            Assert.That(day.RecoveryLimit, Is.EqualTo(100));
            Assert.That(day.TryStartNextDay(false, out _), Is.True);
            Assert.That(day.RecoveryLimit, Is.EqualTo(100));
            Assert.That(day.Energy, Is.EqualTo(30));
            Assert.That(day.Money, Is.Zero);
        }

        [Test] public void FinalDayCannotAdvanceOrPayForAnotherNight()
        {
            var day = new PrototypeDay(1);
            EndDay(day);
            Assert.That(day.IsFinished, Is.True);
            Assert.That(day.TryStartNextDay(true, out _), Is.False);
            Assert.That(day.TryStartNextDay(false, out _), Is.False);
            Assert.That(day.Money, Is.EqualTo(120));
            Assert.That(day.TrySkipSleep(out _), Is.False);
            Assert.That(day.RecoveryLimit, Is.EqualTo(100));
        }

        [Test] public void SkippingSleepKeepsCurrentEnergyButCapsRecoveryUntilTheNextSleep()
        {
            var day = new PrototypeDay();
            EndDay(day);
            Assert.That(day.TrySkipSleep(out _), Is.True);
            Assert.That(day.Energy, Is.EqualTo(100));
            Assert.That(day.RecoveryLimit, Is.EqualTo(75));
            Assert.That(day.TrySkipSleep(out _), Is.False);
            Assert.That(day.TryPerform(Activity("study", -40, 0), out _), Is.True);
            Assert.That(day.TryPerform(rest, out _), Is.True);
            Assert.That(day.Energy, Is.EqualTo(75));
            EndDay(day);
            Assert.That(day.TryStartNextDay(false, out _), Is.True);
            Assert.That(day.RecoveryLimit, Is.EqualTo(100));
            Assert.That(day.Energy, Is.EqualTo(100));
            Assert.That(day.Money, Is.EqualTo(120));
        }

        [Test] public void OfferPreviewAndDeclineAreFreeAndCannotBeReversedForRewards()
        {
            var day = new PrototypeDay();
            var promise = new SashaPromise(definition);
            Assert.That(promise.CanAccept(day, out _), Is.True);
            Assert.That(day.Period, Is.Zero);
            Assert.That(day.Readiness, Is.Zero);
            Assert.That(promise.TryDecline(day), Is.True);
            Assert.That(promise.TryAccept(day, out _), Is.False);
            Assert.That(promise.Trust, Is.Zero);
            Assert.That(day.Energy, Is.EqualTo(80));
            Assert.That(day.Period, Is.Zero);
        }

        [Test] public void ConsultationCreatesOnePromiseAndOneReward()
        {
            var day = new PrototypeDay();
            var promise = new SashaPromise(definition);
            Assert.That(promise.TryAccept(day, out _), Is.True);
            Assert.That(promise.TryAccept(day, out _), Is.False);
            Assert.That(day.Readiness, Is.EqualTo(2));
            Assert.That(day.Energy, Is.EqualTo(65));
            Assert.That(day.Period, Is.EqualTo(1));
            Assert.That(promise.State, Is.EqualTo(PromiseState.Promised));
            Assert.That(promise.TryHelp(day, out _), Is.False, "Help cannot be performed before its scheduled period");
        }

        [Test] public void MissingOfferDoesNotCreateAnObligationOrLateReward()
        {
            var day = new PrototypeDay();
            var promise = new SashaPromise(definition);
            ReachDeadline(day);
            Assert.That(promise.TryAccept(day, out _), Is.False);
            day.TryPerform(rest, out _);
            promise.ResolveDeadline(day);
            Assert.That(promise.State, Is.EqualTo(PromiseState.NotAccepted));
            Assert.That(promise.Trust, Is.Zero);
            Assert.That(day.Readiness, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ExclusiveDecisionProducesOneOutcomeAndNoRepeatRewards(bool help)
        {
            var day = new PrototypeDay();
            var promise = new SashaPromise(definition);
            promise.TryAccept(day, out _);
            ReachDeadline(day);
            int money = day.Money;
            Assert.That(help ? promise.TryHelp(day, out _) : promise.TryTakeShift(day, out _), Is.True);
            promise.ResolveDeadline(day);
            promise.ResolveDeadline(day);
            Assert.That(promise.Trust, Is.EqualTo(help ? 1 : -1));
            Assert.That(promise.TryHelp(day, out _), Is.False);
            Assert.That(promise.TryTakeShift(day, out _), Is.False);
            Assert.That(day.Money, Is.EqualTo(money + (help ? 0 : 80)));
            Assert.That(day.Period, Is.EqualTo(3));
        }

        [Test] public void OrdinaryActivityAtDeadlineBreaksPromiseOnce()
        {
            var day = new PrototypeDay();
            var promise = new SashaPromise(definition);
            promise.TryAccept(day, out _);
            ReachDeadline(day);
            day.TryPerform(rest, out _);
            promise.ResolveDeadline(day);
            promise.ResolveDeadline(day);
            Assert.That(promise.State, Is.EqualTo(PromiseState.Broken));
            Assert.That(promise.Trust, Is.EqualTo(-1));
            Assert.That(promise.SpecialShiftTaken, Is.False);
        }

        [Test] public void EarlierPaidShiftPreventsSpecialShiftWithoutBreakingPromise()
        {
            var day = new PrototypeDay();
            var promise = new SashaPromise(definition);
            promise.TryAccept(day, out _);
            EndDay(day);
            day.TryStartNextDay(true, out _);
            EndDay(day);
            day.TryStartNextDay(true, out _);
            day.TryPerform(Activity("work", -15, 25, true), out _);
            day.TryPerform(rest, out _);
            Assert.That(promise.TryTakeShift(day, out _), Is.False);
            Assert.That(day.Period, Is.EqualTo(2));
            Assert.That(promise.State, Is.EqualTo(PromiseState.Promised));
            Assert.That(promise.TryHelp(day, out _), Is.True);
        }

        [Test] public void InsufficientEnergyDoesNotGrantConsultationOrPromise()
        {
            var day = new PrototypeDay();
            var promise = new SashaPromise(definition);
            day.TryPerform(Activity("drain", -80, 0), out _);
            Assert.That(promise.TryAccept(day, out _), Is.False);
            Assert.That(promise.State, Is.EqualTo(PromiseState.NotAccepted));
            Assert.That(day.Period, Is.EqualTo(1));
            Assert.That(day.Readiness, Is.Zero);
        }
    }
}
