using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Quest Type", menuName = "HelloDev/Quest System/Scriptable Objects/Quest Type")]
    public class QuestType_SO : ScriptableObject
    {
        [Header("Core Info")]
        [Tooltip("Internal name for developers, used for identification in code.")]
        [SerializeField]
        private string devName;

        [Tooltip("The localized display name of the quest type.")]
        [SerializeField]
        private LocalizedString displayName;
        
        [SerializeField]
        private Color color;
        
        [SerializeField]
        private Sprite icon;

        public string DevName => devName;
        public LocalizedString DisplayName => displayName;
        public Color Color => color;
        public Sprite Icon => icon;
    }
}
