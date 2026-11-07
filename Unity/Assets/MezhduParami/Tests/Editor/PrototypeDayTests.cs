using NUnit.Framework;
using UnityEngine;

namespace MezhduParami.Tests
{
    public sealed class PrototypeDayTests
    {
        private ActivityDefinition activity;
        [SetUp] public void SetUp()
        {
            activity = ScriptableObject.CreateInstance<ActivityDefinition>();
            activity.Id = "test.activity";
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(activity);

        [Test] public void PreviewDoesNotSpendAnything()
        {
            var day = new PrototypeDay();
            activity.EnergyDelta = -20;
            activity.IntelligenceExperience = 1;
            Assert.That(day.CanPerform(activity, out _), Is.True);
            Assert.That(day.Period, Is.Zero);
            Assert.That(day.Energy, Is.EqualTo(80));
            Assert.That(day.IntelligenceExperience, Is.Zero);
        }

        [Test] public void FailedPaymentLeavesAllStateUntouched()
        {
            var day = new PrototypeDay();
            activity.MoneyDelta = -121;
            activity.EnergyDelta = -20;
            activity.IntelligenceExperience = 3;
            Assert.That(day.TryPerform(activity, out _), Is.False);
            Assert.That(day.Money, Is.EqualTo(120));
            Assert.That(day.Energy, Is.EqualTo(80));
            Assert.That(day.Period, Is.Zero);
            Assert.That(day.IntelligenceExperience, Is.Zero);
        }

        [Test] public void ExhaustedPlayerCanRestAndThenWork()
        {
            var day = new PrototypeDay();
            activity.EnergyDelta = -80;
            Assert.That(day.TryPerform(activity, out _), Is.True);
            activity.EnergyDelta = -15;
            Assert.That(day.TryPerform(activity, out _), Is.False);
            activity.EnergyDelta = 40;
            Assert.That(day.TryPerform(activity, out _), Is.True);
            activity.EnergyDelta = -15;
            activity.MoneyDelta = 25;
            Assert.That(day.TryPerform(activity, out _), Is.True);
            Assert.That(day.Energy, Is.EqualTo(25));
            Assert.That(day.Money, Is.EqualTo(145));
        }

        [Test] public void NoFourthPeriodOrRewardAfterDayEnds()
        {
            var day = new PrototypeDay();
            activity.MoneyDelta = 25;
            for (int i = 0; i < 3; i++) Assert.That(day.TryPerform(activity, out _), Is.True);
            Assert.That(day.IsComplete, Is.True);
            Assert.That(day.TryPerform(activity, out _), Is.False);
            Assert.That(day.Money, Is.EqualTo(195));
            Assert.That(day.Period, Is.EqualTo(3));
        }

        [Test] public void RestCannotExceedMaximumEnergy()
        {
            var day = new PrototypeDay();
            activity.EnergyDelta = 40;
            Assert.That(day.TryPerform(activity, out _), Is.True);
            Assert.That(day.Energy, Is.EqualTo(100));
        }

        [Test] public void ThreeStudyActionsReachFirstIntelligenceLevel()
        {
            var day = new PrototypeDay();
            activity.IntelligenceExperience = 1;
            activity.EnergyDelta = -20;
            for (int i = 0; i < 3; i++) day.TryPerform(activity, out _);
            Assert.That(day.IntelligenceLevel, Is.EqualTo(1));
            Assert.That(day.Energy, Is.EqualTo(20));
        }

        [Test] public void DiagonalInputDoesNotIncreaseSpeed()
        {
            Assert.That(PlayerMotor.NormalizeInput(Vector2.one).magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(PlayerMotor.NormalizeInput(Vector2.zero), Is.EqualTo(Vector2.zero));
        }

        [Test] public void OnlyOnePaidShiftPerDay()
        {
            var day = new PrototypeDay();
            activity.IsWork = true;
            activity.MoneyDelta = 25;
            activity.EnergyDelta = -15;
            Assert.That(day.TryPerform(activity, out _), Is.True);
            Assert.That(day.TryPerform(activity, out _), Is.False);
            Assert.That(day.Money, Is.EqualTo(145));
            Assert.That(day.Energy, Is.EqualTo(65));
            Assert.That(day.Period, Is.EqualTo(1));
        }
    }
}
