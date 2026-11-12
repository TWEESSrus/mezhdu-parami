using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MezhduParami
{
    public sealed class MechanicsSandboxUI : MonoBehaviour, IPlayerInputGate
    {
        public TMP_Text Resources, Prompt, Toast, Title, Body;
        public GameObject Window, Backdrop, PromptCard, ToastCard;
        public Button[] Choices;
        public Button CloseButton, InventoryButton, HelpButton;
        public SandboxInventoryView InventoryView;
        public TaskTrackerPanel Tasks;
        public EnergyMeter EnergyBar;
        public SandboxState State { get; private set; } = new SandboxState();
        public bool InputBlocked => Window != null && Window.activeSelf;
        private enum Screen { None, Shop, Inventory, Offer, Sort, Copy, Study, Rest, Help, Reset }
        private Screen screen;
        private SandboxJob job;
        private int lastClick = -1, lastClose = -1;
        private readonly int[] configuration = new int[3];
        private float toastUntil;
        private string hint = "";
        private static readonly string[] Categories = { "Учёба", "Спорт", "Быт" };
        private static readonly string[,] Parcels = { { "Конспекты", "Учебники", "Тетради" }, { "Мяч", "Спортивная форма", "Коврик для йоги" }, { "Кружка", "Настольная лампа", "Чайник" } };
        private static readonly string[][] CopySettings = { new[] { "A4", "A5", "A3" }, new[] { "1 копия", "2 копии", "3 копии" }, new[] { "Чёрно-белая", "Цветная", "Двусторонняя" } };
        private delegate bool Command(out string message);

        private void Awake()
        {
            for (int i = 0; i < Choices.Length; i++) { int index = i; Choices[i].onClick.AddListener(() => Choose(index)); }
            CloseButton.onClick.AddListener(Close);
            InventoryButton.onClick.AddListener(OpenInventory);
            InventoryView.UseButton.onClick.AddListener(UseInventoryItem);
            HelpButton.onClick.AddListener(OpenHelp);
        }
        private void Start()
        {
            Window.SetActive(false); Backdrop.SetActive(false);
            Notify("Осмотрись. E — действие рядом с объектом, T — задачи, I — вещи. Сроков и таймера здесь нет.");
            Refresh();
        }
        private void Update()
        {
            if (screen != Screen.Help && screen != Screen.Reset && State.TickEnergy(Time.unscaledDeltaTime))
            {
                if (State.BonusEnergy == 0) Notify("Действие энергетика закончилось. Наступил спад.");
                RefreshEnergy();
                if (screen == Screen.Inventory) InventoryView.Refresh();
                else if (screen == Screen.Offer || screen == Screen.Study) Render();
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { if (InputBlocked) Close(); else OpenHelp(); }
            else if (keyboard.iKey.wasPressedThisFrame) { if (screen == Screen.Inventory) Close(); else if (!InputBlocked) OpenInventory(); }
            if (ToastCard != null) ToastCard.SetActive(!InputBlocked && Time.unscaledTime < toastUntil);
        }
        public void SetPrompt(string text)
        {
            Prompt.text = text;
            PromptCard.SetActive(!InputBlocked && !string.IsNullOrEmpty(text));
        }
        public void OpenStation(SandboxStationKind kind)
        {
            if (InputBlocked || lastClose == Time.frameCount) return;
            hint = "";
            switch (kind)
            {
                case SandboxStationKind.Shop: Show(Screen.Shop); break;
                case SandboxStationKind.Sorting: job = SandboxJob.Sorting; Show(Screen.Offer); break;
                case SandboxStationKind.CopyCenter: job = SandboxJob.CopyCenter; Show(Screen.Offer); break;
                case SandboxStationKind.Study: Show(Screen.Study); break;
                case SandboxStationKind.Rest: Show(Screen.Rest); break;
            }
        }
        public void OpenInventory() { if (!InputBlocked) { hint = ""; Show(Screen.Inventory); } }
        public void OpenHelp() { if (!InputBlocked) Show(Screen.Help); }
        public void Close()
        {
            State.CancelWork(); screen = Screen.None; lastClose = Time.frameCount;
            Window.SetActive(false); Backdrop.SetActive(false); SetPrompt(""); Refresh();
        }
        public void Choose(int index)
        {
            if (!InputBlocked || index < 0 || index >= Choices.Length || !Choices[index].gameObject.activeSelf
                || !Choices[index].interactable || lastClick == Time.frameCount) return;
            lastClick = Time.frameCount;
            switch (screen)
            {
                case Screen.Shop:
                    if (index == 0) Execute(State.BuyBook);
                    else if (index == 1) Execute(State.BuySnack);
                    else Execute(State.BuyEnergyDrink);
                    break;
                case Screen.Study: Execute(State.Study); break;
                case Screen.Rest: Execute(index == 0 ? State.Rest : State.Sleep); break;
                case Screen.Offer:
                    if (index == 1) ExecuteQuick();
                    else if (State.BeginWork(job, Random.Range(0, int.MaxValue), out hint))
                    { System.Array.Clear(configuration, 0, 3); Show(job == SandboxJob.Sorting ? Screen.Sort : Screen.Copy); }
                    else Render();
                    break;
                case Screen.Sort:
                    State.Sort(State.ActiveOrder.Id, index, out hint);
                    if (State.ActiveOrder == null) Finish(hint); else Render();
                    break;
                case Screen.Copy:
                    if (index < 3) { configuration[index] = (configuration[index] + 1) % 3; hint = ""; Render(); }
                    else
                    {
                        State.Copy(State.ActiveOrder.Id, configuration[0], configuration[1], configuration[2], out hint);
                        if (State.ActiveOrder == null) Finish(hint); else Render();
                    }
                    break;
                case Screen.Help:
                    if (index == 0) Show(Screen.Reset);
                    else SceneManager.LoadScene("PrototypeDorm");
                    break;
                case Screen.Reset: SceneManager.LoadScene("MechanicsSandbox"); break;
            }
        }
        private void Execute(Command command) { command(out string message); Finish(message); }
        private void UseInventoryItem()
        {
            if (screen != Screen.Inventory || !InventoryView.UseButton.IsInteractable() || lastClick == Time.frameCount) return;
            lastClick = Time.frameCount;
            Notify(InventoryView.UseSelected());
            Refresh();
        }
        private void ExecuteQuick() { State.QuickWork(job, out string message); Finish(message); }
        private void Finish(string message)
        {
            Close();
            Notify(message);
        }
        private void Notify(string text) { Toast.text = text; toastUntil = Time.unscaledTime + 10f; }
        private void Show(Screen next)
        {
            screen = next; Window.SetActive(true); Backdrop.SetActive(true);
            SetPrompt(""); ToastCard.SetActive(false); Render();
        }
        private void Option(int index, string label, bool enabled = true)
        {
            Choices[index].gameObject.SetActive(true); Choices[index].interactable = enabled;
            Choices[index].GetComponentInChildren<TMP_Text>().text = label;
        }
        private void Render()
        {
            foreach (var choice in Choices) choice.gameObject.SetActive(false);
            Body.gameObject.SetActive(screen != Screen.Inventory);
            if (screen != Screen.Inventory) InventoryView.gameObject.SetActive(false);
            CloseButton.GetComponentInChildren<TMP_Text>().text = State.ActiveOrder != null ? "Отменить заказ / Esc — без затрат" : "Закрыть / Esc";
            switch (screen)
            {
                case Screen.Shop:
                    Title.text = "Лавка полезных вещей";
                    Body.text = $"Деньги {State.Money}.\nУчебник даёт +3 опыта интеллекта при чтении за 15 энергии.\nПерекус восстанавливает до 20 энергии. Энергетик даёт временный запас до 30.";
                    Option(0, State.BookOwned ? "Учебник уже куплен" : "Купить вводный учебник — 60", !State.BookOwned && State.Money >= 60);
                    Option(1, $"Купить перекус — 20 ({2 - State.SnacksBought} осталось)", State.SnacksBought < 2 && State.Money >= 20);
                    Option(2, State.HasEnergyDrink ? "Энергетик уже в рюкзаке" : $"Купить энергетик — {SandboxState.EnergyDrinkPrice}",
                        !State.HasEnergyDrink && State.Money >= SandboxState.EnergyDrinkPrice);
                    break;
                case Screen.Inventory:
                    Title.text = "Рюкзак";
                    InventoryView.Open(State);
                    break;
                case Screen.Offer:
                    Title.text = job == SandboxJob.Sorting ? "Подработка: разбор посылок" : "Подработка: копицентр";
                    bool available = State.CanWork(job, out string reason);
                    Body.text = $"Оплата +{SandboxState.Pay(job)}, энергия -{SandboxState.Cost(job)} после выполнения.\n"
                        + (job == SandboxJob.Sorting ? "Разложи три посылки по подписанным полкам." : "Настрой формат, количество и тип печати по заказу. Нужен интеллект 1.")
                        + "\nБез таймера и штрафов за ошибки. После первого успеха ту же работу можно выполнять быстро с той же оплатой.\n" + reason;
                    Option(0, "Выполнить мини-игру", available);
                    Option(1, State.IsMastered(job) ? "Быстро выполнить знакомую работу" : "Быстрое выполнение откроется после первого успеха", available && State.IsMastered(job));
                    break;
                case Screen.Sort:
                    var order = State.ActiveOrder;
                    Title.text = $"Разбор посылок • {order.Step + 1}/3";
                    Body.text = "В посылке: " + Parcels[order.Target(order.Step), (order.Id + order.Step) % 3]
                        + ".\nНа какую полку её поставить?\n\n" + hint;
                    for (int i = 0; i < 3; i++) Option(i, Categories[i]);
                    break;
                case Screen.Copy:
                    Title.text = "Копицентр • настрой заказ";
                    Body.text = "Клиент просит: " + CopySettings[0][State.ActiveOrder.Target(0)] + ", "
                        + CopySettings[1][State.ActiveOrder.Target(1)] + ", " + CopySettings[2][State.ActiveOrder.Target(2)]
                        + ".\nНажимай настройки, чтобы переключать варианты, затем выдай заказ.\n" + hint;
                    for (int i = 0; i < 3; i++) Option(i, new[] { "Формат: ", "Количество: ", "Печать: " }[i] + CopySettings[i][configuration[i]] + "  →");
                    Option(3, "Выдать заказ");
                    break;
                case Screen.Study:
                    Title.text = "Самостоятельная учёба";
                    Body.text = "Энергия -20, опыт интеллекта +1, готовность +1 (до 6).\nТри занятия открывают интеллект 1 без покупки книги. Здесь проверяем рост героя, без дней, периодов и экзаменационных сроков.";
                    Option(0, "Позаниматься", State.Energy >= 20 && State.Readiness < 6); break;
                case Screen.Rest:
                    Title.text = "Отдых и сон";
                    Body.text = $"Осталось отдыхов {State.RestsRemaining}/2. Серый участок шкалы — усталость.\n\nОтдых возвращает до 40 энергии и снимает обычную усталость. Сон переносит на утро, полностью восстанавливает энергию и обновляет оба отдыха.";
                    Option(0, "Отдохнуть", State.CanRest);
                    Option(1, "Спать до утра");
                    break;
                case Screen.Help:
                    Title.text = "Полигон механик";
                    Body.text = "E — действие рядом с объектом. T — показать или скрыть задачи. I — вещи.\nЗадачи — необязательный маршрут знакомства с механиками. Сроков и календаря нет. Ошибки и отмена заказа бесплатны.\nПрогресс живёт до перезапуска; это проверочный режим, не баланс полного курса.";
                    Option(0, "Начать полигон заново"); Option(1, "Открыть прежний трёхдневный сценарий"); break;
                case Screen.Reset:
                    Title.text = "Начать заново?";
                    Body.text = "Деньги, покупки, освоенные работы и отметки задач в полигоне будут сброшены.";
                    Option(0, "Да, начать заново"); break;
            }
        }
        public void Refresh()
        {
            RefreshEnergy();
            bool[] flags = { State.SortingCompleted > 0, State.BookOwned, State.BookRead, State.CopyCompleted > 0, State.SnacksUsed > 0 };
            string[] names = { "Разобрать три посылки", "Купить учебник", "Прочитать учебник", "Выполнить заказ копицентра", "Использовать перекус" };
            string text = ""; int completed = 0;
            for (int i = 0; i < flags.Length; i++) { if (flags[i]) completed++; text += (flags[i] ? "[x] " : "[ ] ") + names[i] + "\n\n"; }
            Tasks.SetEntries(text.TrimEnd(), completed, flags.Length);
        }

        private void RefreshEnergy()
        {
            string bonus = State.BonusEnergy > 0 ? $" (+{State.BonusEnergy} врем.)" : "";
            Resources.text = $"День {State.DayNumber}   •   Деньги {State.Money}   •   Энергия {State.Energy}/100{bonus}   •   Отдых {State.RestsRemaining}/2\n"
                + $"Интеллект {State.IntelligenceLevel} ({State.IntelligenceExperience} опыта)   •   Готовность {State.Readiness}/6";
            if (EnergyBar != null) EnergyBar.Draw(State.RegularEnergy, State.Fatigue, State.BonusEnergy);
        }
    }
}
