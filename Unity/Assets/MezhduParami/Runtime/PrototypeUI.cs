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
        public TMP_Text MoneyValue;
        public TMP_Text EnergyValue;
        public TMP_Text StudyValue;
        public EnergyMeter EnergyBar;
        public CanvasGroup HudGroup;
        public CanvasGroup FooterGroup;
        public CanvasGroup FeedbackGroup;
        public GameObject ModalBackdrop;
        public GameObject PromptCard;
        public GameObject SelectionPanel;
        public GameObject DialogPanel;
        public TMP_Text DialogTitle;
        public TMP_Text DialogBody;
        public TMP_Text PrimaryLabel;
        public UnityEngine.UI.Button PrimaryButton;
        public UnityEngine.UI.Button CancelButton;
        public UnityEngine.UI.Button SleepOption;
        public PlayerMotor Player;
        public SashaQuestDefinition QuestDefinition;

        private enum Screen { Selection, Explore, Activity, Bed, Conversation, Pause, Summary, QuestOffer, QuestHelp, SpecialShift, Night, Journal }
        private Screen screen = Screen.Selection;
        private ActivityDefinition pendingActivity;
        private string student = "Студент";
        private int lastInputFrame = -1;
        private string observedFeedback;
        private float feedbackRemaining;
        public PrototypeDay Day { get; private set; } = new PrototypeDay();
        public SashaPromise Promise { get; private set; }
        public bool InputBlocked => screen != Screen.Explore;

        private void Start()
        {
            Promise = new SashaPromise(QuestDefinition);
            Feedback.text = "Первый курс начинается здесь. Выбери героя.";
            SelectionPanel.SetActive(true);
            DialogPanel.SetActive(false);
            RefreshStatus();
        }

        private void LateUpdate()
        {
            if (Feedback != null && observedFeedback != Feedback.text)
            {
                observedFeedback = Feedback.text;
                feedbackRemaining = 10f;
            }
            // Keep notifications long enough to read after a dialog closes.
            if (screen == Screen.Explore)
                feedbackRemaining = Mathf.Max(0f, feedbackRemaining - Time.unscaledDeltaTime);
            SyncPresentation();
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
            Feedback.text = "Начни с Саши. Стол — учёба, доска — работа, кровать — отдых и сон. J — журнал.";
            RefreshStatus();
        }

        public void SetPrompt(string text)
        {
            Prompt.text = text;
            SyncPresentation();
        }

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
            ShowActivity(pendingActivity.Id == "rest" ? Screen.Bed : Screen.Activity,
                pendingActivity, canPerform, failure, consequence);
        }

        public void OnPrimary()
        {
            if (lastInputFrame == Time.frameCount || !PrimaryButton.interactable) return;
            lastInputFrame = Time.frameCount;
            if (screen == Screen.Activity || screen == Screen.Bed)
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
            else if (screen == Screen.Night) FinishNight();
            else if (screen == Screen.Summary)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            else if (screen == Screen.Pause || screen == Screen.Conversation || screen == Screen.Journal) CloseDialog();
        }

        public void OnSecondary()
        {
            if (lastInputFrame == Time.frameCount) return;
            lastInputFrame = Time.frameCount;
            if (screen == Screen.QuestOffer)
            {
                Promise.TryDecline(Day);
                CloseDialog();
                Feedback.text = "Консультация отклонена. Обязательств нет, доверие не изменилось. Учебный стол доступен.";
                RefreshStatus();
            }
            else CloseDialog();
        }

        public void OnSleepOption()
        {
            if ((screen != Screen.Night && screen != Screen.Bed) || lastInputFrame == Time.frameCount) return;
            lastInputFrame = Time.frameCount;
            FinishNight(skipSleep: screen == Screen.Night);
        }

        private void CompleteAction(bool succeeded, string reason, string message)
        {
            CloseDialog();
            if (!succeeded) { Feedback.text = reason; return; }
            Promise.ResolveDeadline(Day);
            Feedback.text = message;
            RefreshStatus();
            if (Day.IsFinished) ShowSummary();
            else if (Day.IsComplete) OpenNight();
        }

        private void ShowSummary() => ShowDialog(Screen.Summary, "Первые дни позади",
            $"Три дня в общаге завершены.\nДеньги {Day.Money}  •  Энергия {Day.Energy}/100\nИнтеллект: {Day.IntelligenceLevel}  •  Готовность: {Day.Readiness}/6\n\n{Promise.Journal(Day)}\n\nЭто итог прототипа. История первого курса ещё в разработке.",
            "Начать заново", false);

        private void OpenNight()
        {
            ShowDialog(Screen.Night, $"День {Day.DayNumber} завершён",
                "После сна начнётся новый день с полной энергией и двумя отдыхами.\n\nЕсли пропустить сон, останется текущая энергия, а недосып займёт часть шкалы до следующего сна.",
                "Спать до утра", false);
        }

        private void FinishNight(bool skipSleep = false)
        {
            bool advanced = skipSleep ? Day.TrySkipSleep(out var reason) : Day.TrySleep(out reason);
            if (!advanced) { Feedback.text = reason; return; }
            pendingActivity = null;
            Promise.ResolveDeadline(Day);
            if (Day.IsFinished)
            {
                RefreshStatus();
                ShowSummary();
                return;
            }
            DialogPanel.SetActive(false);
            screen = Screen.Explore;
            Feedback.text = $"День {Day.DayNumber}. " + (skipSleep ? $"Недосып. Предел восстановления {Day.RecoveryLimit}." : "Энергия 100/100. Отдыхи обновлены.")
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
                $"{student} • День {Day.DayNumber} из {Day.TotalDays} • {Day.PeriodLabel}\nИнтеллект: {Day.IntelligenceLevel} • Опыт: {Day.IntelligenceExperience}\nГотовность к учёбе: {Day.Readiness}/6. Экзаменов в этом срезе нет.\n\n"
                + Promise.Journal(Day)
                + $"\n\nДоверие Саши: {Signed(Promise.Trust)}.\nРазовая смена: {QuestDefinition.DeadlineLabel}, +{QuestDefinition.SpecialShift.MoneyDelta}; нужна энергия 20 и свободная смена за этот день.",
                "Закрыть", false);
        }

        private void ShowActivity(Screen next, ActivityDefinition activity, bool available, string reason, string consequence)
        {
            ShowDialog(next, activity.Title, activity.Description + "\n\n" + Effects(activity) + consequence
                + (available ? "" : "\n\n" + reason), next == Screen.Bed ? "Отдохнуть" : "Подтвердить", true);
            PrimaryButton.interactable = available;
        }

        private string Effects(ActivityDefinition activity)
        {
            string text = $"1 период  •  Энергия {Signed(activity.EnergyDelta)}  •  Деньги {Signed(activity.MoneyDelta)}";
            if (activity.IntelligenceExperience > 0) text += $"\nОпыт интеллекта +{activity.IntelligenceExperience}";
            if (activity.ReadinessDelta != 0) text += $"\nГотовность к учёбе {Signed(activity.ReadinessDelta)} (до 6)";
            if (activity.Id == "rest")
                text += $"\nОсталось отдыхов {Day.RestsRemaining}/2. Серый участок шкалы — усталость.\n\nСон восстановит всю энергию. Оставшиеся периоды дня будут пропущены.";
            return text;
        }

        public void CloseDialog()
        {
            if (screen == Screen.Selection || screen == Screen.Summary || screen == Screen.Night) return;
            pendingActivity = null;
            DialogPanel.SetActive(false);
            screen = Screen.Explore;
            SyncPresentation();
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
            if (SleepOption != null)
            {
                SleepOption.gameObject.SetActive(next == Screen.Bed || next == Screen.Night);
                SleepOption.GetComponentInChildren<TMP_Text>().text = next == Screen.Night ? "Пропустить сон" : "Спать до утра";
            }
            DialogPanel.SetActive(true);
            Prompt.text = "";
            SyncPresentation();
        }

        private void RefreshStatus()
        {
            Header.text = $"День {Day.DayNumber}";
            Status.text = Day.IsFinished ? "Итог первых дней" : Day.IsComplete ? "Ночь • отдых" : "Первый курс • " + Day.PeriodLabel;
            if (MoneyValue != null) MoneyValue.text = Day.Money.ToString();
            if (EnergyValue != null) EnergyValue.text = $"{Day.Energy}/100";
            if (StudyValue != null) StudyValue.text = $"{Day.Readiness}/6";
            if (EnergyBar != null) EnergyBar.Draw(Day.Energy, Day.Fatigue);
            switch (Promise.State)
            {
                case PromiseState.Promised:
                    QuestStatus.text = "Помочь Саше • " + QuestDefinition.DeadlineLabel;
                    break;
                case PromiseState.Fulfilled: QuestStatus.text = "Обещание выполнено"; break;
                case PromiseState.Broken: QuestStatus.text = "Обещание нарушено"; break;
                case PromiseState.Declined: QuestStatus.text = "Без обещаний"; break;
                default:
                    QuestStatus.text = Day.DayNumber == QuestDefinition.OfferDay ? "Знакомство с Сашей" : "Без обещаний";
                    break;
            }
            SyncPresentation();
        }

        private void SyncPresentation()
        {
            bool explore = screen == Screen.Explore;
            SetGroup(HudGroup, screen != Screen.Selection, explore);
            SetGroup(FooterGroup, explore, explore);
            SetGroup(FeedbackGroup, explore && feedbackRemaining > 0f && !string.IsNullOrWhiteSpace(Feedback.text), false);
            if (ModalBackdrop != null && ModalBackdrop.activeSelf != InputBlocked)
                ModalBackdrop.SetActive(InputBlocked);
            bool showPrompt = explore && !string.IsNullOrWhiteSpace(Prompt.text);
            if (PromptCard != null && PromptCard.activeSelf != showPrompt) PromptCard.SetActive(showPrompt);
        }

        private static void SetGroup(CanvasGroup group, bool visible, bool interactive)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible && interactive;
            group.blocksRaycasts = visible && interactive;
        }

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
    }
}
