using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace chARpack
{
    public class LoadingIndicator : MonoBehaviour
    {

        private static List<LoadingIndicator> instances = new List<LoadingIndicator>();

        public static LoadingIndicator GetPythonInstance()
        {
            var python = instances.Find(x => x.type == Type.PYTHON);
            return python;
        }

        public static LoadingIndicator GetOpenBabelInstance()
        {
            var openbabel = instances.Find(x => x.type == Type.OPENBABEL);
            return openbabel;
        }

        private void Awake()
        {
            instances.Add(this);
        }

        private void Start()
        {
            //show(false);
        }

        public enum Type
        {
            PYTHON,
            OPENBABEL,
            OTHER
        }
        public Type type;
        public Image indicator;
        public TMP_Text label;
        public TMP_Text title;
        private bool stillLoading = false;
        Queue<string> stringUpdates = new Queue<string>();

        public void show(bool value)
        {
            indicator.gameObject.SetActive(value);
            label.gameObject.SetActive(value);
            title.gameObject.SetActive(value);
            GetComponent<Image>().enabled = value;
        }

        public void startLoading(string title_, string text = "Loading ...")
        {
            title.text = title_;
            StartCoroutine(startLoadingCR(text));
        }

        private IEnumerator startLoadingCR(string text)
        {
            label.text = text;
            stillLoading = true;
            int fill = 0;
            while (stillLoading)
            {
                if (stringUpdates.Count > 0)
                {
                    label.text = stringUpdates.Dequeue();
                }
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

        public bool downloadProgressChanged(float? percentage)
        {
            if (percentage.HasValue)
            {
                //Debug.Log($"Download progress {percentage.Value}%");
                stringUpdates.Enqueue($"Downloading {percentage.Value}%");
            }

            return false; // return true if you want to cancel the download
        }

        public void extractProgressChanged(float percent)
        {
            stringUpdates.Clear();
            stringUpdates.Enqueue($"Extracting {percent.ToString("0.00")}%");
        }
    }
}
