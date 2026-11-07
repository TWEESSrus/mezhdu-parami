using UnityEngine;

namespace MezhduParami
{
    public enum SandboxStationKind { Shop, Sorting, CopyCenter, Study, Rest }
    public sealed class SandboxStation : MonoBehaviour
    {
        public string Id;
        public string DisplayName;
        public SandboxStationKind Kind;
    }
}
