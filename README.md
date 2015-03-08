Sacknet.KinectFacialRecognition
===============================

A facial recognition implementation for the Kinect for Windows 2 API
(If you're looking for the Kinect 1 version, it's in the KinectV1 branch).
Based on the Open CV EigenObjectRecognizer, but translated to managed C# so
Emgu CV and all the nasty Open CV DLLs are no longer required!


Installation
------------

- Step 1: Install Sacknet.KinectV2FacialRecognition (x86/x64) from Nuget
- Step 2: Make sure you have a reference to Microsoft.Kinect and
  Microsoft.Kinect.Face.dll in your executable project
- Step 3: Add the following command to the post-build event of your
  executable project:
  xcopy "$(KINECTSDK20_DIR)Redist\Face\$(Platform)\NuiDatabase" "$(TargetDir)\NuiDatabase" /S /R /Y /I



Usage
-----

If you take a look at the demo project it should be fairly obvious, but all
you have to do is:

- Create an instance of KinectFacialRecognitionEngine
- Call SetTargetFaces() to train the recognizer (if there's only one image it
  will always return a positive, I think this might be a bug)
- Listen to the RecognitionComplete event to get the results - if there's a
  match, the key will be set on the recognized face in the RecognitionResult

**If the demo project isn't working, try standing up - usually the
Kinect skeleton tracking doesn't kick in while you're seated!**

Limitations/Todo
----

We currently only watch one skeleton at a time, and as such only one face will
be tracked and recognized at a time.
