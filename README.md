Sacknet.KinectFacialRecognition
===============================

A facial recognition implementation for the Kinect for Windows API.  Based
on the Open CV EigenObjectRecognizer, but translated to managed C# so
Emgu CV is no longer required!


Installation
------------

Since the library no longer uses Emgu CV, installation is much easier!

- Step 1: Install Sacknet.KinectFacialRecognition from Nuget
- Step 2: Add links to FaceTrackLib.dll (in the content folder of the
  KinectToolbox package) and FaceTrackData.dll (you can get it from 
  packages\_Manual\Kinect in the git repository or from the Kinect for
  Windows toolkit projects).  Make sure to change the file properties to
  always copy to the output directory.


Usage
-----

If you take a look at the demo project it should be fairly obvious, but all
you have to do is:

- Create an object that implements IFrameSource (roll your own or use
  AllFramesReadyFrameSource)
- Create an instance of KinectFacialRecognitionEngine
- Call SetTargetFaces() to train the recognizer (if there's only one image it
  will always return a positive, I think this might be a bug)
- Listen to the RecognitionComplete event to get the results - if there's a
  match, the key will be set on the recognized face in the RecognitionResult


Limitations/Todo
----

We currently only watch one skeleton at a time, and as such only one face will
be tracked and recognized at a time.  It shouldn't be too hard to upgrade the
library to support two recognized faces at a time, but more than that will be
impossible since the kinect only can recognize two skeletons at once.