using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// This class provides a hand tracking functionality used in the chain interaction mode.
/// </summary>
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
                UnityEngine.Debug.Log($"[{nameof(HandTracking)}] Instance already exists, destroying duplicate!");
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
        showFragmentIndicator(false);
        gameObject.SetActive(false);
    }

    //private MixedRealityPose indexTip = MixedRealityPose.ZeroIdentity;

    private Vector3 _indexForward = Vector3.zero;
    public Vector3 indexForward { get => _indexForward; private set => _indexForward = value; }
    private MixedRealityPose indexKnucklePose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose indexTipPose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose middleTipPose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose thumbTipPose = MixedRealityPose.ZeroIdentity;
    public GameObject fragmentIndicator;
    bool middleFingerGrab = false;
    bool indexFingerGrab = false;

    public void showFragmentIndicator(bool show)
    {
        if (fragmentIndicator != null)
        {
            fragmentIndicator.SetActive(show);
        }
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


    public delegate void MiddleFingerGrabAction(Vector3 mfpos);
    public event MiddleFingerGrabAction OnMiddleFingerGrab;
    public void MiddleFingerGrab(Vector3 mfpos)
    {
        OnMiddleFingerGrab?.Invoke(mfpos);
    }

    public delegate void MiddleFingerGrabReleaseAction();
    public event MiddleFingerGrabReleaseAction OnMiddleFingerGrabRelease;
    public void MiddleFingerGrabRelease()
    {
        OnMiddleFingerGrabRelease?.Invoke();
    }

    public delegate void IndexFingerGrabAction(Vector3 ifpos);
    public event IndexFingerGrabAction OnIndexFingerGrab;
    public void IndexFingerGrab(Vector3 ifpos)
    {
        OnIndexFingerGrab?.Invoke(ifpos);
    }

    public delegate void IndexFingerGrabReleaseAction();
    public event IndexFingerGrabReleaseAction OnIndexFingerGrabRelease;
    public void IndexFingerGrabRelease()
    {
        OnIndexFingerGrabRelease?.Invoke();
    }

    Stopwatch middleFingerGrabCooldown = new Stopwatch();
    private void Update()
    {
        getPose();
        if (indexForward == Vector3.zero) return;
        transform.forward = indexForward;
        transform.position = indexKnucklePose.Position;
        if (Vector3.Distance(middleTipPose.Position, thumbTipPose.Position) < 0.02f)
        {
            if (!middleFingerGrab)
            {
                middleFingerGrabCooldown.Stop();
                if (middleFingerGrabCooldown.ElapsedMilliseconds > 200)
                {
                    middleFingerGrab = true;
                    MiddleFingerGrab(middleTipPose.Position);
                    middleFingerGrabCooldown.Restart();
                }
                else
                {
                    middleFingerGrabCooldown.Start();
                }
            }
        }
        else
        {
            if (middleFingerGrab)
            {
                middleFingerGrab = false;
                MiddleFingerGrabRelease();
            }
        }

        if (Vector3.Distance(indexTipPose.Position, thumbTipPose.Position) < 0.02f)
        {
            if (!indexFingerGrab)
            {
                indexFingerGrab = true;
                IndexFingerGrab(middleTipPose.Position);
            }
        }
        else
        {
            if (indexFingerGrab)
            {
                indexFingerGrab = false;
                IndexFingerGrabRelease();
            }
        }
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
                        if (hand.ControllerHandedness != SettingsData.handedness) return;
                        if (hand.TryGetJoint(TrackedHandJoint.MiddleTip, out MixedRealityPose MTpose))
                        {
                            middleTipPose = MTpose;
                        }
                        if (hand.TryGetJoint(TrackedHandJoint.ThumbTip, out MixedRealityPose TTpose))
                        {
                            thumbTipPose = TTpose;
                        }
                        if (hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose ITpose))
                        {
                            indexTipPose = ITpose;
                        }
                        if (hand.TryGetJoint(TrackedHandJoint.IndexKnuckle, out MixedRealityPose IKpose))
                        {
                            indexKnucklePose = IKpose;
                        }
                        indexForward = Vector3.Normalize(indexTipPose.Position - indexKnucklePose.Position);
                    }
                    else
                    {
                        var handVisualizer = p.Controller as IMixedRealityHandVisualizer;
                        if (handVisualizer != null)
                        {
                            if (handVisualizer.Controller.ControllerHandedness != SettingsData.handedness) return;
                            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Both, out MixedRealityPose MTpose))
                            {
                                middleTipPose = MTpose;
                            }
                            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Both, out MixedRealityPose TTpose))
                            {
                                thumbTipPose = TTpose;
                            }
                            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Both, out MixedRealityPose ITpose))
                            {
                                indexTipPose = ITpose;
                            }
                            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexKnuckle, Handedness.Both, out MixedRealityPose IKpose))
                            {
                                indexKnucklePose = IKpose;
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

    public Vector3 getIndexKnuckle()
    {
        return indexKnucklePose.Position;
    }
}
