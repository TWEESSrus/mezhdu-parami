using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MezhduParami.Tests
{
    public sealed class PrototypeInteractionTests
    {
        private Keyboard keyboard;
        private PrototypeUI ui;
        private PlayerMotor player;
        private Key[] heldKeys = new Key[0];
        private InputSettings.BackgroundBehavior oldBackground;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
#endif

        [UnitySetUp] public IEnumerator SetUp()
        {
            SceneManager.LoadScene("PrototypeDorm");
            yield return null;
            Assert.That(Application.isPlaying, Is.True, "Integration tests require actual Play Mode");
            keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            heldKeys = new Key[0];
            InputSystem.onAfterUpdate += FeedGameInput;
            oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            ui = Object.FindFirstObjectByType<PrototypeUI>();
            player = Object.FindFirstObjectByType<PlayerMotor>();
            GameObject.Find("StudentFemale").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(ui.InputBlocked, Is.False, "Selection button must enable gameplay");
        }

        [TearDown] public void TearDown()
        {
            InputSystem.onAfterUpdate -= FeedGameInput;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = oldBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
#endif
        }

        private IEnumerator Press(params Key[] keys)
        {
            keyboard.MakeCurrent();
            heldKeys = keys;
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        private void FeedGameInput()
        {
            // Test coroutines can execute with the Editor input buffer active.
            // Write only the game buffer after its normal input update, before
            // MonoBehaviour.Update reads it. No production input settings change.
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            keyboard.MakeCurrent();
            InputState.Change(keyboard, new KeyboardState(heldKeys), InputUpdateType.Dynamic);
        }

        private void Teleport(Vector2 position)
        {
            var body = player.GetComponent<Rigidbody2D>();
            player.transform.position = position;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        [UnityTest] public IEnumerator FullWindowViewportAndPromptFollowLocation()
        {
            var camera = Camera.main;
            var canvas = ui.GetComponent<Canvas>();
            Assert.That(camera.rect, Is.EqualTo(new Rect(0, 0, 1, 1)), "No legacy letterbox viewport");
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(camera.GetComponents<MonoBehaviour>().Any(c => c.GetType().Name == "PixelPerfectCamera"), Is.False);
            var sasha = GameObject.Find("Sasha").transform.position;
            Teleport(sasha + Vector3.down);
            yield return null;
            yield return null;
            Assert.That(ui.Prompt.text, Does.Contain("Саша"));
            Assert.That(ui.PromptCard.activeSelf, Is.True);
            yield return Press(Key.S);
            yield return new WaitForSeconds(1.5f);
            yield return Press();
            Assert.That(ui.Prompt.text, Is.Empty, "Walking away must clear the old action");
            Assert.That(ui.PromptCard.activeSelf, Is.False);
            var desk = GameObject.Find("StudyDesk").GetComponent<BoxCollider2D>().bounds;
            Teleport(new Vector2(desk.center.x, desk.min.y - .65f));
            yield return null;
            yield return null;
            Assert.That(ui.Prompt.text, Does.Contain("Учебный стол"));
            Assert.That(ui.PromptCard.activeSelf, Is.True);
            Assert.That(camera.rect, Is.EqualTo(new Rect(0, 0, 1, 1)));
        }

        [UnityTest] public IEnumerator InputMovesPlayerAndFurnitureStopsThem()
        {
            Teleport(new Vector2(-5.6f, 3.55f));
            yield return null;
            var start = player.transform.position;
            yield return Press(Key.D);
            yield return new WaitForSeconds(0.2f);
            Assert.That(player.transform.position.x, Is.GreaterThan(start.x + 0.2f));
            Assert.That(ui.Day.Period, Is.Zero, "Walking must not advance the calendar");
            yield return Press(Key.D, Key.W);
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,
                Is.EqualTo(player.Speed).Within(0.05f));
            yield return Press();
            var desk = GameObject.Find("StudyDesk").GetComponent<BoxCollider2D>().bounds;
            Teleport(new Vector2(desk.center.x, desk.min.y - 0.65f));
            yield return Press(Key.W);
            yield return new WaitForSeconds(0.6f);
            Assert.That(player.GetComponent<Rigidbody2D>().position.y, Is.LessThan(desk.min.y - 0.2f), "Desk should block movement");
            yield return Press();
        }

        [UnityTest] public IEnumerator EOpensActivityCancelIsFreeAndConfirmationRunsOnce()
        {
            var desk = GameObject.Find("StudyDesk").GetComponent<BoxCollider2D>().bounds;
            Teleport(new Vector2(desk.center.x, desk.min.y - 0.65f));
            yield return null;
            yield return Press(Key.E);
            Assert.That(ui.DialogPanel.activeSelf, Is.True);
            Assert.That(ui.DialogBody.text, Does.Contain("Энергия -20"));
            Assert.That(ui.Day.Period, Is.Zero);
            yield return Press();
            yield return Press(Key.Escape);
            Assert.That(ui.DialogPanel.activeSelf, Is.False);
            Assert.That(ui.Day.Energy, Is.EqualTo(80));
            yield return Press();
            yield return Press(Key.E);
            ui.PrimaryButton.onClick.Invoke();
            ui.PrimaryButton.onClick.Invoke();
            Assert.That(ui.Day.Period, Is.EqualTo(1));
            Assert.That(ui.Day.Energy, Is.EqualTo(60));
            Assert.That(ui.Day.IntelligenceExperience, Is.EqualTo(1));
            Assert.That(ui.DialogPanel.activeSelf, Is.False);
            yield return Press();
            Teleport((Vector2)GameObject.Find("Sasha").transform.position + Vector2.up * 0.9f);
            yield return null;
            yield return Press(Key.E);
            Assert.That(ui.DialogBody.text, Does.Contain("Привет"));
            yield return Press();
            yield return Press(Key.Escape);
            Assert.That(ui.Day.Period, Is.EqualTo(1), "Reading and cancelling the offer must be free");
            Assert.That(ui.Promise.State, Is.EqualTo(PromiseState.NotAccepted));
        }

        [UnityTest] public IEnumerator CampusEntranceIsPassableAndCameraAndWalkCycleFollowPlayer()
        {
            var view = Camera.main;
            var start = player.transform.position;
            float initialCameraY = view.transform.position.y;
            Assert.That(player.WalkAnimator, Is.Not.Null);
            var visual = player.WalkAnimator.GetComponent<SpriteRenderer>();
            Assert.That(visual.sprite, Is.Not.Null);
            var idle = visual.sprite;
            Assert.That(player.WalkAnimator.speed, Is.Zero);
            yield return Press(Key.W);
            yield return new WaitForSeconds(0.32f);
            Assert.That(player.WalkAnimator.speed, Is.EqualTo(1f));
            Assert.That(visual.sprite, Is.Not.EqualTo(idle), "The authored walk loop should advance with movement");
            yield return new WaitForSeconds(0.83f);
            Assert.That(player.transform.position.y, Is.GreaterThan(start.y + 2.5f), "Walk through the mapped entrance into the hall");
            Assert.That(view.transform.position.y, Is.GreaterThan(initialCameraY + 1.5f));
            yield return Press();
            yield return null;
            Assert.That(player.WalkAnimator.speed, Is.Zero);
            Assert.That(ui.Day.Period, Is.Zero);
        }

        [UnityTest] public IEnumerator CampusHudTracksLibraryVisitAndJournalButtonOpensWithoutSpendingTime()
        {
            Assert.That(ui.SelectionPanel.activeInHierarchy, Is.False);
            Assert.That(ui.HudGroup, Is.Not.Null, "The HUD must be wired in the saved scene");
            Assert.That(ui.HudGroup.alpha, Is.EqualTo(1f));
            Assert.That(ui.HudGroup.interactable, Is.True);
            Assert.That(ui.MoneyValue, Is.Not.Null);
            Assert.That(ui.MoneyValue.text, Is.EqualTo(ui.Day.Money.ToString()));
            Assert.That(ui.EnergyBar, Is.Not.Null);
            Assert.That(ui.EnergyBar.Energy.fillAmount, Is.EqualTo(ui.Day.Energy / 100f).Within(0.001f));

            var minimap = Object.FindFirstObjectByType<CampusMinimap>();
            Assert.That(minimap, Is.Not.Null, "The saved scene must have a minimap");
            Assert.That(minimap.Player, Is.SameAs(player.transform));
            Assert.That(minimap.MapArea, Is.Not.Null);
            Assert.That(minimap.PlayerMarker, Is.Not.Null);
            Assert.That(minimap.PlayerMarker.parent, Is.SameAs(minimap.MapArea));
            Canvas.ForceUpdateCanvases();
            yield return null;
            var start = (Vector2)minimap.MapArea.InverseTransformPoint(minimap.PlayerMarker.position);

            var desk = GameObject.Find("StudyDesk").GetComponent<BoxCollider2D>().bounds;
            Teleport(new Vector2(desk.center.x, desk.min.y - 0.65f));
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var area = minimap.MapArea.rect;
            var position = (Vector2)minimap.MapArea.InverseTransformPoint(minimap.PlayerMarker.position);
            var expected = CampusMinimap.WorldToMapPosition(player.transform.position, minimap.WorldBounds, area);
            Assert.That(position.x, Is.GreaterThan(start.x), "The library is east of the entrance");
            Assert.That(position.y, Is.GreaterThan(start.y), "The library is north of the entrance");
            Assert.That(Vector2.Distance(position, expected), Is.LessThan(0.01f));
            var markerBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(minimap.MapArea, minimap.PlayerMarker);
            Assert.That(area.Contains((Vector2)markerBounds.min) && area.Contains((Vector2)markerBounds.max), Is.True,
                "The player marker must stay inside the map at the library");

            var journalButton = ui.GetComponentsInChildren<UnityEngine.UI.Button>()
                .SingleOrDefault(button => Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                    .Any(index => button.onClick.GetPersistentTarget(index) == ui
                        && button.onClick.GetPersistentMethodName(index) == nameof(PrototypeUI.OpenJournal)));
            Assert.That(journalButton, Is.Not.Null, "The visible HUD needs a wired journal button");
            Assert.That(journalButton.IsInteractable(), Is.True);
            journalButton.onClick.Invoke();
            Assert.That(ui.DialogPanel.activeInHierarchy, Is.True);
            Assert.That(ui.DialogTitle.text, Does.Contain("Журнал"));
            Assert.That(ui.Day.Period, Is.Zero, "Moving and opening the journal must not advance time");
        }

        [UnityTest] public IEnumerator NightCannotBeDismissedAndDoubleClickSleepsOnce()
        {
            yield return Confirm("StudyDesk");
            yield return Confirm("Bed");
            yield return Confirm("Bed");
            Assert.That(ui.Day.IsComplete, Is.True);
            Assert.That(ui.InputBlocked, Is.True);
            ui.CloseDialog();
            Assert.That(ui.DialogPanel.activeSelf, Is.True, "Night requires an explicit sleep choice");
            yield return null;
            ui.PrimaryButton.onClick.Invoke();
            ui.PrimaryButton.onClick.Invoke();
            Assert.That(ui.Day.DayNumber, Is.EqualTo(2));
            Assert.That(ui.Day.Period, Is.Zero);
            Assert.That(ui.Day.Money, Is.EqualTo(120));
            Assert.That(ui.Day.Energy, Is.EqualTo(100));
            Assert.That(ui.InputBlocked, Is.False);
        }

        [UnityTest] public IEnumerator BedBlocksThirdRestAndSleepSkipsTheRemainingPeriod()
        {
            yield return Confirm("Bed");
            yield return Confirm("Bed");
            ui.OpenInteraction(GameObject.Find("Bed").GetComponent<WorldInteraction>());
            Assert.That(ui.PrimaryButton.interactable, Is.False);
            Assert.That(ui.SleepOption.gameObject.activeInHierarchy, Is.True);
            ui.SleepOption.onClick.Invoke();
            ui.SleepOption.onClick.Invoke();
            Assert.That(ui.Day.DayNumber, Is.EqualTo(2));
            Assert.That(ui.Day.Period, Is.Zero);
            Assert.That(ui.Day.Energy, Is.EqualTo(100));
            Assert.That(ui.Day.RestsRemaining, Is.EqualTo(2));
            Assert.That(ui.InputBlocked, Is.False);
        }

        [UnityTest] public IEnumerator KeepingPromiseShowsTrustAndClosedEnding() => RunPromiseRoute(true);

        [UnityTest] public IEnumerator TakingShiftShowsWarningAndBrokenPromiseEnding() => RunPromiseRoute(false);

        [UnityTest] public IEnumerator DecliningIsFreeAndSkippingSleepAppliesTheRecoveryLimit()
        {
            ui.OpenInteraction(GameObject.Find("Sasha").GetComponent<WorldInteraction>());
            ui.CancelButton.onClick.Invoke();
            Assert.That(ui.Promise.State, Is.EqualTo(PromiseState.Declined));
            Assert.That(ui.Day.Period, Is.Zero);
            Assert.That(ui.Day.Readiness, Is.Zero);
            Assert.That(ui.Day.Energy, Is.EqualTo(80));
            yield return Confirm("StudyDesk");
            yield return Confirm("Bed");
            yield return Confirm("Bed");
            ui.PrimaryButton.onClick.Invoke();
            ui.PrimaryButton.onClick.Invoke();
            Assert.That(ui.Day.DayNumber, Is.EqualTo(2));
            Assert.That(ui.Day.Money, Is.EqualTo(120));
            Assert.That(ui.Day.RecoveryLimit, Is.EqualTo(100));
            Assert.That(ui.Promise.Trust, Is.Zero);
            yield return Confirm("Bed");
            yield return Confirm("Bed");
            yield return Confirm("StudyDesk");
            Assert.That(ui.SleepOption.gameObject.activeInHierarchy, Is.True);
            ui.SleepOption.onClick.Invoke();
            ui.SleepOption.onClick.Invoke();
            Assert.That(ui.Day.DayNumber, Is.EqualTo(3));
            Assert.That(ui.Day.RecoveryLimit, Is.EqualTo(75));
            Assert.That(ui.EnergyBar.Fatigue.fillAmount, Is.EqualTo(.25f).Within(.001f));
        }

        private IEnumerator RunPromiseRoute(bool help)
        {
            yield return Confirm("Sasha");
            Assert.That(ui.Promise.State, Is.EqualTo(PromiseState.Promised));
            Assert.That(ui.Day.Readiness, Is.EqualTo(2));
            Assert.That(ui.QuestStatus.text, Does.Contain("день 3"));
            yield return Confirm("Bed");
            yield return Confirm("Bed");
            yield return null;
            ui.PrimaryButton.onClick.Invoke();
            yield return null;
            yield return Confirm("StudyDesk");
            yield return Confirm("Bed");
            yield return Confirm("Bed");
            yield return null;
            ui.PrimaryButton.onClick.Invoke();
            yield return null;
            yield return Confirm("Bed");
            yield return Confirm("Bed");
            Assert.That(ui.Day.DayNumber, Is.EqualTo(3));
            Assert.That(ui.Day.Period, Is.EqualTo(2));
            ui.OpenJournal();
            Assert.That(ui.DialogBody.text, Does.Contain("Выполнить оба дела нельзя"));
            ui.CloseDialog();
            ui.OpenInteraction(GameObject.Find(help ? "Sasha" : "Noticeboard").GetComponent<WorldInteraction>());
            Assert.That(ui.DialogBody.text, Does.Contain(help ? "Доверие Саши +1" : "доверие -1"));
            int before = ui.Day.Money;
            yield return null;
            ui.PrimaryButton.onClick.Invoke();
            ui.PrimaryButton.onClick.Invoke();
            Assert.That(ui.Day.IsFinished, Is.True);
            Assert.That(ui.Promise.Trust, Is.EqualTo(help ? 1 : -1));
            Assert.That(ui.Promise.State, Is.EqualTo(help ? PromiseState.Fulfilled : PromiseState.Broken));
            Assert.That(ui.Day.Money, Is.EqualTo(before + (help ? 0 : 80)));
            Assert.That(ui.DialogTitle.text, Is.EqualTo("Первые дни позади"));
            Assert.That(ui.DialogBody.text, Does.Contain(help ? "Обещание выполнено" : "Обещание нарушено"));
            Assert.That(ui.CancelButton.gameObject.activeSelf, Is.False);
        }

        private IEnumerator Confirm(string objectName)
        {
            yield return null;
            ui.OpenInteraction(GameObject.Find(objectName).GetComponent<WorldInteraction>());
            Assert.That(ui.DialogPanel.activeSelf, Is.True);
            Assert.That(ui.PrimaryButton.interactable, Is.True, ui.DialogBody.text);
            ui.PrimaryButton.onClick.Invoke();
            yield return null;
        }
    }
}
