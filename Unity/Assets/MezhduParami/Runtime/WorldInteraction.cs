using UnityEngine;

namespace MezhduParami
{
    public sealed class WorldInteraction : MonoBehaviour
    {
        public string DisplayName;
        public ActivityDefinition Activity;
        [TextArea] public string Conversation;
    }
}
