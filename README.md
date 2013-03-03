Sacknet.KinectFacialRecognition
===============================

A facial recognition implementation for the Kinect for Windows API.


Installation
------------

Unfortunately, since this library uses Emgu CV it's no cakewalk to get
running, but I've tried to make it as simple as possible.  If you download
the repository at https://github.com/mrosack/Sacknet.KinectFacialRecognition
it should build and run the demo project with no problems, but if you're
trying to integrate it into your own project you'll need to do the following...

- Step 1: Install Sacknet.KinectFacialRecognition from Nuget
- Step 2: Add Emgu CV references and C++ DLLs.  (See
  Sacknet.KinectFacialRecognitionDemo for examples)
  - We're using Emgu CV 2.2.0, since the DLLs aren't obnoxiously large like
    they are in the current versions, and they don't require CUDA.  The DLLs
	are in packages\_Manual\Emgu.CV.
  - Add references to Emgu.CV and Emgu.Util to your main project.
  - Right-click on your main project, click add -> existing item, and add the
    rest of the Emgu.CV dlls as links.  Make sure to change the file
	properties to always copy to the output directory.
- Step 3: Add links to FaceTrackLib.dll (in the content folder of the
  KinectToolbox package) and FaceTrackData.dll (in packages\_Manual\Kinect).
  Make sure to change the file properties to always copy to the output
  directory.


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