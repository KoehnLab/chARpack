using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using chARpack.ColorPalette;

namespace chARpack
{
    public class tabButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
    {
        public TabGroup group;
        [HideInInspector] public TextMeshProUGUI text;
        public void OnPointerClick(PointerEventData eventData)
        {
            group.OnTabSelected(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            group.OnTabHover(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            group.OnTabExit(this);
        }

        // Start is called before the first frame update
        void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
            text.color = chARpackColors.gray;
            group.Subscribe(this);
        }
    }
}
