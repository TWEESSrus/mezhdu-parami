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
        public GameObject SelectionPanel;
        public GameObject DialogPanel;
        public TMP_Text DialogTitle;
        public TMP_Text DialogBody;
        public TMP_Text PrimaryLabel;
        public UnityEngine.UI.Button PrimaryButton;
        public UnityEngine.UI.Button CancelButton;
        public PlayerMotor Player;

        private enum Screen { Selection, Explore, Activity, Conversation, Pause, Summary }
        private Screen screen = Screen.Selection;
        private ActivityDefinition pendingActivity;
        private string student = "Студент";
        public PrototypeDay Day { get; private set; } = new PrototypeDay();
        public bool InputBlocked => screen != Screen.Explore;

        private void Start()
        {
            Header.text = "МЕЖДУ ПАРАМИ    /    Первый день в общаге";
            Feedback.text = "Технический прототип • Временная графика • Один игровой день";
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
                else if (screen == Screen.Activity || screen == Screen.Conversation || screen == Screen.Pause)
                    CloseDialog();
            }
            else if (keyboard.enterKey.wasPressedThisFrame && screen != Screen.Selection
                && screen != Screen.Explore && PrimaryButton.interactable)
                OnPrimary();
        }

        public void SelectStudent(bool female)
        {
            if (screen != Screen.Selection) return;
            student = female ? "Студентка" : "Студент";
            Player.Shirt.color = female ? new Color32(166, 92, 185, 255) : new Color32(49, 129, 155, 255);
            SelectionPanel.SetActive(false);
            screen = Screen.Explore;
            Feedback.text = "Осмотрись: стол — учёба, доска — работа, кровать — отдых. Разговоры бесплатны.";
            RefreshStatus();
        }

        public void SetPrompt(string text) => Prompt.text = text;

        public void OpenInteraction(WorldInteraction target)
        {
            if (screen != Screen.Explore || target == null) return;
            pendingActivity = target.Activity;
            if (pendingActivity == null)
            {
                ShowDialog(Screen.Conversation, target.DisplayName, target.Conversation, "Понятно", false);
                return;
            }
            bool available = Day.CanPerform(pendingActivity, out var reason);
            string effects = $"1 период  •  Энергия {Signed(pendingActivity.EnergyDelta)}  •  Деньги {Signed(pendingActivity.MoneyDelta)}";
            if (pendingActivity.IntelligenceExperience > 0)
                effects += $"\nОпыт интеллекта +{pendingActivity.IntelligenceExperience}";
            ShowDialog(Screen.Activity, pendingActivity.Title,
                pendingActivity.Description + "\n\n" + effects + (available ? "" : "\n\n" + reason),
                "Подтвердить", true);
            PrimaryButton.interactable = available;
        }

        public void OnPrimary()
        {
            if (screen == Screen.Activity)
            {
                // Clear the pending confirmation before applying the result. A
                // second click cannot execute the old confirmation a second time.
                var activity = pendingActivity;
                pendingActivity = null;
                CloseDialog();
                if (!Day.TryPerform(activity, out var reason)) Feedback.text = reason;
                else Feedback.text = activity.Title + " — выполнено. " + Day.PeriodLabel + ".";
                RefreshStatus();
                if (Day.IsComplete)
                    ShowDialog(Screen.Summary, "Первый день завершён",
                        $"Использованы все три периода.\nДеньги: {Day.Money}  •  Энергия: {Day.Energy}\nОпыт интеллекта: {Day.IntelligenceExperience}\n\nЭто итог тестового дня. Полный первый курс, квесты и сохранения появятся на следующих этапах.",
                        "Начать заново", false);
            }
            else if (screen == Screen.Summary)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            else if (screen == Screen.Pause || screen == Screen.Conversation) CloseDialog();
        }

        public void CloseDialog()
        {
            if (screen == Screen.Selection || screen == Screen.Summary) return;
            pendingActivity = null;
            DialogPanel.SetActive(false);
            screen = Screen.Explore;
        }

        private void OpenPause() => ShowDialog(Screen.Pause, "Пауза",
            "WASD или стрелки — движение\nE — взаимодействие\nEnter — подтвердить\nEsc — отмена или пауза\n\nХодьба и разговоры не расходуют игровые периоды. Сохранение пока не реализовано.",
            "Продолжить", false);

        private void ShowDialog(Screen next, string title, string body, string primary, bool cancel)
        {
            screen = next;
            DialogTitle.text = title;
            DialogBody.text = body;
            PrimaryLabel.text = primary;
            PrimaryButton.interactable = true;
            CancelButton.gameObject.SetActive(cancel);
            DialogPanel.SetActive(true);
            Prompt.text = "";
        }

        private void RefreshStatus() => Status.text =
            $"{student}  •  День 1 / {Day.PeriodLabel}    |    Деньги {Day.Money}    Энергия {Day.Energy}/100    Здоровье 80/100    Интеллект {Day.IntelligenceLevel} ({Day.IntelligenceExperience} опыта)";

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
    }
}
