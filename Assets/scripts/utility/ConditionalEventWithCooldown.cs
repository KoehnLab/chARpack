using System;
using System.Collections.Generic;
using UnityEngine;

namespace chARpack
{
    public class ConditionalEventWithCooldown
    {
        public delegate void ConditionalEventHandler();

        private List<(ConditionalEventHandler Handler, Func<bool> BoundsCheck)> _handlers = new List<(ConditionalEventHandler, Func<bool>)>();
        private ConditionalEventHandler _defaultHandler;
        private float _cooldownDuration;
        private float _lastInvokeTime;
        private bool _isOnCooldown;

        public ConditionalEventWithCooldown(float cooldownDuration = 0f)
        {
            _cooldownDuration = cooldownDuration;
        }

        public void AddListener(ConditionalEventHandler handler, Func<bool> boundsCheck)
        {
            _handlers.Add((handler, boundsCheck));
        }

        public void RemoveListener(ConditionalEventHandler handler)
        {
            _handlers.RemoveAll(h => h.Handler == handler);
        }

        public void SetDefaultListener(ConditionalEventHandler handler)
        {
            _defaultHandler = handler;
        }

        public void RemoveDefaultListener()
        {
            _defaultHandler = delegate { };
        }

        public void Invoke()
        {
            if (_isOnCooldown)
                return;

            _isOnCooldown = true;
            _lastInvokeTime = Time.time;

            foreach (var (handler, boundsCheck) in _handlers)
            {
                if (boundsCheck())
                {
                    handler();
                    _isOnCooldown = false; // Reset cooldown if a handler is executed
                    return; // Stop further execution
                }
            }

            _defaultHandler?.Invoke(); // Execute the default handler if no other handlers were executed
            _isOnCooldown = false; // Reset cooldown if the default handler is executed
        }

        public void UpdateCooldown()
        {
            if (_isOnCooldown && Time.time - _lastInvokeTime >= _cooldownDuration)
            {
                _isOnCooldown = false;
            }
        }
    }
}