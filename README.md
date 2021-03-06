<p align="center">
    <a href="https://ci.appveyor.com/project/gavazquez/gazetracker"><img src="https://img.shields.io/appveyor/ci/gavazquez/lunamultiplayer/master.svg?style=flat&logo=appveyor" alt="AppVeyor"/></a>
    <a href="https://paypal.me/gavazquez"><img src="https://img.shields.io/badge/paypal-donate-yellow.svg?style=flat&logo=paypal" alt="PayPal"/></a>
    <a href="../../releases"><img src="https://img.shields.io/github/release/gavazquez/gazetracker.svg?style=flat&logo=github&logoColor=white" alt="Latest release" /></a>
    <a href="../../releases"><img src="https://img.shields.io/github/downloads/gavazquez/gazetracker/total.svg?style=flat&logo=github&logoColor=white" alt="Total downloads" /></a>
</p>

# GazeTracker
Tracker that uses an artificial intelligence to track the orientation and position of your face using a webcam

## Download:
Just go to the [releases](../../releases) page

## Usage:

1) Run `GazeTracker.exe`
2) Select the webcam and the resolution you want to use
3) Once it's running check that your face angles and position is correctly detected. It works better if the camera is far away and you **don't wear glasses** or things that cover your face that could mess the AI
4) Check the port number at the bottom of `GazeTracker` and change it if you want
5) Open `OpenTrack`
6) In the `Input` dropdown of OpenTrack select `UDP over network`
7) Click on the button next to the `Input` dropdown and in the popup write **the same port that you selected before**
8) Click `start` in OpenTrack
9) You might need to adjust the axes. To do so, check the [OpenTrack wiki](https://github.com/opentrack/opentrack/wiki)

## Playstation 3 Eye
You will need to have the CL Eye Driver installed if you want to use this type of webcam. You can get it [here](https://archive.org/download/CLEyeDriver5.3.0.0341Emuline/CL-Eye-Driver-5.3.0.0341-Emuline.exe)

## Tips:

- Try to use a webcam with a wide field of view and at eye level to correctly detect pitch angles
- Usually a lower resolution increase the framerate and makes the work easier for the AI

## Compiling:

1) You need to run the `download_models.ps1` script to download the AI models (~500 Mb).  
2) Build it with visual studio 2019 and make sure that the `Libs` content is copied to the output

## Acknowledgments:

This tracker uses the amazing [Tada's OpenFace API](https://github.com/TadasBaltrusaitis/OpenFace).  
Unfortunately the AI model doesn't work well with people who wear glasses and training it takes a lot of time and knowledge
