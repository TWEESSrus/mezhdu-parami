using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MezhduParami
{
    public sealed class SandboxInventoryView : MonoBehaviour
    {
        public Button BookSlot, SnackSlot, DrinkSlot, UseButton;
        public Image BookIcon, SnackIcon, DrinkIcon, PreviewIcon;
        public TMP_Text BookLabel, SnackLabel, DrinkLabel, BookCount, SnackCount, DrinkCount;
        public TMP_Text Category, ItemName, Description, Effect, Feedback, UseLabel;

        private enum Item { None, Book, Snack, Drink }
        private static readonly Color32 SlotColor = new Color32(60, 46, 36, 255);
        private static readonly Color32 SelectedColor = new Color32(118, 84, 47, 255);
        private SandboxState state;
        private Item selected;

        private void Awake()
        {
            BookSlot.onClick.AddListener(() => Select(Item.Book));
            SnackSlot.onClick.AddListener(() => Select(Item.Snack));
            DrinkSlot.onClick.AddListener(() => Select(Item.Drink));
        }

        public void Open(SandboxState current)
        {
            state = current;
            selected = Item.None;
            Feedback.text = "";
            gameObject.SetActive(true);
            Refresh();
        }

        public string UseSelected()
        {
            string message = "Выбери предмет.";
            if (selected == Item.Book) state.ReadBook(out message);
            else if (selected == Item.Snack) state.EatSnack(out message);
            else if (selected == Item.Drink) state.DrinkEnergyDrink(out message);
            Feedback.text = message;
            Refresh();
            return message;
        }

        private void Select(Item item)
        {
            if (!Owned(item)) return;
            selected = item;
            Feedback.text = "";
            Refresh();
        }

        private bool Owned(Item item)
        {
            return item == Item.Book && state.BookOwned || item == Item.Snack && state.Snacks > 0
                || item == Item.Drink && state.HasEnergyDrink;
        }

        internal void Refresh()
        {
            if (!Owned(selected))
                selected = state.BookOwned ? Item.Book : state.Snacks > 0 ? Item.Snack : state.HasEnergyDrink ? Item.Drink : Item.None;

            UpdateSlot(BookSlot, BookIcon, BookLabel, BookCount, state.BookOwned ? 1 : 0, selected == Item.Book);
            UpdateSlot(SnackSlot, SnackIcon, SnackLabel, SnackCount, state.Snacks, selected == Item.Snack);
            UpdateSlot(DrinkSlot, DrinkIcon, DrinkLabel, DrinkCount, state.HasEnergyDrink ? 1 : 0, selected == Item.Drink);
            PreviewIcon.enabled = selected != Item.None;
            Effect.transform.parent.gameObject.SetActive(selected != Item.None);
            UseButton.gameObject.SetActive(selected != Item.None);

            switch (selected)
            {
                case Item.Book:
                    PreviewIcon.sprite = BookIcon.sprite;
                    Category.text = "УЧЁБА";
                    ItemName.text = "Вводный учебник";
                    Description.text = "Чтение даёт +3 опыта интеллекта. Первый уровень открывает работу в копицентре.";
                    Effect.text = state.BookRead
                        ? "Учебник прочитан. Опыт уже получен."
                        : state.Energy < 15
                            ? $"Нужно 15 энергии. Сейчас {state.Energy}."
                            : $"Энергия {state.Energy} → {state.Energy - 15}\nОпыт интеллекта {state.IntelligenceExperience} → {state.IntelligenceExperience + 3}";
                    UseLabel.text = state.BookRead ? "Прочитан" : "Прочитать";
                    UseButton.interactable = !state.BookRead && state.Energy >= 15;
                    break;
                case Item.Snack:
                    PreviewIcon.sprite = SnackIcon.sprite;
                    Category.text = "РАСХОДНИК";
                    ItemName.text = "Перекус";
                    Description.text = "Восстанавливает до 20 энергии в пределах текущего лимита.";
                    Effect.text = state.SnackRecovery == 0
                        ? "Достигнут предел восстановления. Перекус сохранится."
                        : $"Энергия {state.Energy} → {state.Energy + state.SnackRecovery}\nПредел восстановления {state.RecoveryLimit}";
                    UseLabel.text = "Съесть";
                    UseButton.interactable = state.SnackRecovery > 0;
                    break;
                case Item.Drink:
                    PreviewIcon.sprite = DrinkIcon.sprite;
                    Category.text = "РАСХОДНИК";
                    ItemName.text = "Энергетик";
                    Description.text = "До 30 временной энергии. Каждую секунду исчезает 1. Трата на действия усиливает усталость.";
                    Effect.text = state.BonusEnergy > 0 ? "Предыдущий энергетик ещё действует."
                        : state.DrinkBonus == 0 ? "Энергия уже полная."
                        : $"Энергия {state.Energy} → {state.Energy + state.DrinkBonus}\nВременный запас +{state.DrinkBonus}";
                    UseLabel.text = "Выпить";
                    UseButton.interactable = state.DrinkBonus > 0;
                    break;
                default:
                    Category.text = "";
                    ItemName.text = "Рюкзак пуст";
                    Description.text = "Учебник, перекус и энергетик продаются в лавке.";
                    Effect.text = "";
                    UseButton.interactable = false;
                    break;
            }
        }

        private static void UpdateSlot(Button slot, Image icon, TMP_Text label, TMP_Text quantity, int count, bool active)
        {
            bool filled = count > 0;
            slot.interactable = filled;
            slot.targetGraphic.color = active ? SelectedColor : SlotColor;
            icon.enabled = filled;
            label.enabled = filled;
            quantity.text = filled ? $"×{count}" : "";
        }
    }
}
