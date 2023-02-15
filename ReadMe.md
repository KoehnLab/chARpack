# Molecular Builder for HoloLens2

This project is developed with Unity version [2021.3.12f1](unityhub://2021.3.12f1/8af3c3e441b1).


## MRTK
MRTK tarballs are not checked into git repository. Therefore, the used packages have to be installed by hand.
Please run [MixedRealityFeatureTool](https://www.microsoft.com/en-us/download/details.aspx?id=102778) over the project and install:

    Mixed Reality Toolkit >
    Mixed Reality Toolkit Foundations (v2.8.2)
    Mixed Reality Toolkit Extensions (v2.8.2)
    Mixed Reality Toolkit Examples (v2.8.2)
    Mixed Reality Toolkit Standard Assets (v2.8.2)


    Platform Support >
    Mixed Reality OpenXR Plugin (v1.6.0)

All these features should also appear with the tag "Version x.x.x currently installed".

## NuGet
Download and install [Nuget for Unity](https://github.com/GlitchEnzo/NuGetForUnity/releases/latest). To do this, Download the `.unitypackage` file and drag-and-drop it into Unity. Open the Nuget manager inside Unity and install the packages

    Microsoft.MixedReality.QR
    Microsoft.VCRTForwarders.140

## OpenBabel
For server support of OpenBabel please install the [latest](https://github.com/openbabel/openbabel/releases/latest) version (x64 exe).