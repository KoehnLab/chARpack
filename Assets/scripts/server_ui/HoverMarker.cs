using UnityEngine;
using UnityEngine.UI;

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
    private Sprite hover_grab;

    // Start is called before the first frame update
    void Start()
    {
        hover = Resources.Load<Sprite>("hoverMarker/hover_sprite");
        hover_grab = Resources.Load<Sprite>("hoverMarker/hoverSelect_sprite");

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

    public void setGrab()
    {
        GetComponent<Image>().sprite = hover_grab;
    }

    public void show()
    {
        gameObject.SetActive(true);
    }

    public void hide()
    {
        gameObject.SetActive(false);
    }

    public bool isVisible()
    {
        return gameObject.activeSelf;
    }

}
