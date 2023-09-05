using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandTracking : MonoBehaviour
{
    private static HandTracking _singleton;

    public static HandTracking Singleton
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
                Debug.Log($"[{nameof(HandTracking)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }


    private void Awake()
    {
        Singleton = this;
        gameObject.SetActive(false);
    }

    //private MixedRealityPose indexTip = MixedRealityPose.ZeroIdentity;

    private Vector3 _indexForward = Vector3.zero;
    public Vector3 indexForward { get => _indexForward; private set => _indexForward = value; }
    private MixedRealityPose indexKnucklePose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose indexTipPose = MixedRealityPose.ZeroIdentity;
    public GameObject chainIndicator;

    public void showVisual(bool show)
    {
        chainIndicator.SetActive(show);
    }

    // check hand pose to pick which end of chain to move
    //public void OnSourceDetected(SourceStateEventData eventData)
    //{
    //    var hand = eventData.Controller as IMixedRealityHand;
    //    if (hand != null)
    //    {
    //        Debug.Log("[HandTracking] Controller found");
    //        if (hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPose))
    //        {
    //            indexForward = jointPose.Forward;
    //        }
    //    } else {
    //        var handVisualizer = eventData.Controller.Visualizer as IMixedRealityHandVisualizer;
    //        if (handVisualizer != null)
    //        {
    //            Debug.Log("[HandTracking] Visualizer found");
    //            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Both, out MixedRealityPose jointPose))
    //            {
    //                indexForward = jointPose.Forward;
    //            }
    //            //if (handVisualizer.TryGetJointTransform(TrackedHandJoint.IndexTip, out Transform jointTransform))
    //            //{
    //            //    indexForward = jointTransform.forward;
    //            //}
    //        }
    //    }
    //}

    private void Update()
    {
        getPose();
        transform.forward = indexForward;
        transform.position = indexKnucklePose.Position;
    }


    private void getPose()
    {
        foreach (var source in CoreServices.InputSystem.DetectedInputSources)
        {
            // Ignore anything that is not a hand because we want articulated hands
            if (source.SourceType == InputSourceType.Hand)
            {
                foreach (var p in source.Pointers)
                {
                    var hand = p.Controller as IMixedRealityHand;
                    if (hand != null)
                    {
                        if (hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose tipPose))
                        {
                            indexTipPose = tipPose;
                        }
                        if (hand.TryGetJoint(TrackedHandJoint.IndexKnuckle, out MixedRealityPose middlePose))
                        {
                            indexKnucklePose = middlePose;
                        }
                        indexForward = Vector3.Normalize(indexTipPose.Position - indexKnucklePose.Position);
                    }
                    else
                    {
                        var handVisualizer = p.Controller as IMixedRealityHandVisualizer;
                        if (handVisualizer != null)
                        {
                            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Both, out MixedRealityPose tipPose))
                            {
                                indexTipPose = tipPose;
                            }
                            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexKnuckle, Handedness.Both, out MixedRealityPose middlePose))
                            {
                                indexKnucklePose = middlePose;
                            }
                            indexForward = Vector3.Normalize(indexTipPose.Position - indexKnucklePose.Position);
                        }
                    }
                }
            }
        }
    }

    public Vector3 getForward()
    {
        return indexForward;
    }

    public Vector3 getIndexTip()
    {
        return indexTipPose.Position;
    }
}
