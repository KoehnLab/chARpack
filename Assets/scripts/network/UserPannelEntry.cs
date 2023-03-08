using TMPro;
using UnityEngine;

public class UserPannelEntry : MonoBehaviour
{
    public GameObject textGameObject;
    [HideInInspector] public TextMeshPro nameField;

    private void Awake()
    {
        nameField = GetComponent<TextMeshPro>();
    }
}
