using System;
using System.Linq;
using OpenBabel;

/// <summary>
/// Utility methods for setting up OpenBabel operations.
/// </summary>
public static class OpenBabelSetup
{
    public static Version RequiredVersion = new Version("3.0.0");
    /// <summary>
    /// Indicates whether OpenBabel is available on the system.
    /// </summary>
    public static bool IsOpenBabelAvailable
    {
        get
        {
            if (!checkedForOpenBabel)
                CheckForOpenBabel();
            return isOpenBabelAvailable;
        }
    }

    private static bool checkedForOpenBabel;
    private static bool isOpenBabelAvailable;
    private static string openBabelVersion = null;

    public static void ThrowOpenBabelNotFoundError()
    {
        if (openBabelVersion != null)
            throw new OpenBabelException($"OpenBabel v{RequiredVersion}+ is required, " +
                                         $"v{openBabelVersion} was found.");
        throw new OpenBabelException($"OpenBabel v{RequiredVersion}+ not found, is it on your PATH?");
    }

    private static void CheckForOpenBabel()
    {
        try
        {
            openBabelVersion = openbabel_csharp.OBReleaseVersion();
            isOpenBabelAvailable = new Version(openBabelVersion) >= RequiredVersion;
        }
        catch (TypeInitializationException)
        {
            isOpenBabelAvailable = false;
        }

        checkedForOpenBabel = true;
    }

    /// <summary>
    /// Throw an exception due to OpenBabel, looking in the error log to potentially
    /// find the issue.
    /// </summary>
    public static void ThrowOpenBabelException(string message)
    {
        var log = openbabel_csharp.obErrorLog;
        var error = "";
        var errors = log.GetMessagesOfLevel(OBMessageLevel.obError);
        if (errors.Count > 0)
            error = errors.Last();
        log.ClearLog();
        if (!string.IsNullOrEmpty(error))
            throw new OpenBabelException(message + "\n" + error);
        throw new OpenBabelException(message);
    }


}

/// <summary>
/// Exception caused by an error in OpenBabel.
/// </summary>
public class OpenBabelException : Exception
{
    public OpenBabelException(string message) : base(message)
    {
    }
}