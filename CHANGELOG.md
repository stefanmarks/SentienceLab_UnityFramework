# Changelog

All notable changes to the Sentience Lab Unity Framework package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).


## [1.5] - 2020-10-21

### Added


### Changed

- Added offset nodes between TrackedPose drivers and the XR controller models
- Adapted TrackedPose and input signifier actions to OpenXR plugin names
- Added template string to SynchronisedGameObject

### Removed

- Removing SentienceLab InputHandler system, now only supporting Unity's new Input System



## [1.4] - 2020-06-02

### Added

- Touchpad button composite
- Transform limits for pos/rot/scale
- TrackingModeOriginSetup script
- Oculus Quest 2 controller models and prefabs
- Input signifier scripts

### Changed

- Restructure of folders and component menu items
- Deprecating SentienceLab InputHandler system, preparing for switch to Input System
- Ray controlled by parameter instead of input action

### Removed

- XR/OpenVR MoCap Modules
- ConfigurationManager, StartPosition
- Default Inputs JSON file


## [1.3] - 2020-01-23

### Added

- Redirected Walking modifier

### Changed

- MajorDomo protocol update to v0.5.3 + Flatbuffer/NetMQ updates
- Replaced SynchronisedTransform/Parameters by SynchronisedGameObject
- Adding MaxOutput to PID controller
- Various bugfixes


## [1.2] - 2019-11-25

### Changed

- MajorDomo protocol update to v0.5.2
- Changes to Physics framework (interactivity)


## [1.1] - 2019-08-13

### Changed

- OpenCV client pulls device attributes to construct device name (e.g., "ViveController1" is now "OpenVR_ControllerLeft")


## [1.0] - 2019-08-05

### Fixed

- n.a.

### Added

- n.a.

### Changed

- Separated into separate package

