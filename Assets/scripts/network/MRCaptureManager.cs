using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.WebCam;

public class MRCaptureManager : MonoBehaviour
{
    static readonly float MaxRecordingTime = 5.0f;


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


    VideoCapture m_VideoCapture = null;
    float m_stopRecordingTimer = float.MaxValue;


    public void setRecording(bool record)
    {
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
    }

    public bool isRecording()
    {
        if (m_VideoCapture != null)
        {
            return m_VideoCapture.IsRecording;
        }
        else
        {
            return false;
        }

    }

    public void StartVideoCapture()
    {

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
    }

    void OnStartedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("[MRCaptureManager] MR Capture Started!");
        string timeStamp = Time.time.ToString().Replace(".", "").Replace(":", "");
        string filename = string.Format("chARp_capture_{0}.mp4", timeStamp);
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

}
