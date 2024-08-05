using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
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
        middleInBoxClip = Resources.Load<AudioClip>("audio/middleInBox");
        gameObject.AddComponent<AudioSource>();
        showFragmentIndicator(false);
        particleSystemGO.SetActive(false);
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
    public GameObject particleSystemGO;
    bool middleFingerGrab = false;
    bool indexFingerGrab = false;
    bool isMiddleInBox = false;
    private AudioClip middleInBoxClip;

    private IMixedRealityHandJointService handJointService;

    private IMixedRealityHandJointService HandJointService =>
        handJointService ??
        (handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>());

    private MixedRealityPose? previousLeftHandPose;

    private MixedRealityPose? previousRightHandPose;


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


    public ConditionalEventWithCooldown OnMiddleFingerGrab = new ConditionalEventWithCooldown(0.2f);

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

        //if (Vector3.Distance(indexTipPose.Position, thumbTipPose.Position) < 0.025f)
        if (GestureUtils.IsIndexPinching(SettingsData.handedness))
        {
            if (!indexFingerGrab)
            {
                indexFingerGrab = true;
                IndexFingerGrab(indexTipPose.Position);
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

        //if (Vector3.Distance(middleTipPose.Position, thumbTipPose.Position) < 0.025f && Vector3.Distance(indexTipPose.Position, thumbTipPose.Position) > 0.025f)
        if (GestureUtils.IsMiddlePinching(SettingsData.handedness) && !indexFingerGrab)
        {
            if (!middleFingerGrab)
            {
                middleFingerGrabCooldown.Stop();
                if (middleFingerGrabCooldown.ElapsedMilliseconds > 200)
                {
                    middleFingerGrab = true;
                    //MiddleFingerGrab(middleTipPose.Position);
                    OnMiddleFingerGrab.Invoke();
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



        // check if middle in bounds
        List<Bounds> boundsInScene = new List<Bounds>();
        if (GenericObject.objects != null)
        {
            foreach (var obj in GenericObject.objects.Values)
            {
                if (obj.getIsInteractable())
                {
                    boundsInScene.Add(obj.GetComponent<myBoundingBox>().localBounds);
                }
            }
        }
        if (GlobalCtrl.Singleton != null)
        {
            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
            {
                if (mol.getIsInteractable())
                {
                    boundsInScene.Add(mol.GetComponent<myBoundingBox>().localBounds);
                }
            }
        }
        bool contained_in_any = false;
        foreach (var bound in boundsInScene)
        {
            if (bound.Contains(middleTipPose.Position))
            {
                contained_in_any = true;
                break;
            }
        }
        if (contained_in_any && !isMiddleInBox)
        {
            isMiddleInBox = true;
            particleSystemGO.SetActive(true);
            particleSystemGO.GetComponent<ParticleSystem>().Play();
            //AudioSource loopSound = GetComponent<AudioSource>();
            //loopSound.clip = middleInBoxClip;
            //loopSound.loop = true;
            //loopSound.Play();
        }
        if (!contained_in_any && isMiddleInBox)
        {
            isMiddleInBox = false;
            particleSystemGO.GetComponent<ParticleSystem>().Stop();
            particleSystemGO.SetActive(false);
            //GetComponent<AudioSource>().Stop();
        }
        if (isMiddleInBox)
        {
            particleSystemGO.transform.position = middleTipPose.Position;
        }
    }

    public bool isIndexGrabbed()
    {
        return indexFingerGrab;
    }

    private void getPose()
    {

        if (HandJointService.IsHandTracked(SettingsData.handedness))
        {
            //var palmTransform = HandJointService.RequestJointTransform(TrackedHandJoint.Palm, SettingsData.handedness);

            var indexTopTransform = HandJointService.RequestJointTransform(TrackedHandJoint.IndexTip, SettingsData.handedness);
            indexTipPose = new MixedRealityPose(indexTopTransform.position, indexTopTransform.rotation);

            var indexKnuckleTransform = HandJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, SettingsData.handedness);
            indexKnucklePose = new MixedRealityPose(indexKnuckleTransform.position, indexKnuckleTransform.rotation);

            var middleTipTransform = HandJointService.RequestJointTransform(TrackedHandJoint.MiddleTip, SettingsData.handedness);
            middleTipPose = new MixedRealityPose(middleTipTransform.position, middleTipTransform.rotation);

            var thumbTipTransform = HandJointService.RequestJointTransform(TrackedHandJoint.ThumbTip, SettingsData.handedness);
            thumbTipPose = new MixedRealityPose(thumbTipTransform.position, thumbTipTransform.rotation);

            indexForward = Vector3.Normalize(indexTipPose.Position - indexKnucklePose.Position);

        }


        //currentTrackedHandedness = TrackedHandedness;
        //if (currentTrackedHandedness == Handedness.Both)
        //{
        //    if (HandJointService.IsHandTracked(PreferredTrackedHandedness))
        //    {
        //        currentTrackedHandedness = PreferredTrackedHandedness;
        //    }


        //foreach (var source in CoreServices.InputSystem.DetectedInputSources)
        //{
        // Ignore anything that is not a hand because we want articulated hands
        //if (source.SourceType == InputSourceType.Hand)
        //{
        //        foreach (var p in source.Pointers)
        //    {
        //var hand = p.Controller as IMixedRealityHand;
        //if (hand != null)
        //{
        //    if (hand.ControllerHandedness != SettingsData.handedness || hand.ControllerHandedness != Handedness.Both) return;
        //    if (hand.TryGetJoint(TrackedHandJoint.MiddleTip, out MixedRealityPose MTpose))
        //    {
        //        middleTipPose = MTpose;
        //    }
        //    if (hand.TryGetJoint(TrackedHandJoint.ThumbTip, out MixedRealityPose TTpose))
        //    {
        //        thumbTipPose = TTpose;
        //    }
        //    if (hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose ITpose))
        //    {
        //        indexTipPose = ITpose;
        //    }
        //    if (hand.TryGetJoint(TrackedHandJoint.IndexKnuckle, out MixedRealityPose IKpose))
        //    {
        //        indexKnucklePose = IKpose;
        //    }
        //    indexForward = Vector3.Normalize(indexTipPose.Position - indexKnucklePose.Position);
        //}
        //else
        //{
        //}
        //var handVisualizer = p.Controller as IMixedRealityHandVisualizer;
        //if (handVisualizer != null)
        //{
        //    if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Both, out MixedRealityPose MTpose))
        //    {
        //        middleTipPose = MTpose;
        //    }
        //    if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Both, out MixedRealityPose TTpose))
        //    {
        //        thumbTipPose = TTpose;
        //    }
        //    if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Both, out MixedRealityPose ITpose))
        //    {
        //        indexTipPose = ITpose;
        //    }
        //    if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexKnuckle, Handedness.Both, out MixedRealityPose IKpose))
        //    {
        //        indexKnucklePose = IKpose;
        //    }
        //    indexForward = Vector3.Normalize(indexTipPose.Position - indexKnucklePose.Position);
        //}
        //}
        //}
        //}
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

    public Vector3 getMiddleTip()
    {
        return middleTipPose.Position;
    }
}

public static class GestureUtils
{
    private const float PinchThreshold = 0.7f;
    private const float GrabThreshold = 0.4f;

    public static bool IsIndexPinching(Handedness trackedHand)
    {
        return CalculateIndexPinch(trackedHand) > PinchThreshold;
    }

    public static bool IsMiddlePinching(Handedness trackedHand)
    {
        return CalculateMiddlePinch(trackedHand) > PinchThreshold;
    }

    public static bool IsGrabbing(Handedness trackedHand)
    {
        return !IsIndexPinching(trackedHand) &&
               HandPoseUtils.MiddleFingerCurl(trackedHand) > GrabThreshold &&
               HandPoseUtils.RingFingerCurl(trackedHand) > GrabThreshold &&
               HandPoseUtils.PinkyFingerCurl(trackedHand) > GrabThreshold &&
               HandPoseUtils.ThumbFingerCurl(trackedHand) > GrabThreshold;
    }

    /// <summary>
    /// Pinch calculation of the index finger with the thumb based on the distance between the finger tip and the thumb tip.
    /// 4 cm (0.04 unity units) is the threshold for fingers being far apart and pinch being read as 0.
    /// </summary>
    /// <param name="handedness">Handedness to query joint pose against.</param>
    /// <returns> Float ranging from 0 to 1. 0 if the thumb and finger are not pinched together, 1 if thumb finger are pinched together</returns>

    private const float IndexThumbSqrMagnitudeThreshold = 0.0016f;
    public static float CalculateIndexPinch(Handedness handedness)
    {
        HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, handedness, out var indexPose);
        HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out var thumbPose);

        Vector3 distanceVector = indexPose.Position - thumbPose.Position;
        float indexThumbSqrMagnitude = distanceVector.sqrMagnitude;

        float pinchStrength = Mathf.Clamp(1 - indexThumbSqrMagnitude / IndexThumbSqrMagnitudeThreshold, 0.0f, 1.0f);
        return pinchStrength;
    }

    public static float CalculateMiddlePinch(Handedness handedness)
    {
        HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, handedness, out var middlePose);
        HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out var thumbPose);

        Vector3 distanceVector = middlePose.Position - thumbPose.Position;
        float middleThumbSqrMagnitude = distanceVector.sqrMagnitude;

        float pinchStrength = Mathf.Clamp(1 - middleThumbSqrMagnitude / IndexThumbSqrMagnitudeThreshold, 0.0f, 1.0f);
        return pinchStrength;
    }
}
