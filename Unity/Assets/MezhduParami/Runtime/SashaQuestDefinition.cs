using UnityEngine;

namespace MezhduParami
{
    [CreateAssetMenu(menuName = "Между парами/Обещание Саше")]
    public sealed class SashaQuestDefinition : ScriptableObject
    {
        public string Id = "sasha.prototype.promise";
        public int OfferDay = 1;
        public int DueDay = 3;
        [Range(0, 2)] public int DuePeriod = 2;
        public ActivityDefinition Consultation;
        public ActivityDefinition Help;
        public ActivityDefinition SpecialShift;
        [TextArea(3, 8)] public string OfferText;
        [TextArea(2, 6)] public string FulfilledText;
        [TextArea(2, 6)] public string BrokenText;
        [TextArea(2, 6)] public string DeclinedText;
        public string DeadlineLabel => $"день {DueDay}, {new[] { "утро", "день", "вечер" }[DuePeriod]}";
    }
}
