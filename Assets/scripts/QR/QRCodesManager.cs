﻿using System;
using System.Collections;

using System.Collections.Generic;

using UnityEngine;

#if WINDOWS_UWP
using Microsoft.MixedReality.QR;
#endif

namespace QRTracking
{
    public static class QRCodeEventArgs
    {
        public static QRCodeEventArgs<TData> Create<TData>(TData data)
        {
            return new QRCodeEventArgs<TData>(data);
        }
    }

    [Serializable]
    public class QRCodeEventArgs<TData> : EventArgs
    {
        public TData Data { get; private set; }

        public QRCodeEventArgs(TData data)
        {
            Data = data;
        }
    }

    public class QRCodesManager : MonoBehaviour
    {

        public delegate void QRPoseUpdateAction(Guid qr_id, Pose pose);
        public event QRPoseUpdateAction OnQRPoseUpdate;
        public void QRPoseUpdate(Guid qr_id, Pose pose)
        {
            OnQRPoseUpdate?.Invoke(qr_id, pose);
        }

        private static QRCodesManager _singleton;

        public static QRCodesManager Singleton
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
                    Debug.Log($"[{nameof(QRCodesManager)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }

            }
        }


        [Tooltip("Determines if the QR codes scanner should be automatically started.")]
        public bool AutoStartQRTracking = true;

        public bool IsTrackerRunning { get; private set; }

        public bool IsSupported { get; private set; }
#if WINDOWS_UWP
        public event EventHandler<bool> QRCodesTrackingStateChanged;
        public event EventHandler<QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode>> QRCodeAdded;
        public event EventHandler<QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode>> QRCodeUpdated;
        public event EventHandler<QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode>> QRCodeRemoved;

        public System.Collections.Generic.SortedDictionary<System.Guid, Microsoft.MixedReality.QR.QRCode> qrCodesList = new SortedDictionary<System.Guid, Microsoft.MixedReality.QR.QRCode>();

        private QRCodeWatcher qrTracker;
        private bool capabilityInitialized = false;
        private QRCodeWatcherAccessStatus accessStatus;
        private System.Threading.Tasks.Task<QRCodeWatcherAccessStatus> capabilityTask;

        public System.Guid GetIdForQRCode(string qrCodeData)
        {
            lock (qrCodesList)
            {
                foreach (var ite in qrCodesList)
                {
                    if (ite.Value.Data == qrCodeData)
                    {
                        return ite.Key;
                    }
                }
            }
            return new System.Guid();
        }

        public System.Collections.Generic.IList<Microsoft.MixedReality.QR.QRCode> GetList()
        {
            lock (qrCodesList)
            {
                return new List<Microsoft.MixedReality.QR.QRCode>(qrCodesList.Values);
            }
        }
        protected void Awake()
        {
            Singleton = this;
        }


        // Use this for initialization
        async protected virtual void Start()
        {
            IsSupported = QRCodeWatcher.IsSupported();
            capabilityTask = QRCodeWatcher.RequestAccessAsync();
            accessStatus = await capabilityTask;
            capabilityInitialized = true;
            Debug.Log($"[QRCodesManager] capabilityInitialized {capabilityInitialized}; IsSupported {IsSupported}");
        }
#endif
        private void SetupQRTracking()
        {
#if WINDOWS_UWP
            try
            {
                qrTracker = new QRCodeWatcher();
                IsTrackerRunning = false;
                qrTracker.Added += QRCodeWatcher_Added;
                qrTracker.Updated += QRCodeWatcher_Updated;
                qrTracker.Removed += QRCodeWatcher_Removed;
                qrTracker.EnumerationCompleted += QRCodeWatcher_EnumerationCompleted;
                Debug.Log("[QRCodesManager] Setup complete.");
            }
            catch (Exception ex)
            {
                Debug.Log("[QRCodesManager] Exception starting the tracker " + ex.ToString());
            }

            if (AutoStartQRTracking)
            {
                Debug.Log("[QRCodesManager] Auto starting.");
                StartQRTracking();
            }
#endif
        }

        public void StartQRTracking()
        {
#if WINDOWS_UWP
            if (qrTracker != null && !IsTrackerRunning)
            {
                Debug.Log("[QRCodesManager] Starting QRCodeWatcher");
                try
                {
                    qrTracker.Start();
                    IsTrackerRunning = true;
                    QRCodesTrackingStateChanged?.Invoke(this, true);
                }
                catch(Exception ex)
                {
                    Debug.LogWarning("[QRCodesManager] Starting QRCodeWatcher Exception:" + ex.ToString());
                }
            }
#endif
        }

        public void StopQRTracking()
        {
#if WINDOWS_UWP
            if (IsTrackerRunning)
            {
                IsTrackerRunning = false;
                if (qrTracker != null)
                {
                    qrTracker.Stop();
                    qrCodesList.Clear();
                }

                var handlers = QRCodesTrackingStateChanged;
                if (handlers != null)
                {
                    handlers(this, false);
                }
            }
#endif
        }
#if WINDOWS_UWP
        private void QRCodeWatcher_Removed(object sender, QRCodeRemovedEventArgs args)
        {
            Debug.Log("[QRCodesManager] QRCodeWatcher_Removed");

            bool found = false;
            lock (qrCodesList)
            {
                if (qrCodesList.ContainsKey(args.Code.Id))
                {
                    qrCodesList.Remove(args.Code.Id);
                    found = true;
                }
            }
            if (found)
            {
                var handlers = QRCodeRemoved;
                if (handlers != null)
                {
                    handlers(this, QRCodeEventArgs.Create(args.Code));
                }
            }
        }

        private void QRCodeWatcher_Updated(object sender, QRCodeUpdatedEventArgs args)
        {
            Debug.Log("[QRCodesManager] QRCodeWatcher_Updated");

            bool found = false;
            lock (qrCodesList)
            {
                if (qrCodesList.ContainsKey(args.Code.Id))
                {
                    found = true;
                    qrCodesList[args.Code.Id] = args.Code;
                }
            }
            if (found)
            {
                var handlers = QRCodeUpdated;
                if (handlers != null)
                {
                    handlers(this, QRCodeEventArgs.Create(args.Code));
                }
            }
        }

        private void QRCodeWatcher_Added(object sender, QRCodeAddedEventArgs args)
        {
            Debug.Log("[QRCodesManager] QRCodeWatcher_Added");

            lock (qrCodesList)
            {
                qrCodesList[args.Code.Id] = args.Code;
            }
            var handlers = QRCodeAdded;
            if (handlers != null)
            {
                handlers(this, QRCodeEventArgs.Create(args.Code));
            }
        }

        private void QRCodeWatcher_EnumerationCompleted(object sender, object e)
        {
            Debug.Log("[QRCodesManager] QrTracker_EnumerationCompleted");
        }

        private void Update()
        {
            if (qrTracker == null && capabilityInitialized && IsSupported)
            {
                if (accessStatus == QRCodeWatcherAccessStatus.Allowed)
                {
                    SetupQRTracking();
                }
                else
                {  
                    Debug.Log("[QRCodesManager] Capability access status : " + accessStatus);
                }
            }
        }
#endif
    }
}