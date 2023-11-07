using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class ConvertHelpers
{
    /// <summary>
    /// This method transforms an array of unmanaged character pointers (pointed to by pUnmanagedStringArray)
    /// into an array of managed strings.
    ///
    /// This method also destroys each unmanaged character pointers and will also destroy the array itself.
    /// </summary>
    /// <param name="pUnmanagedStringArray"></param>
    /// <param name="StringCount"></param>
    /// <param name="ManagedStringArray"></param>
    public static void MarshalUnmananagedStrArray2ManagedStrArray
    (
      IntPtr pUnmanagedStringArray,
      int StringCount,
      out string[] ManagedStringArray
    )
    {
        IntPtr[] pIntPtrArray = new IntPtr[StringCount];
        ManagedStringArray = new string[StringCount];

        Marshal.Copy(pUnmanagedStringArray, pIntPtrArray, 0, StringCount);

        for (int i = 0; i < StringCount; i++)
        {
            ManagedStringArray[i] = Marshal.PtrToStringAnsi(pIntPtrArray[i]);
            Marshal.FreeCoTaskMem(pIntPtrArray[i]);
        }

        Marshal.FreeCoTaskMem(pUnmanagedStringArray);
    }
}
