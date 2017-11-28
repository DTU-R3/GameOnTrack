# GamesOnTrack + ROS / MQTT
The code to calibrate the GameOnTrack system and sending data to ROS.

Product page:
* http://www.gamesontrack.dk/

ROS .net is used in this project:
* https://github.com/DTU-R3/ROS.NET

## How to use
Set ROS_HOSTNAME and ROS_MASTER_URI in windows environment variables

## GOTSDKSample
This project comes from the GamesOnTrack company as a sample project for C#. It is used as the calibration program for our system

## ROSOnTrack
This project get the postion data from two GamesOnTrack sensors and calculate the position of the robot afterwards. The raw sensor data and the calculation are published as ROS topic

## ROSOnTrack-Console
A console program version of ROSOnTrack, publishing the two GamesOnTrack sensors' data to ROS and subscribing the address topic in order to change the address of the sensor dynamically


## ROSOnTrack-MQTT

A console program that publishes all GamesOnTrack sensor data through MQTT. The variables 'clientId' and 'Server' need to be changed according to the configuration.
