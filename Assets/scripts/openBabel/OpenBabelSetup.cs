using System;
using System.IO;
using System.Linq;
using OpenBabel;
using UnityEngine;

namespace chARpack
{
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


        public static void setEnvironmentForLocalOpenBabel()
        {
#if UNITY_EDITOR
            var scope = EnvironmentVariableTarget.Process; // or Machine, User
#else
        var scope = EnvironmentVariableTarget.User;
#endif
            var oldValue = Environment.GetEnvironmentVariable("PATH", scope);
            var openbabel_bin = Path.GetFullPath(Path.Combine(Path.Combine(Application.dataPath, ".."), "openbabel"));
            Debug.Log($"[OpenBabelSetup] Setting PATH: {openbabel_bin}");
            if (!oldValue.Contains(openbabel_bin))
            {
                var newValue = $"{openbabel_bin};" + oldValue;
                Environment.SetEnvironmentVariable("PATH", newValue, scope);
            }

            var babel_datadir = Path.GetFullPath(Path.Combine(Path.Combine(Path.Combine(Application.dataPath, ".."), "openbabel"), "data"));
            Debug.Log($"[OpenBabelSetup] Setting BABEL_DATADIR: {babel_datadir}");
            Environment.SetEnvironmentVariable("BABEL_DATADIR", babel_datadir, scope);
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
}