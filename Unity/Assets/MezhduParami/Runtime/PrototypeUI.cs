using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MezhduParami
{
    public sealed class PrototypeUI : MonoBehaviour
    {
        public TMP_Text Header;
        public TMP_Text Status;
        public TMP_Text Prompt;
        public TMP_Text Feedback;
        public TMP_Text QuestStatus;
        public GameObject SelectionPanel;
        public GameObject DialogPanel;
        public TMP_Text DialogTitle;
        public TMP_Text DialogBody;
        public TMP_Text PrimaryLabel;
        public UnityEngine.UI.Button PrimaryButton;
        public UnityEngine.UI.Button CancelButton;
        public PlayerMotor Player;
        public SashaQuestDefinition QuestDefinition;

        private enum Screen { Selection, Explore, Activity, Conversation, Pause, Summary, QuestOffer, QuestHelp, SpecialShift, Night, Journal }
        private Screen screen = Screen.Selection;
        private ActivityDefinition pendingActivity;
        private string student = "Студент";
        private int lastInputFrame = -1;
        public PrototypeDay Day { get; private set; } = new PrototypeDay();
        public SashaPromise Promise { get; private set; }
        public bool InputBlocked => screen != Screen.Explore;

        private void Start()
        {
            Promise = new SashaPromise(QuestDefinition);
            Header.text = "МЕЖДУ ПАРАМИ    /    Первые дни в общаге";
            Feedback.text = "Трёхдневный прототип • Временная графика • J — журнал и обещания";
            SelectionPanel.SetActive(true);
            DialogPanel.SetActive(false);
            RefreshStatus();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (screen == Screen.Explore) OpenPause();
                else CloseDialog();
            }
            else if (keyboard.enterKey.wasPressedThisFrame && screen != Screen.Selection
                && screen != Screen.Explore && PrimaryButton.interactable)
                OnPrimary();
            else if (keyboard.jKey.wasPressedThisFrame && screen == Screen.Explore) OpenJournal();
        }

        public void SelectStudent(bool female)
        {
            if (screen != Screen.Selection) return;
            student = female ? "Студентка" : "Студент";
            Player.Shirt.color = female ? new Color32(166, 92, 185, 255) : new Color32(49, 129, 155, 255);
            SelectionPanel.SetActive(false);
            screen = Screen.Explore;
            Feedback.text = "Начни с Саши. Стол — учёба, доска — работа, кровать — отдых. J — журнал.";
            RefreshStatus();
        }

        public void SetPrompt(string text) => Prompt.text = text;

        public void OpenInteraction(WorldInteraction target)
        {
            if (screen != Screen.Explore || target == null) return;
            if (target.Id == "npc.sasha") { OpenSasha(); return; }
            if (target.Id == "work.board" && Promise.IsDecisionTime(Day))
            {
                bool available = Promise.CanTakeShift(Day, out var reason);
                string warning = Promise.State == PromiseState.Promised
                    ? "\n\nЭто время обещано Саше. Выбор смены нарушит обещание: доверие -1."
                    : "\n\nВ этот период помощь Саше уже будет недоступна.";
                ShowActivity(Screen.SpecialShift, QuestDefinition.SpecialShift, available, reason, warning);
                return;
            }
            pendingActivity = target.Activity;
            if (pendingActivity == null)
            {
                ShowDialog(Screen.Conversation, target.DisplayName, target.Conversation, "Понятно", false);
                return;
            }
            bool canPerform = Day.CanPerform(pendingActivity, out var failure);
            string consequence = Promise.State == PromiseState.Promised && Promise.IsDecisionTime(Day)
                ? "\n\nЭто последний период срока обещания. Другое занятие нарушит его: доверие Саши -1."
                : "";
            ShowActivity(Screen.Activity, pendingActivity, canPerform, failure, consequence);
        }

        public void OnPrimary()
        {
            if (lastInputFrame == Time.frameCount || !PrimaryButton.interactable) return;
            lastInputFrame = Time.frameCount;
            if (screen == Screen.Activity)
            {
                var activity = pendingActivity;
                pendingActivity = null;
                CloseDialog();
                CompleteAction(Day.TryPerform(activity, out var reason), reason, activity == null ? "" : activity.Title + " — выполнено.");
            }
            else if (screen == Screen.QuestOffer)
            {
                CompleteAction(Promise.TryAccept(Day, out var reason), reason,
                    "Консультация пройдена: готовность +2. Обещание записано в журнал: " + QuestDefinition.DeadlineLabel + ".");
            }
            else if (screen == Screen.QuestHelp)
            {
                CompleteAction(Promise.TryHelp(Day, out var reason), reason, "Помощь оказана. Доверие Саши +1.");
            }
            else if (screen == Screen.SpecialShift)
            {
                CompleteAction(Promise.TryTakeShift(Day, out var reason), reason, $"Разовая смена выполнена. Деньги +{QuestDefinition.SpecialShift.MoneyDelta}.");
            }
            else if (screen == Screen.Night) FinishNight(true);
            else if (screen == Screen.Summary)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            else if (screen == Screen.Pause || screen == Screen.Conversation || screen == Screen.Journal) CloseDialog();
        }

        public void OnSecondary()
        {
            if (lastInputFrame == Time.frameCount) return;
            lastInputFrame = Time.frameCount;
            if (screen == Screen.Night) FinishNight(false);
            else if (screen == Screen.QuestOffer)
            {
                Promise.TryDecline(Day);
                CloseDialog();
                Feedback.text = "Консультация отклонена. Обязательств нет, доверие не изменилось. Учебный стол доступен.";
                RefreshStatus();
            }
            else CloseDialog();
        }

        private void CompleteAction(bool succeeded, string reason, string message)
        {
            CloseDialog();
            if (!succeeded) { Feedback.text = reason; return; }
            Promise.ResolveDeadline(Day);
            Feedback.text = message;
            RefreshStatus();
            if (Day.IsFinished)
                ShowDialog(Screen.Summary, "Первые дни позади",
                    $"Три дня в общаге завершены.\nДеньги: {Day.Money}  •  Энергия: {Day.Energy}  •  Здоровье: {Day.Health}\nИнтеллект: {Day.IntelligenceLevel}  •  Готовность: {Day.Readiness}/6\n\n{Promise.Journal(Day)}\n\nЭто итог прототипа. История первого курса ещё в разработке.",
                    "Начать заново", false);
            else if (Day.IsComplete) OpenNight();
        }

        private void OpenNight()
        {
            ShowDialog(Screen.Night, $"День {Day.DayNumber} завершён",
                $"Все три периода использованы.\n\nОбычная еда и сон: деньги -8, энергия +{(Day.Health < 40 ? 35 : 50)}, здоровье без изменений.\nБез еды: бесплатно, здоровье -8, энергия +30.\nЭнергия не превысит 100.\n\nУтром можно снова взять одну оплачиваемую смену.",
                "Еда и сон: 8", true, "Без еды: здоровье -8");
            PrimaryButton.interactable = Day.Money >= PrototypeDay.NormalFoodPrice;
            if (!PrimaryButton.interactable) DialogBody.text += "\nНа обычную еду не хватает денег.";
        }

        private void FinishNight(bool buyFood)
        {
            if (!Day.TryStartNextDay(buyFood, out var reason)) { Feedback.text = reason; return; }
            DialogPanel.SetActive(false);
            screen = Screen.Explore;
            Feedback.text = $"День {Day.DayNumber}. " + (buyFood ? "Еда оплачена, силы восстановлены." : "Без еды здоровье снизилось на 8.")
                + (Day.DayNumber == QuestDefinition.DueDay ? " Сегодня вечером: помощь Саше или разовая смена +80." : " J — журнал и сроки.");
            RefreshStatus();
        }

        private void OpenSasha()
        {
            if (Promise.State == PromiseState.NotAccepted && Day.DayNumber == QuestDefinition.OfferDay)
            {
                bool available = Promise.CanAccept(Day, out var reason);
                ShowDialog(Screen.QuestOffer, "Саша • взаимная помощь", "Привет! " + QuestDefinition.OfferText
                    + "\n\n" + Effects(QuestDefinition.Consultation)
                    + $"\nОбещание: {QuestDefinition.DeadlineLabel}, 1 период, энергия {QuestDefinition.Help.EnergyDelta}."
                    + $"\nВ тот же вечер: разовая смена +{QuestDefinition.SpecialShift.MoneyDelta}. Оба дела выполнить нельзя."
                    + (available ? "" : "\n" + reason), "Принять и учиться", true, "Отказаться");
                PrimaryButton.interactable = available;
            }
            else if (Promise.State == PromiseState.Promised && Promise.IsDecisionTime(Day))
            {
                bool available = Promise.CanHelp(Day, out var reason);
                ShowActivity(Screen.QuestHelp, QuestDefinition.Help, available, reason,
                    "\n\nДоверие Саши +1. Разовая смена в этот вечер будет упущена.");
            }
            else ShowDialog(Screen.Conversation, "Саша", Promise.Journal(Day), "Понятно", false);
        }

        public void OpenJournal()
        {
            if (screen != Screen.Explore) return;
            ShowDialog(Screen.Journal, "Журнал и известные сроки",
                $"День {Day.DayNumber} из {Day.TotalDays} • {Day.PeriodLabel}\nГотовность к учёбе: {Day.Readiness}/6. Экзаменов в этом срезе нет.\n\n"
                + Promise.Journal(Day)
                + $"\n\nДоверие Саши: {Signed(Promise.Trust)}.\nРазовая смена: {QuestDefinition.DeadlineLabel}, +{QuestDefinition.SpecialShift.MoneyDelta}; нужна энергия 20 и свободная смена за этот день.",
                "Закрыть", false);
        }

        private void ShowActivity(Screen next, ActivityDefinition activity, bool available, string reason, string consequence)
        {
            ShowDialog(next, activity.Title, activity.Description + "\n\n" + Effects(activity) + consequence
                + (available ? "" : "\n\n" + reason), "Подтвердить", true);
            PrimaryButton.interactable = available;
        }

        private static string Effects(ActivityDefinition activity)
        {
            string text = $"1 период  •  Энергия {Signed(activity.EnergyDelta)}  •  Деньги {Signed(activity.MoneyDelta)}";
            if (activity.IntelligenceExperience > 0) text += $"\nОпыт интеллекта +{activity.IntelligenceExperience}";
            if (activity.ReadinessDelta != 0) text += $"\nГотовность к учёбе {Signed(activity.ReadinessDelta)} (до 6)";
            return text;
        }

        public void CloseDialog()
        {
            if (screen == Screen.Selection || screen == Screen.Summary || screen == Screen.Night) return;
            pendingActivity = null;
            DialogPanel.SetActive(false);
            screen = Screen.Explore;
        }

        private void OpenPause() => ShowDialog(Screen.Pause, "Пауза",
            "WASD или стрелки — движение\nE — взаимодействие\nJ — журнал и сроки\nEnter — подтвердить\nEsc — отмена или пауза\n\nХодьба и просмотр диалогов бесплатны. Консультация и помощь расходуют период после подтверждения. Сохранения пока нет.",
            "Продолжить", false);

        private void ShowDialog(Screen next, string title, string body, string primary, bool cancel, string secondary = "Отмена / Esc")
        {
            screen = next;
            DialogTitle.text = title;
            DialogBody.text = body;
            PrimaryLabel.text = primary;
            PrimaryButton.interactable = true;
            CancelButton.GetComponentInChildren<TMP_Text>().text = secondary;
            CancelButton.gameObject.SetActive(cancel);
            DialogPanel.SetActive(true);
            Prompt.text = "";
        }

        private void RefreshStatus()
        {
            Status.text = $"{student}  •  День {Day.DayNumber}/{Day.TotalDays} / {Day.PeriodLabel}    |    Деньги {Day.Money}    Энергия {Day.Energy}/100    Здоровье {Day.Health}/100    Интеллект {Day.IntelligenceLevel} ({Day.IntelligenceExperience} опыта)";
            QuestStatus.text = Promise.State == PromiseState.Promised
                ? "Обещание Саше: " + QuestDefinition.DeadlineLabel + " • Помощь или смена +80 • J — журнал"
                : $"Доверие Саши: {Signed(Promise.Trust)} • Готовность {Day.Readiness}/6 • J — журнал и сроки";
        }

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
    }
}
