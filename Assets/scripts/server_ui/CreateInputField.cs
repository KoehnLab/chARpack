using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CreateInputField : MonoBehaviour
{
    private static CreateInputField _singleton;
    public static CreateInputField Singleton
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
                Debug.Log($"[{nameof(CreateInputField)}] Instance already exists, destroying duplicate!");
                Destroy(value.gameObject);
            }

        }
    }
    private void Awake()
    {
        Singleton = this;
    }

    [HideInInspector] public TMP_InputField input_field;
    // Start is called before the first frame update
    void Start()
    {
        input_field = GetComponent<TMP_InputField>();
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            foreach (var symbol in GlobalCtrl.Singleton.Dic_ElementData.Keys)
            {
                if (input_field.text.ToLower() == symbol.ToLower())
                {
                    GlobalCtrl.Singleton.createAtomUI(symbol);
                    gameObject.SetActive(false);
                    return;
                }
            }
            OpenBabelReadWrite.Singleton.createSmiles(input_field.text);
            gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.SetActive(false);
        }
    }
}
