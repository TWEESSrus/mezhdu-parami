using System.Collections;
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
            Assert.That(ui.Tasks.Visible, Is.True);
            yield return Press(Key.T); yield return Press();
            Assert.That(ui.Tasks.Visible, Is.False);
            Assert.That(ui.InputBlocked, Is.False);
            var start = player.transform.position;
            yield return Press(Key.D); yield return new WaitForSeconds(.25f); yield return Press();
            Assert.That(player.transform.position.x, Is.GreaterThan(start.x + .3f));
            yield return Press(Key.T); yield return Press(); Assert.That(ui.Tasks.Visible, Is.True);
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
            Assert.That(ui.Tasks.Caption.text, Does.Contain("5/5"));
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
