using System;
using System.Collections.Generic;

namespace chARpack
{
    public class ConditionalEvent
    {
        public delegate void ConditionalEventHandler();

        private List<(ConditionalEventHandler Handler, Func<bool> BoundsCheck)> _handlers = new List<(ConditionalEventHandler, Func<bool>)>();
        private ConditionalEventHandler _defaultHandler;

        // Add a handler with its bounds check function
        public void AddListener(ConditionalEventHandler handler, Func<bool> boundsCheck)
        {
            _handlers.Add((handler, boundsCheck));
        }

        // Remove a handler
        public void RemoveListener(ConditionalEventHandler handler)
        {
            _handlers.RemoveAll(h => h.Handler == handler);
        }

        // Set a default handler to be executed if no other handlers are invoked
        public void SetDefaultListener(ConditionalEventHandler handler)
        {
            _defaultHandler = handler;
        }

        // Invoke the event
        public void Invoke()
        {
            foreach (var (handler, boundsCheck) in _handlers)
            {
                if (boundsCheck())
                {
                    handler(); // Execute the handler if the bounds check passes
                    return; // Stop further execution
                }
            }

            _defaultHandler?.Invoke(); // Execute the default handler if no other handlers were executed
        }
    }

    public class ConditionalEvent<T1>
    {
        public delegate void ConditionalEventHandler(T1 arg1);

        private List<(ConditionalEventHandler Handler, Func<bool> BoundsCheck)> _handlers = new List<(ConditionalEventHandler, Func<bool>)>();
        private ConditionalEventHandler _defaultHandler;

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

        public void Invoke(T1 arg1)
        {
            foreach (var (handler, boundsCheck) in _handlers)
            {
                if (boundsCheck())
                {
                    handler(arg1);
                    return;
                }
            }

            _defaultHandler?.Invoke(arg1);
        }
    }

    public class ConditionalEvent<T1, T2>
    {
        // Define the delegate with parameters
        public delegate void ConditionalEventHandler(T1 arg1, T2 arg2);

        // Store handlers and their associated bounds check functions
        private List<(ConditionalEventHandler Handler, Func<bool> BoundsCheck)> _handlers = new List<(ConditionalEventHandler, Func<bool>)>();
        private ConditionalEventHandler _defaultHandler;

        // Add a handler with its bounds check function
        public void AddListener(ConditionalEventHandler handler, Func<bool> boundsCheck)
        {
            _handlers.Add((handler, boundsCheck));
        }

        // Remove a handler
        public void RemoveListener(ConditionalEventHandler handler)
        {
            _handlers.RemoveAll(h => h.Handler == handler);
        }

        // Set a default handler to be executed if no other handlers are invoked
        public void SetDefaultListener(ConditionalEventHandler handler)
        {
            _defaultHandler = handler;
        }

        // Invoke the event with arguments
        public void Invoke(T1 arg1, T2 arg2)
        {
            foreach (var (handler, boundsCheck) in _handlers)
            {
                if (boundsCheck())
                {
                    handler(arg1, arg2); // Execute the handler with arguments if the bounds check passes
                    return; // Stop further execution
                }
            }

            _defaultHandler?.Invoke(arg1, arg2); // Execute the default handler with arguments if no other handlers were executed
        }
    }
}
