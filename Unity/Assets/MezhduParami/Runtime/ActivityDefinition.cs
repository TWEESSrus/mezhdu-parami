using UnityEngine;

namespace MezhduParami
{
    [CreateAssetMenu(menuName = "Между парами/Занятие")]
    public sealed class ActivityDefinition : ScriptableObject
    {
        public string Id;
        public string Title;
        [TextArea] public string Description;
        public int EnergyDelta;
        public int MoneyDelta;
        public int IntelligenceExperience;
        public bool IsWork;
    }
}
