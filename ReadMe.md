# chARp Molecular Builder
This repository of chARp (chemistry AR package) provides the Molecular Builder application that is designed for the HoloLens2.
Quickly building molecule structures, and sending them to the high performance compute (HPC) cluster for simulations is integrated in the normal workflow of researchers.
The package chARp also supports collaboration with multiple HoloLens2 devices.
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
Take the `OBDotNet.dll` from the OpenBabel install directory and copy it into `Assets/plugins`.
If your system if having trouble to detect all necessary DLLs, check if the OpenBabel install path is added to your `PATH`.
Under Windows go to "Edit the system environment variables" under "Environment Variables..." add the OpenBabel install path to your "Path" variable in the system variables.
Under Linux add the OpenBabel install path to your `PATH` variable.
