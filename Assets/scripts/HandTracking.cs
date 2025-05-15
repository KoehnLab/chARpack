#if CHARPACK_MRTK_2_8
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using TrackedHandJoint = Microsoft.MixedReality.Toolkit.Utilities.TrackedHandJoint;
using MRTKHandedness = Microsoft.MixedReality.Toolkit.Utilities.Handedness;
using HandPoseUtils = Microsoft.MixedReality.Toolkit.Utilities.HandPoseUtils;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System;
using UnityEngine.SceneManagement;


namespace chARpack
{
    public static class HandTrackingExtensions
    {
#if CHARPACK_MRTK_2_8
        public static MRTKHandedness toMRTK(this HandTracking.Handedness input)
        {
            return (MRTKHandedness)(int)input;
        }

        public static HandTracking.Handedness toCharpack(this MRTKHandedness input)
        {
            return (HandTracking.Handedness)(int)input;
        }
#endif
    }

    /// <summary>
    /// This class provides a hand tracking functionality used in the chain interaction mode.
    /// </summary>
    public class HandTracking : MonoBehaviour
    {

        [Flags]
        public enum Handedness : byte
        {
            /// <summary>
            /// No hand specified by the SDK for the controller
            /// </summary>
            None = 0 << 0,
            /// <summary>
            /// The controller is identified as being provided in a Left hand
            /// </summary>
            Left = 1 << 0,
            /// <summary>
            /// The controller is identified as being provided in a Right hand
            /// </summary>
            Right = 1 << 1,
            /// <summary>
            /// The controller is identified as being either left and/or right handed.
            /// </summary>
            Both = Left | Right,
            /// <summary>
            /// Reserved, for systems that provide alternate hand state.
            /// </summary>
            Other = 1 << 2,
            /// <summary>
            /// Global catchall, used to map actions to any controller (provided the controller supports it)
            /// </summary>
            /// <remarks>Note, by default the specific hand actions will override settings mapped as both</remarks>
            Any = Other | Left | Right,
        }

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
            if (!SceneManager.GetActiveScene().name.Contains("Login"))
            {
                wipeVFX.transform.position = indexTipPose.position;
                wipeVFX.GetComponent<VisualEffect>().Play();
            }
        }

        private Vector3 _indexForward = Vector3.zero;
        public Vector3 indexForward { get => _indexForward; private set => _indexForward = value; }
        private Pose indexKnucklePose = Pose.identity;
        private Pose indexTipPose = Pose.identity;
        private Pose indexMiddlePose = Pose.identity;
        private Pose indexDistalPose = Pose.identity;
        private Pose middleTipPose = Pose.identity;
        private Pose middleMiddlePose = Pose.identity;
        private Pose middleDistalPose = Pose.identity;
        private Pose middleKnucklePose = Pose.identity;
        private Pose thumbTipPose = Pose.identity;
        private Pose thumbDistalPose = Pose.identity;
        private Pose thumbProximalPose = Pose.identity;
        private Pose wristPose = Pose.identity;
        public GameObject fragmentIndicator;
        public GameObject particleSystemGO;
        private bool middleFingerGrab = false;
        private bool indexFingerGrab = false;
        private bool isMiddleInBox = false;
        private AudioClip middleInBoxClip;
        private Vector3 handVelocity = Vector3.zero;
        private Vector3 indexFingerVelocity = Vector3.zero;
        private Handedness currentHand = Handedness.None;

#if CHARPACK_MRTK_2_8
        private IMixedRealityHandJointService handJointService;
        private IMixedRealityHandJointService HandJointService =>
            handJointService ??
            (handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>());
#endif

        private Pose? previousLeftHandPose;

        private Pose? previousRightHandPose;


        public void showFragmentIndicator(bool show)
        {
            if (fragmentIndicator != null)
            {
                fragmentIndicator.SetActive(show);
            }
        }

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
#if CHARPACK_MRTK_2_8
            currentHand = getPose();
            if (currentHand == Handedness.None) return;
            if (indexForward == Vector3.zero) return;
            transform.forward = indexForward;
            transform.position = indexKnucklePose.position;

            //if (Vector3.Distance(indexTipPose.Position, thumbTipPose.Position) < 0.025f)
            if (GestureUtils.IsIndexPinching(currentHand.toMRTK()))
            {
                if (!indexFingerGrab)
                {
                    indexFingerGrab = true;
                    IndexFingerGrab(indexTipPose.position);
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
                    if (SettingsData.syncMode == TransitionManager.SyncMode.Async && SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.FLICK))
                    {
                        StartCoroutine(checkForFlick());
                    }
                    IndexFingerGrabRelease();
                }
            }

            //if (Vector3.Distance(middleTipPose.Position, thumbTipPose.Position) < 0.025f && Vector3.Distance(indexTipPose.Position, thumbTipPose.Position) > 0.025f)
            if (GestureUtils.IsMiddlePinching(currentHand.toMRTK()) && !indexFingerGrab)
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
                var index_inner = indexMiddlePose.position - indexKnucklePose.position;
                var index_outer = indexTipPose.position - indexDistalPose.position;

                var middle_inner = middleMiddlePose.position - middleKnucklePose.position;
                var middle_outer = middleTipPose.position - middleDistalPose.position;

                var thumb_inner = thumbDistalPose.position - thumbProximalPose.position;
                var thumb_outer = thumbTipPose.position - thumbDistalPose.position;

                var angle_threshold = 10f;

                if (Vector3.Angle(index_inner, index_outer) < angle_threshold && Vector3.Angle(middle_inner, middle_outer) < angle_threshold && Vector3.Angle(thumb_inner, thumb_outer) < 2f * angle_threshold)
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
                    if (bound.Contains(middleTipPose.position))
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
                    particleSystemGO.transform.position = middleTipPose.position;
                }
            }
#endif
        }


        IEnumerator checkForFlick()
        {
            float time = 0f;
            while (time < 0.1f)
            {
                var index_inner = indexMiddlePose.position - indexKnucklePose.position;
                var index_outer = indexTipPose.position - indexDistalPose.position;
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

#if CHARPACK_MRTK_2_8
        private Handedness getPose()
        {
            var current_hand = MRTKHandedness.None;
            if (SettingsData.handedness == Handedness.Both)
            {
                if (HandJointService.IsHandTracked(MRTKHandedness.Left) && HandJointService.IsHandTracked(MRTKHandedness.Right)) // both hands are currently tracked
                {
                    // Find which hand is closer to the head view ray
                    var leftIndexKnuckle = HandJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, MRTKHandedness.Left);
                    var rightIndexKnuckle = HandJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, MRTKHandedness.Right);

                    var leftDist = chARpackUtils.distanceToHeadRay(leftIndexKnuckle.position, currentCam);
                    var rightDist = chARpackUtils.distanceToHeadRay(rightIndexKnuckle.position, currentCam);

                    if (leftDist < rightDist)
                    {
                        current_hand = MRTKHandedness.Left;
                    }
                    else
                    {
                        current_hand = MRTKHandedness.Right;
                    }
                }
                else // only one hand is currently tracked: find out which
                {
                    if (HandJointService.IsHandTracked(MRTKHandedness.Right))
                    {
                        current_hand = MRTKHandedness.Right;
                    }
                    else if (HandJointService.IsHandTracked(MRTKHandedness.Left))
                    {
                        current_hand = MRTKHandedness.Left;
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
            indexFingerVelocity = indexTipTransform.position - indexTipPose.position;
            indexTipPose = new Pose(indexTipTransform.position, indexTipTransform.rotation);

            var indexKnuckleTransform = HandJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, current_hand);
            handVelocity = indexKnuckleTransform.position - indexKnucklePose.position;
            indexKnucklePose = new Pose(indexKnuckleTransform.position, indexKnuckleTransform.rotation);

            var indexMiddleTransform = HandJointService.RequestJointTransform(TrackedHandJoint.IndexMiddleJoint, current_hand);
            indexMiddlePose = new Pose(indexMiddleTransform.position, indexMiddleTransform.rotation);

            var indexDistalTransform = HandJointService.RequestJointTransform(TrackedHandJoint.IndexDistalJoint, current_hand);
            indexDistalPose = new Pose(indexDistalTransform.position, indexDistalTransform.rotation);

            var middleTipTransform = HandJointService.RequestJointTransform(TrackedHandJoint.MiddleTip, current_hand);
            middleTipPose = new Pose(middleTipTransform.position, middleTipTransform.rotation);

            var middleMiddleTransform = HandJointService.RequestJointTransform(TrackedHandJoint.MiddleMiddleJoint, current_hand);
            middleMiddlePose = new Pose(middleMiddleTransform.position, middleMiddleTransform.rotation);

            var middleDistalTransform = HandJointService.RequestJointTransform(TrackedHandJoint.MiddleDistalJoint, current_hand);
            middleDistalPose = new Pose(middleDistalTransform.position, middleDistalTransform.rotation);

            var middleKnuckleTransform = HandJointService.RequestJointTransform(TrackedHandJoint.MiddleKnuckle, current_hand);
            middleKnucklePose = new Pose(middleKnuckleTransform.position, middleKnuckleTransform.rotation);

            var thumbTipTransform = HandJointService.RequestJointTransform(TrackedHandJoint.ThumbTip, current_hand);
            thumbTipPose = new Pose(thumbTipTransform.position, thumbTipTransform.rotation);

            var thumbDistalTransform = HandJointService.RequestJointTransform(TrackedHandJoint.ThumbDistalJoint, current_hand);
            thumbDistalPose = new Pose(thumbDistalTransform.position, thumbDistalTransform.rotation);

            var thumbProximalTransform = HandJointService.RequestJointTransform(TrackedHandJoint.ThumbProximalJoint, current_hand);
            thumbProximalPose = new Pose(thumbProximalTransform.position, thumbProximalTransform.rotation);

            var wristTransform = HandJointService.RequestJointTransform(TrackedHandJoint.Wrist, current_hand);
            wristPose = new Pose(wristTransform.position, wristTransform.rotation);

            indexForward = Vector3.Normalize(indexTipPose.position - indexKnucklePose.position);

            return current_hand.toCharpack();
        }
#endif


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
            return indexTipPose.position;
        }

        public Vector3 getIndexKnuckle()
        {
            return indexKnucklePose.position;
        }

        public Vector3 getMiddleTip()
        {
            return middleTipPose.position;
        }

        public Pose getWristPose()
        {
            var pose = new Pose();
            pose.position = wristPose.position;
            pose.rotation = wristPose.rotation;
            return pose;
        }

        public Pose getMiddleKnucklePose()
        {
            var pose = new Pose();
            pose.position = middleKnucklePose.position;
            pose.rotation = middleKnucklePose.rotation;
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
#if CHARPACK_MRTK_2_8
        public static bool IsIndexPinching(MRTKHandedness trackedHand)
        {
            return CalculateIndexPinch(trackedHand) > PinchThreshold;
        }

        public static bool IsMiddlePinching(MRTKHandedness trackedHand)
        {
            return CalculateMiddlePinch(trackedHand) > PinchThreshold;
        }

        public static bool IsGrabbing(MRTKHandedness trackedHand)
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
        public static float CalculateIndexPinch(MRTKHandedness handedness)
        {
            HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, handedness, out var indexPose);
            HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out var thumbPose);

            Vector3 distanceVector = indexPose.Position - thumbPose.Position;
            float indexThumbSqrMagnitude = distanceVector.sqrMagnitude;

            float pinchStrength = Mathf.Clamp(1 - indexThumbSqrMagnitude / IndexThumbSqrMagnitudeThreshold, 0.0f, 1.0f);
            return pinchStrength;
        }

        public static float CalculateMiddlePinch(MRTKHandedness handedness)
        {
            HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, handedness, out var middlePose);
            HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out var thumbPose);

            Vector3 distanceVector = middlePose.Position - thumbPose.Position;
            float middleThumbSqrMagnitude = distanceVector.sqrMagnitude;

            float pinchStrength = Mathf.Clamp(1 - middleThumbSqrMagnitude / IndexThumbSqrMagnitudeThreshold, 0.0f, 1.0f);
            return pinchStrength;
        }
#endif
    }
}
