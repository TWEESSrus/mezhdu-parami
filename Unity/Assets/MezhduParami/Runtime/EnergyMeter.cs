using UnityEngine;

namespace MezhduParami
{
    public sealed class EnergyMeter : MonoBehaviour
    {
        public UnityEngine.UI.Image Energy;
        public UnityEngine.UI.Image Fatigue;
        public UnityEngine.UI.Image Bonus;

        public void Draw(int energy, int fatigue, int bonus = 0)
        {
            Energy.fillAmount = energy / (float)EnergyReserve.Full;
            Fatigue.fillAmount = fatigue / (float)EnergyReserve.Full;
            Bonus.enabled = bonus > 0;
            Bonus.rectTransform.anchorMin = new Vector2(energy / (float)EnergyReserve.Full, 0f);
            Bonus.rectTransform.anchorMax = new Vector2((energy + bonus) / (float)EnergyReserve.Full, 1f);
        }
    }
}
