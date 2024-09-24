using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingIndicator : MonoBehaviour
{

    private static LoadingIndicator _singleton;

    public static LoadingIndicator Singleton
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
                Debug.Log($"[{nameof(LoadingIndicator)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public Image indicator;
    public TMP_Text label;
    private bool stillLoading = false;

    public IEnumerator startLoading(string text = "Loading ...")
    {
        gameObject.SetActive(true);
        label.text = text;
        stillLoading = true;
        int fill = 0;
        while (stillLoading)
        {
            indicator.fillAmount = fill / 100f;
            fill += 1;
            fill %= 100;
            yield return null;
        }
        gameObject.SetActive(false);
    }

    public IEnumerator showFinal(bool success, string message)
    {
        gameObject.SetActive(true);
        label.text = message;
        indicator.fillAmount = success ? 1f : 0f;
        yield return new WaitForSeconds(5);
    }

    public void loadingFinished()
    {
        stillLoading = false;
    }

}
