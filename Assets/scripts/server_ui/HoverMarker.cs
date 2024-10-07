using UnityEngine;
using UnityEngine.UI;

namespace chARpack
{
    public class HoverMarker : MonoBehaviour
    {

        private static HoverMarker _singleton;

        public static HoverMarker Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                {
                    _singleton = value;
                }
                else if (_singleton != value)
                {
                    Debug.Log($"[{nameof(HoverMarker)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        private void Awake()
        {
            Singleton = this;
        }

        private Sprite hover;
        private Sprite hoverMiddleGrab;
        private Sprite hoverIndexGrab;

        public bool settingsActive = false;

        // Start is called before the first frame update
        void Start()
        {
            hover = Resources.Load<Sprite>("hoverMarker/hover_sprite");
            hoverMiddleGrab = Resources.Load<Sprite>("hoverMarker/hoverSelect_sprite");
            hoverIndexGrab = Resources.Load<Sprite>("hoverMarker/hoverIndexSelect_sprite");

            gameObject.SetActive(false);
        }

        public void setPosition(Vector2 pos)
        {
            var rect = transform as RectTransform;
            rect.position = pos;
        }

        public void setHover()
        {
            GetComponent<Image>().sprite = hover;
        }

        public void setMiddleGrab()
        {
            GetComponent<Image>().sprite = hoverMiddleGrab;
        }

        public void setIndexGrab()
        {
            GetComponent<Image>().sprite = hoverIndexGrab;
        }

        public void show()
        {
            gameObject.SetActive(true);
        }

        public void hide()
        {
            gameObject.SetActive(false);
        }

        public void setSettingsActive(bool value)
        {
            if (value) hide();
            else show();
            settingsActive = value;
        }

        public bool isVisible()
        {
            return gameObject.activeSelf;
        }

    }
}