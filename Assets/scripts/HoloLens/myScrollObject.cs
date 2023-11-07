using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class provides scrollability by pagination used in multiple menus.
/// </summary>
public class myScrollObject : MonoBehaviour
{
    public GameObject gridObjectCollection;
    public GameObject scrollParent;
    public GameObject scrollingObjectCollection;
    public GameObject clippingBox;


    protected void clearEntries()
    {
        foreach (Transform child in gridObjectCollection.transform)
        {
            Destroy(child.gameObject);
        }
    }

    protected void resetRotation()
    {
        foreach (Transform child in gridObjectCollection.transform)
        {
            child.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Updates clipping of out of sight entries.
    /// </summary>
    public void updateClipping()
    {
        if (gameObject.activeSelf)
        {
            var cb = clippingBox.GetComponent<ClippingBox>();
            foreach (Transform child in gridObjectCollection.transform)
            {
                var renderers = child.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    cb.AddRenderer(renderer);
                }
            }
        }
    }

    /// <summary>
    /// Smoothly moves the scroll container a relative number of tiers of cells.
    /// </summary>
    public void ScrollByTier(int amount)
    {
        var scrollView = scrollingObjectCollection.GetComponent<ScrollingObjectCollection>();
        Debug.Assert(scrollView != null, "Scroll view needs to be defined before using pagination.");
        scrollView.MoveByTiers(amount);
        scrollUpdate();
    }

    /// <summary>
    /// Implements a short waiting period so the scrolling animation runs smoothly.
    /// </summary>
    public void scrollUpdate()
    {
        StartCoroutine(waitAndUpdate());
    }

    private IEnumerator waitAndUpdate()
    {
        yield return new WaitForSeconds(0.25f);
        scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
    }
}
