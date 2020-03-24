# GazeTracker
Tracker that uses AI to track the orientation and position of your face using a webcam

Shameless copy from the examples of Tadas API [OpenFace](https://github.com/TadasBaltrusaitis/OpenFace)  
The idea is to make it work with [OpenTrack](https://github.com/opentrack/opentrack) trough UDP

## Download:
 Just go to the [releases](../../releases) page
 
## Compiling:

1: You need to run the `download_models.ps1` script to download the AI models.  
2: Build it with visual studio 2019 and make sure that the `Libs` content is copied to the output

## Usage:

1: Run ´GazeTracker.exe´
2: Select the webcam and the resolution you want to use
3: Once it's running check that your face angles and position is correctly detected. It works better if the camera is far away and you **don't wear glasses** or things that cover your face and could mess the AI
4: Check the port number at the bottom of ´GazeTracker´ and change it if you want
5: Open OpenTrack
6: In the ´Input´ dropdown of OpenTrack select ´UDP over network´
7: Click on the button next to the ´Input´dropdown and in the popup write the same port that you selected before
8: Click ´start´ in OpenTrack
9: You might need to adjust the axes. To do so, check [OpenTrack wiki](https://github.com/opentrack/opentrack/wiki)
