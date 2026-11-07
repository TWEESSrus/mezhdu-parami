namespace MezhduParami
{
    public enum PromiseState { NotAccepted, Declined, Promised, Fulfilled, Broken }

    // One bounded chain, not a general quest scripting engine. Definitions are
    // shared assets; mutable progress belongs only to the current run.
    public sealed class SashaPromise
    {
        public SashaQuestDefinition Definition { get; }
        public PromiseState State { get; private set; }
        public bool SpecialShiftTaken { get; private set; }
        public int Trust => State == PromiseState.Fulfilled ? 1 : State == PromiseState.Broken ? -1 : 0;

        public SashaPromise(SashaQuestDefinition definition)
        {
            Definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.Id) || definition.Consultation == null
                || definition.Help == null || definition.SpecialShift == null
                || definition.DueDay <= definition.OfferDay || definition.DuePeriod < 0 || definition.DuePeriod > 2)
                throw new System.ArgumentException("Promise definition is incomplete.", nameof(definition));
        }

        public bool IsDecisionTime(PrototypeDay day) => !day.IsComplete
            && day.DayNumber == Definition.DueDay && day.Period == Definition.DuePeriod;

        public bool CanAccept(PrototypeDay day, out string reason)
        {
            if (State != PromiseState.NotAccepted) reason = "Ответ Саше уже дан.";
            else if (day.DayNumber != Definition.OfferDay) reason = "Предложение консультации было в первый день.";
            else return day.CanPerform(Definition.Consultation, out reason);
            return false;
        }

        public bool TryAccept(PrototypeDay day, out string reason)
        {
            if (!CanAccept(day, out reason)) return false;
            if (!day.TryPerform(Definition.Consultation, out reason)) return false;
            State = PromiseState.Promised;
            return true;
        }

        public bool TryDecline(PrototypeDay day)
        {
            if (State != PromiseState.NotAccepted || day.DayNumber != Definition.OfferDay || day.IsComplete) return false;
            State = PromiseState.Declined;
            return true;
        }

        public bool CanHelp(PrototypeDay day, out string reason)
        {
            if (State != PromiseState.Promised) reason = "Сейчас нет действующего обещания Саше.";
            else if (!IsDecisionTime(day)) reason = "Помощь запланирована: " + Definition.DeadlineLabel + ".";
            else return day.CanPerform(Definition.Help, out reason);
            return false;
        }

        public bool TryHelp(PrototypeDay day, out string reason)
        {
            if (!CanHelp(day, out reason)) return false;
            if (!day.TryPerform(Definition.Help, out reason)) return false;
            State = PromiseState.Fulfilled;
            return true;
        }

        public bool CanTakeShift(PrototypeDay day, out string reason)
        {
            if (SpecialShiftTaken) reason = "Разовая смена уже выполнена.";
            else if (!IsDecisionTime(day)) reason = "Разовая смена доступна: " + Definition.DeadlineLabel + ".";
            else return day.CanPerform(Definition.SpecialShift, out reason);
            return false;
        }

        public bool TryTakeShift(PrototypeDay day, out string reason)
        {
            if (!CanTakeShift(day, out reason)) return false;
            if (!day.TryPerform(Definition.SpecialShift, out reason)) return false;
            SpecialShiftTaken = true;
            ResolveDeadline(day);
            return true;
        }

        public void ResolveDeadline(PrototypeDay day)
        {
            if (State == PromiseState.Promised && (day.DayNumber > Definition.DueDay
                || (day.DayNumber == Definition.DueDay && day.Period > Definition.DuePeriod)))
                State = PromiseState.Broken;
        }

        public string Journal(PrototypeDay day)
        {
            switch (State)
            {
                case PromiseState.Promised:
                    return $"Обещание Саше: {Definition.DeadlineLabel}. Помощь: 1 период, энергия {Definition.Help.EnergyDelta}. Доверие +1. В тот же вечер разовая смена: +{Definition.SpecialShift.MoneyDelta}. Выполнить оба дела нельзя.";
                case PromiseState.Fulfilled: return "Обещание выполнено. Доверие Саши +1. " + Definition.FulfilledText;
                case PromiseState.Broken: return "Обещание нарушено. Доверие Саши -1. " + Definition.BrokenText;
                case PromiseState.Declined: return Definition.DeclinedText + " Доверие не изменилось.";
                default: return day.DayNumber == Definition.OfferDay
                    ? "Саша предлагает консультацию в первый день. Поговори с ним до конца вечера. Принять или отказаться можно после просмотра условий."
                    : "Предложение консультации пропущено. Обещаний нет, доверие не изменилось.";
            }
        }
    }
}
