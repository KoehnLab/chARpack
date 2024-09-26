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
        //show(false);
    }

    public Image indicator;
    public TMP_Text label;
    private bool stillLoading = false;

    public void show(bool value)
    {
        indicator.gameObject.SetActive(value);
        label.gameObject.SetActive(value);
        GetComponent<Image>().enabled = value;
    }

    public void startLoading(string text = "Loading ...")
    {
        StartCoroutine(startLoadingCR(text));
    }

    private IEnumerator startLoadingCR(string text)
    {
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
    }

    private IEnumerator showFinalCR(bool success, string message)
    {
        label.text = message;
        indicator.fillAmount = success ? 1f : 0f;
        yield return new WaitForSeconds(5);
        show(false);
    }

    public void loadingFinished(bool success, string message)
    {
        stillLoading = false;
        StartCoroutine(showFinalCR(success, message));
    }

}
