using UnityEngine;
#if WINDOWS_UWP
using UnityEngine.Windows.WebCam;
#endif

namespace chARpack
{
    public class MRCaptureManager : MonoBehaviour
    {
        private static MRCaptureManager _singleton;
        public static MRCaptureManager Singleton
        {
            get => _singleton;
            private set

            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != null)
                {
                    Debug.Log($"{nameof(MRCaptureManager)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }
        public void Awake()
        {
            Singleton = this;
        }

#if WINDOWS_UWP
    VideoCapture m_VideoCapture = null;
#endif

        public void setRecording(bool record)
        {
#if WINDOWS_UWP
        if (record)
        {
            StartVideoCapture();
        }
        else if (!record)
        {
            if (m_VideoCapture != null)
            {
                m_VideoCapture.StopRecordingAsync(OnStoppedRecordingVideo);
            }
        }
#endif
        }

        public bool isRecording()
        {
#if WINDOWS_UWP
        if (m_VideoCapture != null)
        {
            return m_VideoCapture.IsRecording;
        }
#endif
            return false;
        }

        public void StartVideoCapture()
        {
#if WINDOWS_UWP
        Resolution cameraResolution = VideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        Debug.Log(cameraResolution);

        float cameraFramerate = VideoCapture.GetSupportedFrameRatesForResolution(cameraResolution).OrderByDescending((fps) => fps).First();
        Debug.Log(cameraFramerate);

        VideoCapture.CreateAsync(true, delegate (VideoCapture videoCapture)
        {
            if (videoCapture != null)
            {
                m_VideoCapture = videoCapture;

                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 1.0f;
                cameraParameters.frameRate = cameraFramerate;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

                m_VideoCapture.StartVideoModeAsync(cameraParameters,
                                                   VideoCapture.AudioState.ApplicationAndMicAudio,
                                                   OnStartedVideoCaptureMode);
            }
            else
            {
                Debug.LogError("[MRCaptureManager] Failed to create VideoCapture Instance!");
            }
        });
#endif
        }

#if WINDOWS_UWP

    void OnStartedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("[MRCaptureManager] MR Capture Started!");
        string timeStamp = Time.time.ToString().Replace(".", "").Replace(":", "");
        string filename = string.Format("chARpack_capture_{0}.mp4", timeStamp);
        string filepath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        filepath = filepath.Replace("/", @"\");
        m_VideoCapture.StartRecordingAsync(filepath, OnStartedRecordingVideo);
    }

    void OnStoppedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        // empty
    }

    void OnStartedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        // empty
    }

    void OnStoppedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("[MRCaptureManager] MR Capture Stopped!");
        m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);
    }

#endif
    }
}