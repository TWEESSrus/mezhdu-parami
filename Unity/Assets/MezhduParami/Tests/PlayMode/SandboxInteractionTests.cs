using System.Collections;
using TMPro;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MezhduParami.Tests
{
    public sealed class SandboxInteractionTests
    {
        private MechanicsSandboxUI ui;
        private PlayerMotor player;
        private Keyboard keyboard;
        private Key[] keys = new Key[0];
        private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
        [UnitySetUp] public IEnumerator SetUp()
        {
            SceneManager.LoadScene("MechanicsSandbox"); yield return null;
            ui = Object.FindFirstObjectByType<MechanicsSandboxUI>();
            player = Object.FindFirstObjectByType<PlayerMotor>();
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.onAfterUpdate += FeedInput; yield return null;
        }
        [TearDown] public void TearDown()
        {
            InputSystem.onAfterUpdate -= FeedInput;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        }
        private void FeedInput()
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            keyboard.MakeCurrent(); InputState.Change(keyboard, new KeyboardState(keys), InputUpdateType.Dynamic);
        }
        private IEnumerator Press(params Key[] held)
        {
            keys = held; yield return null; yield return new WaitForFixedUpdate();
        }
        private IEnumerator Choice(int index)
        {
            yield return null; ui.Choices[index].onClick.Invoke(); ui.Choices[index].onClick.Invoke(); yield return null;
        }
        private IEnumerator Close()
        {
            yield return null; ui.CloseButton.onClick.Invoke(); yield return null;
        }
        private IEnumerator Open(SandboxStationKind kind)
        {
            yield return null; ui.OpenStation(kind); yield return null;
        }
        [UnityTest] public IEnumerator TaskToggleDoesNotBlockMovementButModalDoes()
        {
            Assert.That(ui.Tasks.Visible, Is.False);
            yield return Press(Key.T); yield return Press();
            Assert.That(ui.Tasks.Visible, Is.True);
            Assert.That(ui.InputBlocked, Is.False);
            var start = player.transform.position;
            yield return Press(Key.D); yield return new WaitForSeconds(.25f); yield return Press();
            Assert.That(player.transform.position.x, Is.GreaterThan(start.x + .3f));
            yield return Press(Key.T); yield return Press(); Assert.That(ui.Tasks.Visible, Is.False);
            yield return Open(SandboxStationKind.Shop);
            start = player.transform.position;
            yield return Press(Key.D); yield return new WaitForSeconds(.2f); yield return Press();
            Assert.That(Vector2.Distance(player.transform.position, start), Is.LessThan(.02f));
            yield return Press(Key.Escape); yield return Press();
            Assert.That(ui.InputBlocked, Is.False);
            Assert.That(Camera.main.rect, Is.EqualTo(new Rect(0, 0, 1, 1)));
        }
        [UnityTest] public IEnumerator ButtonsCanCompletePurchaseProgressionAndBothJobsWithoutDoubleRewards()
        {
            yield return Press(Key.I); yield return Press();
            var inventory = ui.InventoryView;
            Assert.That(inventory.gameObject.activeInHierarchy, Is.True);
            Assert.That(inventory.UseButton.gameObject.activeSelf, Is.False);
            yield return Press(Key.I); yield return Press();
            Assert.That(ui.InputBlocked, Is.False);
            yield return Open(SandboxStationKind.Shop); yield return Choice(0);
            Assert.That(ui.State.Money, Is.EqualTo(60)); Assert.That(ui.State.BookRead, Is.False);
            Assert.That(ui.InputBlocked, Is.False, "Purchases return to play without an inventory prompt");
            ui.InventoryButton.onClick.Invoke(); yield return null;
            Assert.That(inventory.BookSlot.interactable, Is.True);
            inventory.BookSlot.onClick.Invoke();
            inventory.UseButton.onClick.Invoke(); inventory.UseButton.onClick.Invoke(); yield return null;
            Assert.That(ui.State.IntelligenceLevel, Is.EqualTo(1)); Assert.That(ui.State.Energy, Is.EqualTo(65));
            Assert.That(inventory.gameObject.activeInHierarchy, Is.True);
            Assert.That(inventory.UseButton.interactable, Is.False);
            yield return Close(); yield return Open(SandboxStationKind.Sorting); yield return Choice(0);
            var order = ui.State.ActiveOrder;
            for (int i = 0; i < 3; i++) yield return Choice(order.Target(i));
            Assert.That(ui.State.SortingCompleted, Is.EqualTo(1)); Assert.That(ui.State.Money, Is.EqualTo(85));
            Assert.That(ui.InputBlocked, Is.False, "Completed jobs return to play");
            yield return Open(SandboxStationKind.CopyCenter); yield return Choice(0);
            order = ui.State.ActiveOrder;
            for (int i = 0; i < 3; i++) for (int value = 0; value < order.Target(i); value++) yield return Choice(i);
            yield return Choice(3);
            Assert.That(ui.State.CopyCompleted, Is.EqualTo(1)); Assert.That(ui.State.Money, Is.EqualTo(130));
            Assert.That(ui.State.Energy, Is.EqualTo(30));
            Assert.That(ui.State.RecoveryLimit, Is.EqualTo(75));
            yield return Open(SandboxStationKind.Shop); yield return Choice(1);
            yield return Open(SandboxStationKind.Shop); yield return Choice(1);
            ui.InventoryButton.onClick.Invoke(); yield return null;
            inventory.SnackSlot.onClick.Invoke();
            Assert.That(inventory.SnackCount.text, Is.EqualTo("×2"));
            inventory.UseButton.onClick.Invoke(); inventory.UseButton.onClick.Invoke(); yield return null;
            Assert.That(ui.State.Energy, Is.EqualTo(50)); Assert.That(ui.State.Snacks, Is.EqualTo(1));
            Assert.That(inventory.SnackCount.text, Is.EqualTo("×1"));
            Assert.That(ui.Tasks.Caption.text, Does.Contain("5/9"));
        }
        [UnityTest] public IEnumerator TrainingCourseAndWarehouseWorkThroughTheWorldStations()
        {
            yield return InteractAt("warehouse");
            Assert.That(ui.Body.text, Does.Contain("сила 1"));
            Assert.That(ui.Choices[0].interactable, Is.False);
            yield return Close();
            yield return Open(SandboxStationKind.Shop); yield return Choice(3);
            Assert.That(ui.InputBlocked, Is.False);
            ui.InventoryButton.onClick.Invoke(); yield return null;
            Assert.That(ui.InventoryView.CourseCount.text, Is.EqualTo("3/3"));
            yield return Close();
            for (int i = 0; i < 2; i++)
            {
                yield return InteractAt("training");
                Assert.That(ui.Body.text, Does.Contain(i == 0 ? "силы +2" : "силы +1"));
                yield return Choice(0);
            }
            Assert.That(ui.State.StrengthLevel, Is.EqualTo(1));
            Assert.That(ui.State.TrainingCourseRemaining, Is.EqualTo(2));
            yield return InteractAt("warehouse");
            yield return Choice(0);
            var order = ui.State.ActiveOrder;
            Assert.That(order, Is.Not.Null);
            yield return Choice(0);
            int second = 1;
            while (order.Target(0) + order.Target(second) != order.WarehouseLoad) second++;
            yield return Choice(second);
            Assert.That(ui.State.WarehouseCompleted, Is.EqualTo(1));
            Assert.That(ui.State.Money, Is.EqualTo(135));
            Assert.That(ui.State.Energy, Is.EqualTo(5));
            Assert.That(ui.InputBlocked, Is.False);
            Assert.That(ui.Tasks.Caption.text, Does.Contain("2/9"));
        }

        [UnityTest] public IEnumerator GuideAndVisitorDeskWorkThroughTheNormalControls()
        {
            yield return InteractAt("visitorDesk", Vector3.up * 1.55f);
            Assert.That(ui.Body.text, Does.Contain("харизма 1"));
            Assert.That(ui.Choices[0].interactable, Is.False);
            yield return Close();
            yield return Open(SandboxStationKind.Shop); yield return Choice(4);
            ui.InventoryButton.onClick.Invoke(); yield return null;
            Assert.That(ui.InventoryView.GuideSlot.interactable, Is.True);
            ui.InventoryView.GuideSlot.onClick.Invoke();
            ui.InventoryView.UseButton.onClick.Invoke();
            ui.InventoryView.UseButton.onClick.Invoke();
            Assert.That(ui.State.CharismaExperience, Is.EqualTo(3));
            yield return Close();
            yield return InteractAt("visitorDesk", Vector3.up * 1.55f);
            yield return Choice(0);
            var order = ui.State.ActiveOrder;
            yield return Choice(order.Target(0));
            yield return Choice(order.Target(1));
            Assert.That(ui.State.VisitorDeskCompleted, Is.EqualTo(1));
            Assert.That(ui.State.Money, Is.EqualTo(100));
            Assert.That(ui.State.Energy, Is.EqualTo(50));
            Assert.That(ui.Tasks.Caption.text, Does.Contain("2/9"));
        }

        [UnityTest] public IEnumerator NotesPayOnceAfterTwoCorrectAnswersAndUnlockTheSameQuickReward()
        {
            yield return InteractAt("study");
            Assert.That(ui.Choices[1].gameObject.activeSelf, Is.False);
            yield return Choice(0);
            yield return Choice((CorrectNoteChoice() + 1) % 3);
            Assert.That(ui.Body.text, Does.Contain("Попытка бесплатна"));
            Assert.That(ui.State.Experience, Is.Zero);
            yield return Choice(CorrectNoteChoice());
            yield return Close();
            Assert.That(ui.State.Energy, Is.EqualTo(80));
            Assert.That(ui.State.Readiness, Is.Zero);
            Assert.That(ui.State.IntelligenceExperience, Is.Zero);
            Assert.That(ui.State.Experience, Is.Zero);

            yield return InteractAt("study");
            yield return Choice(0);
            yield return Choice(CorrectNoteChoice());
            yield return Choice(CorrectNoteChoice());
            Assert.That(ui.InputBlocked, Is.False);
            Assert.That(ui.State.IntelligenceExperience, Is.EqualTo(1));
            Assert.That(ui.State.Experience, Is.EqualTo(4));
            Assert.That(ui.State.Energy, Is.EqualTo(60));
            Assert.That(ui.State.Readiness, Is.EqualTo(1));

            yield return InteractAt("study");
            Assert.That(ui.Choices[1].interactable, Is.True);
            yield return Choice(1);
            Assert.That(ui.State.IntelligenceExperience, Is.EqualTo(2));
            Assert.That(ui.State.Experience, Is.EqualTo(8));
            Assert.That(ui.State.Energy, Is.EqualTo(40));
            yield return Press(Key.J);
            yield return Press();
            Assert.That(ui.CharacterView.gameObject.activeInHierarchy, Is.True);
            Assert.That(ui.InputBlocked, Is.False);
            Assert.That(ui.CharacterView.SkillExperience[0].text, Does.Contain("2/3 опыта"));
            Assert.That(ui.CharacterView.Experience.text, Does.Contain("8/30"));
            Assert.That(ui.CharacterView.Experience.text, Does.Contain("22"));
        }

        [UnityTest] public IEnumerator CharacterSidebarAllowsMovementTasksAndStationInteractions()
        {
            int money = ui.State.Money, energy = ui.State.Energy;
            Assert.That(ui.Resources.text, Does.Not.Contain("Интеллект"));
            Assert.That(ui.Progress.text, Does.Not.Contain("Опыт"));
            yield return Press(Key.C); yield return Press();
            Assert.That(ui.CharacterView.gameObject.activeInHierarchy, Is.True);
            Assert.That(ui.InventoryView.gameObject.activeInHierarchy, Is.False);
            Assert.That(ui.Window.activeSelf, Is.False);
            Assert.That(ui.Backdrop.activeSelf, Is.False);
            Assert.That(ui.InputBlocked, Is.False);
            Vector3 start = player.transform.position;
            yield return Press(Key.D); yield return new WaitForSeconds(.2f); yield return Press();
            Assert.That(player.transform.position.x, Is.GreaterThan(start.x + .3f));
            yield return Press(Key.T); yield return Press();
            Assert.That(ui.Tasks.Visible, Is.True);
            Assert.That(ui.CharacterView.gameObject.activeInHierarchy, Is.True);
            yield return Press(Key.C); yield return Press();
            Assert.That(ui.CharacterView.gameObject.activeSelf, Is.False);
            Assert.That(ui.InputBlocked, Is.False);
            ui.SkillsButton.onClick.Invoke(); yield return null;
            Assert.That(ui.CharacterView.gameObject.activeInHierarchy, Is.True);
            ui.SkillsButton.onClick.Invoke(); yield return null;
            Assert.That(ui.CharacterView.gameObject.activeSelf, Is.False);
            ui.OpenSkills();
            ui.CharacterView.CloseButton.onClick.Invoke(); yield return null;
            Assert.That(ui.CharacterView.gameObject.activeSelf, Is.False);
            ui.OpenSkills();
            yield return Press(Key.Escape); yield return Press();
            Assert.That(ui.CharacterView.gameObject.activeSelf, Is.False);
            Assert.That(ui.InputBlocked, Is.False);
            Assert.That(ui.State.Money, Is.EqualTo(money));
            Assert.That(ui.State.Energy, Is.EqualTo(energy));
            Assert.That(ui.State.Experience, Is.Zero);
            ui.OpenSkills();
            yield return InteractAt("training");
            Assert.That(ui.InputBlocked, Is.True, "Only the activity itself blocks movement");
            Assert.That(ui.CharacterView.gameObject.activeInHierarchy, Is.True);
            yield return Choice(0);
            Assert.That(ui.InputBlocked, Is.False);
            Assert.That(ui.CharacterView.SkillExperience[1].text, Does.Contain("1/3"));
            Assert.That(ui.CharacterView.Experience.text, Does.Contain("4/30"));
            Assert.That(ui.CharacterView.Condition.text, Does.Contain("55/100"));
            yield return Press(Key.I); yield return Press();
            Assert.That(ui.InventoryView.gameObject.activeInHierarchy, Is.True);
            yield return Press(Key.Escape); yield return Press();
            Assert.That(ui.InputBlocked, Is.False);
            Assert.That(ui.CharacterView.gameObject.activeInHierarchy, Is.True);
            Assert.That(ui.Tasks.Visible, Is.True);
        }

        [UnityTest] public IEnumerator CharacterSidebarUpdatesEnergyWhileTheGameKeepsRunning()
        {
            ui.State.BuyEnergyDrink(out _);
            ui.State.DrinkEnergyDrink(out _);
            ui.Refresh();
            yield return Press(Key.C); yield return Press();
            int bonus = ui.State.BonusEnergy;
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(ui.State.BonusEnergy, Is.LessThan(bonus));
            Assert.That(ui.CharacterView.Readiness.text, Does.Contain($"энергетик +{ui.State.BonusEnergy}"));
            Assert.That(ui.InputBlocked, Is.False);
            ui.State.TickEnergy(ui.State.BonusEnergy - 1);
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(ui.State.BonusEnergy, Is.Zero);
            Assert.That(ui.CharacterView.Condition.text, Does.Contain("75/100"));
            Assert.That(ui.CharacterView.Condition.text, Does.Contain("5/70"));
            Assert.That(ui.CharacterView.Readiness.text, Does.Not.Contain("энергетик"));
            Assert.That(ui.CharacterView.gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest] public IEnumerator AllStationApproachesAreReachableAndSelectTheCorrectActivity()
        {
            // Flood the real colliders with a player-sized clearance, then use E at each approach.
            const float step = .4f;
            var reached = new System.Collections.Generic.HashSet<Vector2Int>();
            var queue = new System.Collections.Generic.Queue<Vector2Int>();
            Vector2 origin = player.transform.position;
            reached.Add(Vector2Int.zero); queue.Enqueue(Vector2Int.zero);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var next = cell + direction;
                    Vector2 point = origin + (Vector2)next * step;
                    if (Mathf.Abs(point.x) > 13 || Mathf.Abs(point.y) > 6.2f || reached.Contains(next)) continue;
                    bool blocked = false;
                    foreach (var hit in Physics2D.OverlapBoxAll(point, new Vector2(.6f, .6f), 0))
                        if (!hit.isTrigger && hit.GetComponent<PlayerMotor>() == null) { blocked = true; break; }
                    if (blocked) continue;
                    reached.Add(next); queue.Enqueue(next);
                }
            }
            foreach (var station in Object.FindObjectsByType<SandboxStation>(FindObjectsSortMode.None))
            {
                Vector2 approach = (Vector2)station.transform.position + Vector2.down * 1.2f;
                Vector2 cell = (approach - origin) / step;
                Assert.That(reached.Contains(new Vector2Int(Mathf.RoundToInt(cell.x), Mathf.RoundToInt(cell.y))), Is.True, station.Id);
                player.transform.position = approach;
                player.GetComponent<Rigidbody2D>().position = approach;
                Physics2D.SyncTransforms(); yield return null;
                Assert.That(ui.Prompt.text, Does.Contain(station.DisplayName));
                yield return Press(Key.E); yield return Press();
                Assert.That(ui.InputBlocked, Is.True, station.Id);
                yield return Close();
            }
        }

        private int CorrectNoteChoice()
        {
            string question = ui.Body.text.Split('\n')[2];
            string answer = question switch
            {
                "Чтобы взять книгу домой, нужно обратиться в ___." => "Библиотеку",
                "Чтобы читать книгу на месте, нужно идти в ___." => "Читальный зал",
                "На вторник нужно подготовить ___." => "Черновик",
                "Итоговый текст сдают в ___." => "Пятницу",
                "В начало доклада нужно поставить ___." => "Вопрос исследования",
                "В завершение доклада нужно поставить ___." => "Вывод",
                _ => throw new System.InvalidOperationException(question)
            };
            for (int i = 0; i < 3; i++)
                if (ui.Choices[i].GetComponentInChildren<TMP_Text>().text == answer) return i;
            throw new System.InvalidOperationException("Missing answer");
        }

        private IEnumerator InteractAt(string name, Vector3? offset = null)
        {
            var location = GameObject.Find(name).transform.position + (offset ?? Vector3.right * 1.8f);
            player.transform.position = location;
            player.GetComponent<Rigidbody2D>().position = location;
            Physics2D.SyncTransforms();
            yield return null;
            yield return Press(Key.E);
            yield return Press();
            Assert.That(ui.InputBlocked, Is.True);
        }

        [UnityTest] public IEnumerator EnergyDrinkCanBeUsedFromTheBackpackAndDecaysWhileIdle()
        {
            yield return Open(SandboxStationKind.Shop);
            yield return Choice(2);
            Assert.That(ui.InputBlocked, Is.False);
            ui.InventoryButton.onClick.Invoke();
            yield return null;
            var inventory = ui.InventoryView;
            Assert.That(inventory.DrinkSlot.interactable, Is.True);
            inventory.DrinkSlot.onClick.Invoke();
            inventory.UseButton.onClick.Invoke();
            inventory.UseButton.onClick.Invoke();
            Assert.That(ui.State.HasEnergyDrink, Is.False);
            Assert.That(ui.State.BonusEnergy, Is.EqualTo(20));
            Assert.That(ui.EnergyBar.Bonus.enabled, Is.True);
            Assert.That(ui.EnergyBar.Energy.fillOrigin, Is.EqualTo((int)UnityEngine.UI.Image.OriginHorizontal.Left));
            Assert.That(ui.EnergyBar.Fatigue.fillOrigin, Is.EqualTo((int)UnityEngine.UI.Image.OriginHorizontal.Right));
            Assert.That(ui.EnergyBar.Bonus.rectTransform.anchorMin.x, Is.EqualTo(.8f).Within(.001f));
            yield return Close();
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(ui.State.BonusEnergy, Is.LessThan(20));
            Assert.That(ui.State.RegularEnergy, Is.EqualTo(80));
            Assert.That(ui.State.Fatigue, Is.Zero);
            Assert.That(ui.State.TickEnergy(ui.State.BonusEnergy - 1), Is.True);
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(ui.State.BonusEnergy, Is.Zero);
            Assert.That(ui.State.RegularEnergy, Is.EqualTo(75));
            Assert.That(ui.State.Fatigue, Is.EqualTo(5));
            Assert.That(ui.EnergyBar.Bonus.enabled, Is.False);
            Assert.That(ui.EnergyBar.Fatigue.fillAmount, Is.EqualTo(.05f).Within(.001f));
            Assert.That(ui.Toast.text, Does.Contain("Наступил спад"));
        }

        [UnityTest] public IEnumerator CancelAndQuickWorkRespectCostsAndInteractionPromptsClear()
        {
            var body = player.GetComponent<Rigidbody2D>();
            var station = GameObject.Find("sorting").transform.position;
            player.transform.position = station + Vector3.down * 1.5f; body.position = player.transform.position;
            Physics2D.SyncTransforms(); yield return null; yield return null;
            Assert.That(ui.Prompt.text, Does.Contain("Посылки"));
            yield return Press(Key.E); yield return Press();
            Assert.That(ui.InputBlocked, Is.True);
            yield return Choice(0); var first = ui.State.ActiveOrder;
            for (int i = 0; i < 3; i++) yield return Choice(first.Target(i));
            yield return Close(); yield return Open(SandboxStationKind.Sorting); yield return Choice(0);
            yield return Choice(ui.State.ActiveOrder.Target(0)); yield return Close();
            Assert.That(ui.State.Money, Is.EqualTo(145)); Assert.That(ui.State.Energy, Is.EqualTo(65));
            yield return Open(SandboxStationKind.Sorting); yield return Choice(1);
            Assert.That(ui.State.Money, Is.EqualTo(170)); Assert.That(ui.State.Energy, Is.EqualTo(50));
            yield return Close();
            player.transform.position = Vector3.zero; body.position = Vector2.zero; Physics2D.SyncTransforms();
            yield return null; yield return null;
            Assert.That(ui.Prompt.text, Is.Empty); Assert.That(ui.PromptCard.activeSelf, Is.False);
        }
    }
}
