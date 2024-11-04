using UnityEngine;
using UnityEngine.EventSystems;

namespace chARpack
{
    public class InfoButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject infoPanel;
        public void OnPointerEnter(PointerEventData eventData)
        {
            infoPanel.SetActive(true);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            infoPanel.SetActive(false);
        }
    }
}