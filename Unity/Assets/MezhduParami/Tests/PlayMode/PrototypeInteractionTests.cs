using System.Collections;
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

        [UnityTest] public IEnumerator InputMovesPlayerAndFurnitureStopsThem()
        {
            var start = player.transform.position;
            yield return Press(Key.D);
            yield return new WaitForSeconds(0.2f);
            Assert.That(player.transform.position.x, Is.GreaterThan(start.x + 0.2f));
            Assert.That(ui.Day.Period, Is.Zero, "Walking must not advance the calendar");
            yield return Press(Key.D, Key.W);
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,
                Is.EqualTo(player.Speed).Within(0.05f));
            yield return Press();
            Teleport(new Vector2(-5, 0));
            yield return Press(Key.W);
            yield return new WaitForSeconds(0.6f);
            Assert.That(player.GetComponent<Rigidbody2D>().position.y, Is.LessThan(0.6f), "Desk should block movement");
            yield return Press();
        }

        [UnityTest] public IEnumerator EOpensActivityCancelIsFreeAndConfirmationRunsOnce()
        {
            Teleport(new Vector2(-5, 0));
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
            Teleport(new Vector2(2.2f, -0.8f));
            yield return null;
            yield return Press(Key.E);
            Assert.That(ui.DialogBody.text, Does.Contain("Привет"));
            ui.PrimaryButton.onClick.Invoke();
            Assert.That(ui.Day.Period, Is.EqualTo(1), "Conversation must be free");
        }
    }
}
