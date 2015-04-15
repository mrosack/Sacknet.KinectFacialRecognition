Sacknet.KinectFacialRecognition
===============================

A facial recognition implementation for the Kinect for Windows 2 API
(If you're looking for the Kinect 1 version, it's in the KinectV1 branch).

**NEW IN 1.0:** The code has been refactored quite a bit, so if you've been
using older version of the library you'll have to adjust your code.
However, the library now supports the ability to swap out different facial
recognition "processors", and we now support two out of the box...


Processors
----------
**EigenObjectRecognitionProcessor:** The legacy processor, based on the Open CV EigenObjectRecognizer, but translated to managed C# so Emgu CV and all the nasty Open CV DLLs are no longer required!  This processor is quick and can return a result every
frame, but is less accurate.

**FaceModelRecognitionProcessor:** Uses the Kinect 2 High Definition Face Model Builder
to construct a model of your face, comparing the generated FaceShapeDeformations to
determine identity.  This is more accurate than the Eigen Object recognizer, but it takes
a while to build the model, and you need to provide feedback to the user for them
to know what the Kinect needs to generate the model.  In the demo, the dots drawn around
the detected face show where you need to look - forward/left/right/up.


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
The demo project gives examples of how to use both processors.  Using the EigenObject
processor is much easier to implement than the FaceModel processor, since it requires
no user action.

- Create an instance of KinectFacialRecognitionEngine, passing in instance(s)
of the processor(s) you'd like to use
- Call SetTargetFaces() on the processor(s) to train the recognizer
- Listen to the RecognitionComplete event to get the results - if there's a
  match, the key will be set on the recognized face in the RecognitionResult

**If the demo project isn't working, try standing up - usually the
Kinect skeleton tracking doesn't kick in while you're seated!**


Limitations/Todo
----------------
- We currently only watch one skeleton at a time, and as such only one face will
be tracked and recognized at a time.
- The Face Model processor blocks the current thread when it has all the data it needs
and has to create the face model.  I've tried to make this run in a seperate thread,
but it seems to cause the Kinect API to crash.  Hopefully we can find a solution to
this, if so the Face Model processor will be quite nice.