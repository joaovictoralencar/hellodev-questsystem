using System;
using HelloDev.UI.Default;
using UnityEngine;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    [RequireComponent(typeof(UISelectable))]
    public class UISelectableStateStyle : MonoBehaviour
    {
        [SerializeField] private Image image;
        [Header("Color")]
        [SerializeField] Color normalStateColour      = Color.white;
        [SerializeField] Color selectedStateColour    = Color.white;
        [SerializeField] Color highlightedStateColour = Color.white;
        [SerializeField] Color pressedStateColour     = Color.white;
        [SerializeField] Color disabledStateColour    = Color.white;
        
        private UISelectable selectable;
        private void Awake()
        {
            selectable   = GetComponent<UISelectable>();
            selectable.ChangedStateEvent.AddListener(OnButtonStateChanged);
            if (image == null) image = selectable.GetComponentInChildren<Image>();
        }

        private void OnButtonStateChanged(UISelectable.SelectableState state)
        {
            switch (state)
            {
                case UISelectable.SelectableState.Normal:
                    image.color = normalStateColour;
                    break;
                case UISelectable.SelectableState.Selected:
                    image.color = selectedStateColour;
                    break;
                case UISelectable.SelectableState.Highlighted:
                    image.color = highlightedStateColour;
                    break;
                case UISelectable.SelectableState.Pressed:
                    image.color = pressedStateColour;
                    break;
                case UISelectable.SelectableState.Disabled:
                    image.color = disabledStateColour;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
