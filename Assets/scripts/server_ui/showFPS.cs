using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace chARpack
{
    public class showFPS : MonoBehaviour
    {
        public float deltaTime;

        void Update()
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            if (LoginData.isServer)
            {
                GetComponent<TextMeshProUGUI>().text = $"{Mathf.Ceil(fps)} fps";
            }
            else
            {
                GetComponent<TextMeshPro>().text = $"{Mathf.Ceil(fps)} fps";
            }
        }
    }
}
