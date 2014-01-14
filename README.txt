(a) Pucong Han 

(b)
(b1)-My code and project files are in the project folder (TrackingCamera)
(b2)-All .fbx models locates at: 
TrackingCamera\TrackingCamera\TrackingCameraContent\

(b3) The executable file of my project locates at:
TrackingCamera\TrackingCamera\TrackingCamera\x86\bin\Release\TrackingCamera.exe

(b4) All .dll files are in: 
TrackingCamera\TrackingCamera\TrackingCamera
TrackingCamera\TrackingCamera\TrackingCamera\x86\bin\Debug
TrackingCamera\TrackingCamera\TrackingCamera\x86\bin\Release

(b5) I use the default ground markers and toolbar markers. They are located at:
TrackingCamera\TrackingCamera\TrackingCamera\ALVARGroundArray.xml
TrackingCamera\TrackingCamera\TrackingCamera\Toolbar.txt

(c) My OS is Windows 7 32bit

(d) Print the marker in MyMarker folder. The bigger one is the ground marker. The smaller one is the toolbar marker

(e) All these four objects are downloaded from TurboSquid.com/XNA. The website is a platform for graphic users to share and sell 3D models. The website offers a number of free objects. Detail information about my four objects are:
The humvee car model is published on 23 July 2008. The URL link to download the model: http://www.turbosquid.com/3d-models/free-humvees-3d-model/413510
The gear model is published on 10 Dec 2011. The URL link to download the model is: http://www.turbosquid.com/FullPreview/Index.cfm/ID/643650
The cup model is published on 28 Jan 2012. The URL link to download the model is: http://www.turbosquid.com/FullPreview/Index.cfm/ID/651726
The g36c gun model is published on 9 Mar 2012. The URL link to download the model is: http://www.turbosquid.com/FullPreview/Index.cfm/ID/659740

(f) I implemented all functions including two additional extra works using keyboard to control scaling and using control panel to control rotation. I love to be creative and make my project as perfect as possible.

(g) GoblinXNA has problems in transfering object between markers. Once an object transfered between markers. Its previous properties in physics engine will get lost, including translation, rotation and scaling. As a consequences, any additional matrixs applied to the transfered object will not work. However, I find a way to fix that problem. Developers can recreate the same object using the same name and adding to the physics engine. The collusion detection callback function need to be called again. By doing so, the selected object can be selected again after transfered between markers.