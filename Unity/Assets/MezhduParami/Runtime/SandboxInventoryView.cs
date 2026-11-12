using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MezhduParami
{
    public sealed class SandboxInventoryView : MonoBehaviour
    {
        public Button BookSlot, KitSlot, UseButton;
        public Image BookIcon, KitIcon, PreviewIcon;
        public TMP_Text BookLabel, KitLabel, BookCount, KitCount;
        public TMP_Text Category, ItemName, Description, Effect, Feedback, UseLabel;

        private enum Item { None, Book, Kit }
        private static readonly Color32 SlotColor = new Color32(60, 46, 36, 255);
        private static readonly Color32 SelectedColor = new Color32(118, 84, 47, 255);
        private SandboxState state;
        private Item selected;

        private void Awake()
        {
            BookSlot.onClick.AddListener(() => Select(Item.Book));
            KitSlot.onClick.AddListener(() => Select(Item.Kit));
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
            else if (selected == Item.Kit) state.UseKit(out message);
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
            return item == Item.Book && state.BookOwned || item == Item.Kit && state.Kits > 0;
        }

        private void Refresh()
        {
            if (!Owned(selected))
                selected = state.BookOwned ? Item.Book : state.Kits > 0 ? Item.Kit : Item.None;

            UpdateSlot(BookSlot, BookIcon, BookLabel, BookCount, state.BookOwned ? 1 : 0, selected == Item.Book);
            UpdateSlot(KitSlot, KitIcon, KitLabel, KitCount, state.Kits, selected == Item.Kit);
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
                case Item.Kit:
                    PreviewIcon.sprite = KitIcon.sprite;
                    Category.text = "РАСХОДНИК";
                    ItemName.text = "Набор восстановления";
                    Description.text = "Восстанавливает до 20 здоровья и расходует один набор.";
                    Effect.text = state.Health >= 100
                        ? "Здоровье полное. Набор можно оставить на потом."
                        : $"Здоровье {state.Health} → {Mathf.Min(100, state.Health + 20)}\nНаборов {state.Kits} → {state.Kits - 1}";
                    UseLabel.text = "Применить";
                    UseButton.interactable = state.Health < 100;
                    break;
                default:
                    Category.text = "";
                    ItemName.text = "Рюкзак пуст";
                    Description.text = "Учебник и набор восстановления можно купить в лавке.";
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
