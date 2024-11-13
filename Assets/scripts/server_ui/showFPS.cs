using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace chARpack
{
    public class showFPS : MonoBehaviour
    {
        public float deltaTime;
        void Update()
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            GetComponent<TextMeshProUGUI>().text = $"{Mathf.Ceil(fps)} fps";
        }
    }
}
