using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Atom2D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public Atom atom;

    private void Start()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(delegate { selectOnClick(); });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        atom.OnFocusEnter(null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        atom.OnFocusExit(null);
    }

    private void selectOnClick()
    {
        atom.markAtomUI(!atom.isMarked);
    }

}

