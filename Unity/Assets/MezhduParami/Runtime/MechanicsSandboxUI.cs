using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MezhduParami
{
    public sealed class MechanicsSandboxUI : MonoBehaviour, IPlayerInputGate
    {
        public TMP_Text Resources, Progress, Prompt, Toast, Title, Body;
        public GameObject Window, Backdrop, PromptCard, ToastCard;
        public Button[] Choices;
        public Button CloseButton, InventoryButton, HelpButton, SkillsButton;
        public SandboxInventoryView InventoryView;
        public SandboxCharacterView CharacterView;
        public TaskTrackerPanel Tasks;
        public EnergyMeter EnergyBar;
        public SandboxState State { get; private set; } = new SandboxState();
        public bool InputBlocked => Window != null && Window.activeSelf;
        private enum Screen { None, Shop, Inventory, Offer, Sort, Copy, Study, Notes, Rest, Training, Warehouse, Rehearsal, VisitorDesk, PracticeReport, Help, Reset }
        private Screen screen;
        private SandboxJob job;
        private int lastClick = -1, lastClose = -1;
        private readonly int[] configuration = new int[3];
        private float toastUntil;
        private int notesTopic, notesStep;
        private string hint = "";
        private static readonly string[] Categories = { "Учёба", "Спорт", "Быт" };
        private static readonly string[,] Parcels = { { "Конспекты", "Учебники", "Тетради" }, { "Мяч", "Спортивная форма", "Коврик для йоги" }, { "Кружка", "Настольная лампа", "Чайник" } };
        private static readonly string[][] CopySettings = { new[] { "A4", "A5", "A3" }, new[] { "1 копия", "2 копии", "3 копии" }, new[] { "Чёрно-белая", "Цветная", "Двусторонняя" } };
        private static readonly string[] Destinations = { "Библиотека", "Копицентр", "Учебный корпус" };
        private static readonly string[] VisitorRequests =
        {
            "Мне нужен учебник. Где его можно взять на пару дней?",
            "Нужно распечатать доклад с флешки. Куда идти?",
            "У меня семинар в аудитории 203. Как туда попасть?"
        };
        private static readonly (string clue, string gap, string[] cards, int correct)[,] Notes =
        {
            {
                ("Библиотека выдаёт книги на дом, а в читальном зале их можно читать на месте.",
                    "Чтобы взять книгу домой, нужно обратиться в ___.", new[] { "Читальный зал", "Библиотеку", "Копицентр" }, 1),
                ("Библиотека выдаёт книги на дом, а в читальном зале их можно читать на месте.",
                    "Чтобы читать книгу на месте, нужно идти в ___.", new[] { "Читальный зал", "Копицентр", "Спортзал" }, 0)
            },
            {
                ("Задание состоит из черновика и итогового текста. Черновик проверяют во вторник, итог сдают в пятницу.",
                    "На вторник нужно подготовить ___.", new[] { "Итоговый текст", "Список оценок", "Черновик" }, 2),
                ("Задание состоит из черновика и итогового текста. Черновик проверяют во вторник, итог сдают в пятницу.",
                    "Итоговый текст сдают в ___.", new[] { "Понедельник", "Пятницу", "Вторник" }, 1)
            },
            {
                ("Доклад начинается с вопроса исследования. Затем идут наблюдения, а завершает доклад вывод.",
                    "В начало доклада нужно поставить ___.", new[] { "Вопрос исследования", "Вывод", "Наблюдения" }, 0),
                ("Доклад начинается с вопроса исследования. Затем идут наблюдения, а завершает доклад вывод.",
                    "В завершение доклада нужно поставить ___.", new[] { "Наблюдения", "Вывод", "Вопрос исследования" }, 1)
            }
        };
        private delegate bool Command(out string message);

        private void Awake()
        {
            for (int i = 0; i < Choices.Length; i++) { int index = i; Choices[i].onClick.AddListener(() => Choose(index)); }
            CloseButton.onClick.AddListener(Close);
            InventoryButton.onClick.AddListener(OpenInventory);
            InventoryView.UseButton.onClick.AddListener(UseInventoryItem);
            HelpButton.onClick.AddListener(OpenHelp);
            SkillsButton.onClick.AddListener(ToggleSkills);
            CharacterView.CloseButton.onClick.AddListener(() => CharacterView.gameObject.SetActive(false));
        }
        private void Start()
        {
            Window.SetActive(false); Backdrop.SetActive(false);
            Notify("Осмотрись. E — действие, C — персонаж, T — задачи. Занятия доступны в свободном порядке.");
            Refresh();
        }
        private void Update()
        {
            if (screen != Screen.Help && screen != Screen.Reset && State.TickEnergy(Time.unscaledDeltaTime))
            {
                if (State.BonusEnergy == 0) Notify("Действие энергетика закончилось. Наступил спад.");
                RefreshEnergy();
                if (screen == Screen.Inventory) InventoryView.Refresh();
                else if (screen == Screen.Offer || screen == Screen.Study || screen == Screen.Training || screen == Screen.Rehearsal) Render();
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (InputBlocked) Close();
                else if (CharacterView.gameObject.activeSelf) CharacterView.gameObject.SetActive(false);
                else OpenHelp();
            }
            else if (keyboard.iKey.wasPressedThisFrame) { if (screen == Screen.Inventory) Close(); else if (!InputBlocked) OpenInventory(); }
            else if (keyboard.cKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame) ToggleSkills();
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
                case SandboxStationKind.Training: Show(Screen.Training); break;
                case SandboxStationKind.Warehouse: job = SandboxJob.Warehouse; Show(Screen.Offer); break;
                case SandboxStationKind.Rehearsal: Show(Screen.Rehearsal); break;
                case SandboxStationKind.VisitorDesk: job = SandboxJob.VisitorDesk; Show(Screen.Offer); break;
                case SandboxStationKind.PracticeReport: Show(Screen.PracticeReport); break;
            }
        }
        public void OpenInventory() { if (!InputBlocked) { hint = ""; Show(Screen.Inventory); } }
        public void OpenHelp() { if (!InputBlocked) Show(Screen.Help); }
        public void OpenSkills()
        {
            if (InputBlocked) return;
            CharacterView.Refresh(State);
            CharacterView.gameObject.SetActive(true);
        }
        private void ToggleSkills()
        {
            if (InputBlocked) return;
            if (CharacterView.gameObject.activeSelf) CharacterView.gameObject.SetActive(false);
            else OpenSkills();
        }
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
                    else if (index == 2) Execute(State.BuyEnergyDrink);
                    else if (index == 3) Execute(State.BuyTrainingCourse);
                    else Execute(State.BuyCommunicationGuide);
                    break;
                case Screen.Study:
                    if (index == 1 && State.HasStudied) Execute(State.Study);
                    else if (State.CanStudy(out hint))
                    {
                        notesTopic = Random.Range(0, Notes.GetLength(0));
                        notesStep = 0;
                        Show(Screen.Notes);
                    }
                    else Render();
                    break;
                case Screen.Notes:
                    if (index != Notes[notesTopic, notesStep].correct)
                    {
                        hint = "Проверь условие ещё раз. Попытка бесплатна.";
                        Render();
                    }
                    else if (++notesStep == Notes.GetLength(1)) Execute(State.Study);
                    else
                    {
                        hint = "Первый пропуск заполнен.";
                        Render();
                    }
                    break;
                case Screen.PracticeReport: Execute(State.SubmitPracticeReport); break;
                case Screen.Training: Execute(State.Train); break;
                case Screen.Rehearsal: Execute(State.Rehearse); break;
                case Screen.Rest: Execute(index == 0 ? State.Rest : State.Sleep); break;
                case Screen.Offer:
                    if (index == 1) ExecuteQuick();
                    else if (State.BeginWork(job, Random.Range(0, int.MaxValue), out hint))
                    {
                        System.Array.Clear(configuration, 0, configuration.Length);
                        Show(job switch
                        {
                            SandboxJob.Sorting => Screen.Sort,
                            SandboxJob.CopyCenter => Screen.Copy,
                            SandboxJob.Warehouse => Screen.Warehouse,
                            _ => Screen.VisitorDesk
                        });
                    }
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
                case Screen.Warehouse:
                    State.LoadCrate(State.ActiveOrder.Id, index, out hint);
                    if (State.ActiveOrder == null) Finish(hint); else Render();
                    break;
                case Screen.VisitorDesk:
                    State.DirectVisitor(State.ActiveOrder.Id, index, out hint);
                    if (State.ActiveOrder == null) Finish(hint); else Render();
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
            CloseButton.GetComponentInChildren<TMP_Text>().text = State.ActiveOrder != null || screen == Screen.Notes
                ? "Отменить / Esc — без затрат" : "Закрыть / Esc";
            switch (screen)
            {
                case Screen.Shop:
                    Title.text = "Лавка полезных вещей";
                    Body.text = $"Деньги {State.Money}.\nУчебник и практикум дают +3 опыта интеллекта и харизмы соответственно. Чтение стоит 15 энергии.\nПерекус восстанавливает до 20 энергии, энергетик даёт временный запас до 30.\nКурс даёт +1 дополнительный опыт силы за каждую из трёх тренировок.";
                    Option(0, State.BookOwned ? "Учебник уже куплен" : "Купить вводный учебник — 60", !State.BookOwned && State.Money >= 60);
                    Option(1, $"Купить перекус — 20 ({2 - State.SnacksBought} осталось)", State.SnacksBought < 2 && State.Money >= 20);
                    Option(2, State.HasEnergyDrink ? "Энергетик уже в рюкзаке" : $"Купить энергетик — {SandboxState.EnergyDrinkPrice}",
                        !State.HasEnergyDrink && State.Money >= SandboxState.EnergyDrinkPrice);
                    Option(3, State.TrainingCourseOwned ? "Курс тренировок уже куплен" : $"Купить курс тренировок — {SandboxState.TrainingCoursePrice}",
                        !State.TrainingCourseOwned && State.Money >= SandboxState.TrainingCoursePrice);
                    Option(4, State.CommunicationGuideOwned ? "Практикум общения уже куплен" : $"Купить практикум общения — {SandboxState.CommunicationGuidePrice}",
                        !State.CommunicationGuideOwned && State.Money >= SandboxState.CommunicationGuidePrice);
                    break;
                case Screen.Inventory:
                    Title.text = "Рюкзак";
                    InventoryView.Open(State);
                    break;
                case Screen.Offer:
                    Title.text = job switch
                    {
                        SandboxJob.Sorting => "Подработка: разбор посылок",
                        SandboxJob.CopyCenter => "Подработка: копицентр",
                        SandboxJob.Warehouse => "Подработка: склад",
                        _ => "Подработка: стойка информации"
                    };
                    bool available = State.CanWork(job, out string reason);
                    string work = job switch
                    {
                        SandboxJob.Sorting => "Разложи три посылки по подписанным полкам.",
                        SandboxJob.CopyCenter => "Настрой формат, количество и тип печати по заказу. Нужен интеллект 1.",
                        SandboxJob.Warehouse => "Раздели четыре ящика поровну по весу между двумя тележками.",
                        _ => "Помоги двум посетителям найти нужное место. Выбери, куда направить каждого."
                    };
                    string firstShift = State.IsMastered(job) ? "" : $" Первая смена: +{SandboxState.FirstShiftExperience} общего опыта.";
                    Body.text = $"Оплата +{SandboxState.Pay(job)}, энергия -{SandboxState.Cost(job)}.{firstShift}\n"
                        + work
                        + "\nОшибки бесплатны. После первого успеха доступен быстрый повтор.\n" + reason;
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
                case Screen.Warehouse:
                    var shift = State.ActiveOrder;
                    Title.text = "Склад • две тележки";
                    Body.text = $"Выбери два ящика на первую тележку. Остальные два поедут на второй. На каждой должно быть {shift.WarehouseLoad} кг.\n\n" + hint;
                    for (int i = 0; i < 4; i++)
                    {
                        string selected = shift.SelectedCrate == i ? " • выбран" : "";
                        Option(i, $"Ящик {i + 1} · {shift.Target(i)} кг{selected}");
                    }
                    break;
                case Screen.VisitorDesk:
                    var visit = State.ActiveOrder;
                    Title.text = $"Стойка информации • {visit.Step + 1}/2";
                    Body.text = VisitorRequests[visit.Target(visit.Step)] + "\n\n" + hint;
                    for (int i = 0; i < Destinations.Length; i++) Option(i, Destinations[i]);
                    break;
                case Screen.Study:
                    Title.text = "Самостоятельная учёба";
                    bool canStudy = State.CanStudy(out string studyBlock);
                    Body.text = "Заполни два пропуска в конспекте по подсказке. Ошибки и отмена бесплатны.\n\n"
                        + PracticeSummary(SandboxSkill.Intelligence, SandboxState.StudyCost, "интеллекта", State.StudyExperienceGain)
                        + $"\nГотовность +{State.StudyReadinessGain} (сейчас {State.Readiness}/6)."
                        + (canStudy ? (State.StudyExperienceGain == 0 ? "\n" + State.SkillLimitHint : "") : "\n" + studyBlock);
                    Option(0, "Открыть конспект", canStudy);
                    if (State.HasStudied) Option(1, "Быстро повторить подготовку", canStudy);
                    break;
                case Screen.Notes:
                    var note = Notes[notesTopic, notesStep];
                    Title.text = $"Конспект • {notesStep + 1}/{Notes.GetLength(1)}";
                    Body.text = note.clue + "\n\n" + note.gap + (hint.Length > 0 ? "\n\n" + hint : "");
                    for (int i = 0; i < note.cards.Length; i++) Option(i, note.cards[i]);
                    break;
                case Screen.Training:
                    Title.text = "Тренировка";
                    bool canTrain = State.CanTrain(out string trainBlock);
                    Body.text = $"Сила {State.StrengthLevel} ({State.StrengthExperience} опыта). Склад открывается с 3 опытом.\n\n"
                        + PracticeSummary(SandboxSkill.Strength, SandboxState.TrainingCost, "силы", State.TrainingExperienceGain) + "\n"
                        + (State.TrainingCourseRemaining > 0 ? $"Бонусов курса {State.TrainingCourseRemaining}/3. Бонус расходуется при начислении второго очка."
                            : "Тренироваться можно бесплатно. Курс из лавки ускоряет первые три тренировки после покупки.")
                        + (canTrain ? (State.TrainingExperienceGain == 0 ? "\n" + State.SkillLimitHint : "") : "\n" + trainBlock);
                    Option(0, "Тренироваться", canTrain);
                    break;
                case Screen.Rehearsal:
                    Title.text = "Репетиция выступления";
                    bool canRehearse = State.CanRehearse(out string rehearsalBlock);
                    Body.text = $"Харизма {State.CharismaLevel} ({State.CharismaExperience} опыта). Стойка информации открывается с 3 опытом.\n\n"
                        + PracticeSummary(SandboxSkill.Charisma, SandboxState.RehearsalCost, "харизмы", State.RehearsalExperienceGain)
                        + "\nПрактикум из лавки даёт +3 опыта харизмы."
                        + (canRehearse ? (State.RehearsalExperienceGain == 0 ? "\n" + State.SkillLimitHint : "") : "\n" + rehearsalBlock);
                    Option(0, "Репетировать", canRehearse);
                    break;
                case Screen.PracticeReport:
                    Title.text = "Лист практики";
                    Body.text = State.PracticeReportSubmitted ? "Лист уже сдан."
                        : $"Разовое дело: развей любой навык до 1 и заверши хотя бы одну смену.\n\nНаграда — {SandboxState.PracticeReportExperience} общего опыта.";
                    Option(0, "Сдать лист практики", State.CanSubmitPracticeReport);
                    break;
                case Screen.Rest:
                    Title.text = "Отдых и сон";
                    Body.text = $"Осталось отдыхов {State.RestsRemaining}/2. Серый участок шкалы — усталость.\n\nОтдых возвращает до 40 энергии и снимает обычную усталость. Сон переносит на утро, полностью восстанавливает энергию и обновляет оба отдыха.";
                    Option(0, "Отдохнуть", State.CanRest);
                    Option(1, "Спать до утра");
                    break;
                case Screen.Help:
                    Title.text = "Полигон механик";
                    Body.text = "E — действие. C — персонаж. T — задачи. I — рюкзак.\nСверху слева — развитие, справа — работы, снизу — быт и лист практики. Задачи предлагают маршрут знакомства.\nОшибки и отмена бесплатны. Прогресс живёт до перезапуска; это проверочный режим, не баланс полного курса.";
                    Option(0, "Начать полигон заново"); Option(1, "Открыть прежний трёхдневный сценарий"); break;
                case Screen.Reset:
                    Title.text = "Начать заново?";
                    Body.text = "Деньги, покупки, освоенные работы и отметки задач в полигоне будут сброшены.";
                    Option(0, "Да, начать заново"); break;
            }
        }
        private string PracticeSummary(SandboxSkill skill, int cost, string name, int gain)
        {
            int experience = State.PracticeExperience(skill);
            return $"Энергия -{cost}  •  Опыт {name} +{gain}  •  Общий опыт +{experience}";
        }
        public void Refresh()
        {
            RefreshEnergy();
            bool[] flags = { State.SortingCompleted > 0, State.BookOwned, State.BookRead, State.CopyCompleted > 0, State.SnacksUsed > 0,
                State.StrengthLevel > 0, State.WarehouseCompleted > 0, State.CharismaLevel > 0, State.VisitorDeskCompleted > 0 };
            string[] names = { "Разобрать три посылки", "Купить учебник", "Прочитать учебник", "Выполнить заказ копицентра", "Использовать перекус",
                "Развить силу до 1", "Загрузить тележки на складе", "Развить харизму до 1", "Помочь посетителям" };
            string text = ""; int completed = 0;
            for (int i = 0; i < flags.Length; i++) { if (flags[i]) completed++; text += (flags[i] ? "[x] " : "[ ] ") + names[i] + "\n"; }
            Tasks.SetEntries(text.TrimEnd(), completed, flags.Length);
        }

        private void RefreshEnergy()
        {
            Resources.text = $"День {State.DayNumber}    ·    Деньги {State.Money}";
            string bonus = State.BonusEnergy > 0 ? $"  ·  +{State.BonusEnergy} врем." : "";
            Progress.text = $"Энергия {State.Energy}/100{bonus}";
            if (CharacterView.gameObject.activeSelf) CharacterView.Refresh(State);
            if (EnergyBar != null) EnergyBar.Draw(State.RegularEnergy, State.Fatigue, State.BonusEnergy);
        }
    }
}
