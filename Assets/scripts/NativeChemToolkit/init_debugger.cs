using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class init_debugger : MonoBehaviour
{

    // Use this for initialization
    void OnEnable()
    {
        RegisterDebugCallback(OnDebugCallback);
        RegisterWarningCallback(OnWarningCallback);
        RegisterErrorCallback(OnErrorCallback);
        //TestDebug();
        //TestWarn();
        //TestError();
    }

    //------------------------------------------------------------------------------------------------
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    static extern void RegisterDebugCallback(debugCallback cb);
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    static extern void RegisterWarningCallback(warnCallback cb);
    [DllImport("NativeChemToolkit", CallingConvention = CallingConvention.Cdecl)]
    static extern void RegisterErrorCallback(errorCallback cb);
    [DllImport("NativeChemToolkit")]
    static extern void TestDebug();
    [DllImport("NativeChemToolkit")]
    static extern void TestWarn();
    [DllImport("NativeChemToolkit")]
    static extern void TestError();

    //Create string param callback delegate
    delegate void debugCallback(IntPtr request, int color, int size);
    delegate void warnCallback(IntPtr request, int color, int size);
    delegate void errorCallback(IntPtr request, int color, int size);
    enum Color { red, green, blue, black, white, yellow, orange };

    [MonoPInvokeCallback(typeof(debugCallback))]
    static void OnDebugCallback(IntPtr request, int color, int size)
    {
        //Ptr to string
        string debug_string = Marshal.PtrToStringAnsi(request, size);

        //Add Specified Color
        debug_string =
            String.Format("{0}{1}{2}{3}{4}",
            "<color=",
            ((Color)color).ToString(),
            ">",
            debug_string,
            "</color>"
            );

        UnityEngine.Debug.Log(debug_string);
    }

    [MonoPInvokeCallback(typeof(warnCallback))]
    static void OnWarningCallback(IntPtr request, int color, int size)
    {
        //Ptr to string
        string debug_string = Marshal.PtrToStringAnsi(request, size);

        //Add Specified Color
        debug_string =
            String.Format("{0}{1}{2}{3}{4}",
            "<color=",
            ((Color)color).ToString(),
            ">",
            debug_string,
            "</color>"
            );

        UnityEngine.Debug.LogWarning(debug_string);
    }

    [MonoPInvokeCallback(typeof(errorCallback))]
    static void OnErrorCallback(IntPtr request, int color, int size)
    {
        //Ptr to string
        string debug_string = Marshal.PtrToStringAnsi(request, size);

        //Add Specified Color
        debug_string =
            String.Format("{0}{1}{2}{3}{4}",
            "<color=",
            ((Color)color).ToString(),
            ">",
            debug_string,
            "</color>"
            );

        UnityEngine.Debug.LogError(debug_string);
    }
}