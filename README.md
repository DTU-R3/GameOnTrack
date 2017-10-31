# GameOnTrack + ROS
The code to calibrate the GameOnTrack system and sending data to ROS.

Product page:
* http://www.gamesontrack.dk/

ROS .net is used in this project:
* https://github.com/DTU-R3/ROS.NET

## GOTSDKSample
This project comes from the GameOnTrack company as a sample project for C#. It is used as the calibration program for our system

## ROSOnTrack
This project get the postion data from two GameOnTrack sensors and calculate the position of the robot afterwards. The raw sensor data and the calculation are published as ROS topic
