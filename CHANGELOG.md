# Changelog

All notable changes to the SentienceLab Unity Framework package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).


## [1.7] - 2023-03-01

### Added

- GameObject related events (enable/disable)

### Changed

- Extended timer functionality (restart, pause, sending values to text elements)


## [1.6] - 2021-12-12

### Added

- Added Walk-in-Place scripts
- Added scripts in Tools for: InputAction debugging, object spawning, audio detection

### Changed

- All events are now in their own event subsection
- Progress in RedirectedWalking
- ScreenFade works in instanced rendering
- Layers considered in collision and trigger events
- Moved locomotion-related scripts into different menu folder.
- Reworked Teleportation related scripts (only one controller script, separate renderer).
- Refactored Physics manipulator scripts. ``PhysicsGrab`` is now ``PhysicsManipulator_Direct``. ``PhysicsManipulator`` is now ``PhysicsManipulator_Ray``.
- Reorganised components related to events:
  - Moved to subfolder ``Events``.
  - ``ActionEvent_InputSystem`` is now ``InputActionEvent``
  - Toggle aspect separated from ``InputActionEvent`` into ``ToggleEvent`` component.
  - In case of several events for a component, they are now all grouped.

### Removed

- Moving MajorDomo into own Unity package
- InteractivitySignifier removed because events in InteractiveRigidbody provide more flexibility.


## [1.5] - 2020-10-21

### Added

### Changed

- MajorDomo protocol 0.7.1
- Added offset nodes between TrackedPose drivers and the XR controller models
- Adapted TrackedPose and input signifier actions to OpenXR plugin names
- Added template string to SynchronisedGameObject
- Generic HMD model simplified
- "UI_AlwaysOnTop" material renamed to "GUI Text"

### Removed

- Removing SentienceLab InputHandler system, now only supporting Unity's new Input System
- Legacy resources for Oculus Rift DK1 + MoCap VR
- Shader for Always on top UI text


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

