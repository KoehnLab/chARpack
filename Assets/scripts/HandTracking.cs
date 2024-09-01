using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.VFX;

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


    GameObject wipeVFX;
    Camera currentCam;
    private void Start()
    {
        middleInBoxClip = Resources.Load<AudioClip>("audio/middleInBox");
        var wipe_vfx_prefab = Resources.Load<GameObject>("vfxGraph/explosionEffect");
        wipeVFX = Instantiate(wipe_vfx_prefab);
        wipeVFX.GetComponent<VisualEffect>().Stop();
        gameObject.AddComponent<AudioSource>();
        showFragmentIndicator(false);
        particleSystemGO.SetActive(false);
        currentCam = Camera.main;
        //gameObject.SetActive(false);
    }


    public void playWipeVFX()
    {
        wipeVFX.transform.position = indexTipPose.Position;
        wipeVFX.GetComponent<VisualEffect>().Play();
    }

    //private MixedRealityPose indexTip = MixedRealityPose.ZeroIdentity;

    private Vector3 _indexForward = Vector3.zero;
    public Vector3 indexForward { get => _indexForward; private set => _indexForward = value; }
    private MixedRealityPose indexKnucklePose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose indexTipPose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose indexMiddlePose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose indexDistalPose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose middleTipPose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose middleMiddlePose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose middleDistalPose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose middleKnucklePose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose thumbTipPose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose thumbDistalPose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose thumbProximalPose = MixedRealityPose.ZeroIdentity;
    private MixedRealityPose wristPose = MixedRealityPose.ZeroIdentity;
    public GameObject fragmentIndicator;
    public GameObject particleSystemGO;
    private bool middleFingerGrab = false;
    private bool indexFingerGrab = false;
    private bool isMiddleInBox = false;
    private AudioClip middleInBoxClip;
    private Vector3 handVelocity = Vector3.zero;
    private Vector3 indexFingerVelocity = Vector3.zero;
    private Handedness currentHand = Handedness.None;

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
    public ConditionalEventWithCooldown OnEmptyIndexFingerGrab = new ConditionalEventWithCooldown(0.2f);
    public ConditionalEventWithCooldown OnEmptyCloseIndexFingerGrab = new ConditionalEventWithCooldown(0.2f);
    public ConditionalEventWithCooldown OnFlick = new ConditionalEventWithCooldown(0.4f);
    public ConditionalEventWithCooldown OnCatch = new ConditionalEventWithCooldown(0.4f);

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

    private void Update()
    {
        currentHand = getPose();
        if (currentHand == Handedness.None) return;
        if (indexForward == Vector3.zero) return;
        transform.forward = indexForward;
        transform.position = indexKnucklePose.Position;

        //if (Vector3.Distance(indexTipPose.Position, thumbTipPose.Position) < 0.025f)
        if (GestureUtils.IsIndexPinching(currentHand))
        {
            if (!indexFingerGrab)
            {
                indexFingerGrab = true;
                IndexFingerGrab(indexTipPose.Position);
                if (!SettingsData.handRay)
                {
                    OnEmptyIndexFingerGrab.Invoke();
                    OnEmptyCloseIndexFingerGrab.Invoke();
                }
            }
        }
        else
        {
            if (indexFingerGrab)
            {
                indexFingerGrab = false;
                StartCoroutine(checkForFlick());
                IndexFingerGrabRelease();
            }
        }

        //if (Vector3.Distance(middleTipPose.Position, thumbTipPose.Position) < 0.025f && Vector3.Distance(indexTipPose.Position, thumbTipPose.Position) > 0.025f)
        if (GestureUtils.IsMiddlePinching(currentHand) && !indexFingerGrab)
        {
            if (!middleFingerGrab)
            {
                middleFingerGrab = true;
                OnMiddleFingerGrab.Invoke();
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

        if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.CATCH))
        {
            var index_inner = indexMiddlePose.Position - indexKnucklePose.Position;
            var index_outer = indexTipPose.Position - indexDistalPose.Position;

            var middle_inner = middleMiddlePose.Position - middleKnucklePose.Position;
            var middle_outer = middleTipPose.Position - middleDistalPose.Position;

            var thumb_inner = thumbDistalPose.Position - thumbProximalPose.Position;
            var thumb_outer = thumbTipPose.Position - thumbDistalPose.Position;

            var angle_threshold = 10f;

            if (Vector3.Angle(index_inner, index_outer) < angle_threshold && Vector3.Angle(middle_inner, middle_outer) < angle_threshold && Vector3.Angle(thumb_inner, thumb_outer) < 2f*angle_threshold)
            {
                OnCatch.Invoke();
            }
        }


        if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.DISTANT_GRAB))
        {
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
    }


    IEnumerator checkForFlick()
    {
        float time = 0f;
        while (time < 0.1f)
        {
            var index_inner = indexMiddlePose.Position - indexKnucklePose.Position;
            var index_outer = indexTipPose.Position - indexDistalPose.Position;
            if (Vector3.Angle(index_inner, index_outer) < 15f)
            {
                OnFlick.Invoke();
                playWipeVFX();
                yield break;
            }
            time += Time.deltaTime;
            yield return null;
        }
    }


    public bool isIndexGrabbed()
    {
        return indexFingerGrab;
    }

    private Handedness getPose()
    {
        var current_hand = Handedness.None;
        if (SettingsData.handedness == Handedness.Both)
        {
            if (HandJointService.IsHandTracked(Handedness.Left) && HandJointService.IsHandTracked(Handedness.Right)) // both hands are currently tracked
            {
                // Find which hand is closer to the head view ray
                var leftIndexKnuckle = HandJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, Handedness.Left);
                var rightIndexKnuckle = HandJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, Handedness.Right);

                var leftDist = chARpackUtils.distanceToHeadRay(leftIndexKnuckle.position, currentCam);
                var rightDist = chARpackUtils.distanceToHeadRay(rightIndexKnuckle.position, currentCam);

                if (leftDist < rightDist)
                {
                    current_hand = Handedness.Left;
                }
                else
                {
                    current_hand = Handedness.Right;
                }
            }
            else // only one hand is currently tracked: find out which
            {
                if (HandJointService.IsHandTracked(Handedness.Right))
                {
                    current_hand = Handedness.Right;
                }
                else if (HandJointService.IsHandTracked(Handedness.Left))
                {
                    current_hand = Handedness.Left;
                }
                else // no hand is tracked
                {
                    return Handedness.None;
                }
            }
        }
        else
        {
            if (!HandJointService.IsHandTracked(current_hand)) return Handedness.None;
        }

        //var palmTransform = HandJointService.RequestJointTransform(TrackedHandJoint.Palm, current_hand);

        var indexTipTransform = HandJointService.RequestJointTransform(TrackedHandJoint.IndexTip, current_hand);
        indexFingerVelocity = indexTipTransform.position - indexTipPose.Position;
        indexTipPose = new MixedRealityPose(indexTipTransform.position, indexTipTransform.rotation);

        var indexKnuckleTransform = HandJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, current_hand);
        handVelocity = indexKnuckleTransform.position - indexKnucklePose.Position;
        indexKnucklePose = new MixedRealityPose(indexKnuckleTransform.position, indexKnuckleTransform.rotation);

        var indexMiddleTransform = HandJointService.RequestJointTransform(TrackedHandJoint.IndexMiddleJoint, current_hand);
        indexMiddlePose = new MixedRealityPose(indexMiddleTransform.position, indexMiddleTransform.rotation);

        var indexDistalTransform = HandJointService.RequestJointTransform(TrackedHandJoint.IndexDistalJoint, current_hand);
        indexDistalPose = new MixedRealityPose(indexDistalTransform.position, indexDistalTransform.rotation);

        var middleTipTransform = HandJointService.RequestJointTransform(TrackedHandJoint.MiddleTip, current_hand);
        middleTipPose = new MixedRealityPose(middleTipTransform.position, middleTipTransform.rotation);

        var middleMiddleTransform = HandJointService.RequestJointTransform(TrackedHandJoint.MiddleMiddleJoint, current_hand);
        middleMiddlePose = new MixedRealityPose(middleMiddleTransform.position, middleMiddleTransform.rotation);

        var middleDistalTransform = HandJointService.RequestJointTransform(TrackedHandJoint.MiddleDistalJoint, current_hand);
        middleDistalPose = new MixedRealityPose(middleDistalTransform.position, middleDistalTransform.rotation);

        var middleKnuckleTransform = HandJointService.RequestJointTransform(TrackedHandJoint.MiddleKnuckle, current_hand);
        middleKnucklePose = new MixedRealityPose(middleKnuckleTransform.position, middleKnuckleTransform.rotation);

        var thumbTipTransform = HandJointService.RequestJointTransform(TrackedHandJoint.ThumbTip, current_hand);
        thumbTipPose = new MixedRealityPose(thumbTipTransform.position, thumbTipTransform.rotation);

        var thumbDistalTransform = HandJointService.RequestJointTransform(TrackedHandJoint.ThumbDistalJoint, current_hand);
        thumbDistalPose = new MixedRealityPose(thumbDistalTransform.position, thumbDistalTransform.rotation);

        var thumbProximalTransform = HandJointService.RequestJointTransform(TrackedHandJoint.ThumbProximalJoint, current_hand);
        thumbProximalPose = new MixedRealityPose(thumbProximalTransform.position, thumbProximalTransform.rotation);

        var wristTransform = HandJointService.RequestJointTransform(TrackedHandJoint.Wrist, current_hand);
        wristPose = new MixedRealityPose(wristTransform.position, wristTransform.rotation);

        indexForward = Vector3.Normalize(indexTipPose.Position - indexKnucklePose.Position);

        return current_hand;
    }


    public Handedness getCurrentHand()
    {
        return currentHand;
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

    public Pose getWristPose()
    {
        var pose = new Pose();
        pose.position = wristPose.Position;
        pose.rotation = wristPose.Rotation;
        return pose;
    }

    public Pose getMiddleKnucklePose()
    {
        var pose = new Pose();
        pose.position = middleKnucklePose.Position;
        pose.rotation = middleKnucklePose.Rotation;
        return pose;
    }

    public Vector3 getHandVelocity()
    {
        return handVelocity;
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
