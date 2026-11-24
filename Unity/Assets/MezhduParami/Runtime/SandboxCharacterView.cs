using TMPro;
using UnityEngine;

namespace MezhduParami
{
    public sealed class SandboxCharacterView : MonoBehaviour
    {
        public TMP_Text Level, Experience, Limit, Condition, Rest, Readiness;
        public TMP_Text[] SkillLevels, SkillExperience, SkillAccess;
        public UnityEngine.UI.Image ExperienceFill;
        public UnityEngine.UI.Image[] SkillFills;
        public UnityEngine.UI.Button CloseButton;

        public void Refresh(SandboxState state)
        {
            Level.text = $"УРОВЕНЬ {state.CharacterLevel}";
            int next = state.NextCharacterThreshold;
            Experience.text = next > 0
                ? $"{state.Experience}/{next} опыта  ·  ещё {next - state.Experience} до ур. {state.CharacterLevel + 1}"
                : $"{state.Experience} опыта  ·  максимальный уровень";
            ExperienceFill.fillAmount = next > 0 ? state.Experience / (float)next : 1;
            Limit.text = $"Потолок навыков — уровень {state.CharacterLevel}";
            DrawSkill(0, state.IntelligenceLevel, state.IntelligenceExperience, state, "Копицентр");
            DrawSkill(1, state.StrengthLevel, state.StrengthExperience, state, "Склад");
            DrawSkill(2, state.CharismaLevel, state.CharismaExperience, state, "Инфостойка");
            Condition.text = $"Энергия {state.Energy}/100  ·  усталость {state.Fatigue}/70";
            Rest.text = $"Отдыхов {state.RestsRemaining}/2  ·  восстановление до {state.RecoveryLimit}";
            Readiness.text = $"Готовность {state.Readiness}/6" + (state.BonusEnergy > 0 ? $"  ·  энергетик +{state.BonusEnergy}" : "");
        }

        private void DrawSkill(int index, int level, int experience, SandboxState state, string access)
        {
            int threshold = level == 0 ? 3 : level == 1 ? 8 : 15;
            bool capped = experience >= state.SkillExperienceLimit;
            int target = capped ? state.SkillExperienceLimit : threshold;
            SkillLevels[index].text = $"{level} ур.";
            SkillExperience[index].text = $"{experience}/{target} опыта" + (capped ? " · предел" : "");
            SkillFills[index].fillAmount = experience / (float)target;
            SkillAccess[index].text = access + (level > 0 ? (index == 2 ? " открыта" : " открыт") : ": ур. 1");
        }
    }
}
