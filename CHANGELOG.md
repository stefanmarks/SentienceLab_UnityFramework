# Changelog


## [1.7] - 2023-03-01 - 2024-09-20

### Added

- GameObject related events (enable/disable)
- Added some tool scripts from CTEC601 (VolumeSpawner, Footsteps, Sun Control, etc.)
- PhysicsManipulator_Pointer 

### Changed

- Minimum Unity version 2022.3
- Extended timer functionality (restart, pause, sending values to text elements)
- Added more Component menu entries
- Bugfix in ToggleEvent to fire events only when state actually changes
- Bugfixes in PhysicsManipulator_Ray and base class
- Allowing Teleporter to move rigidbody based player
- More options in ScreenControl


## [1.6] - 2021-12-12 - 2023-03-01

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


## [1.5] - 2020-10-21 - 2021-12-12

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


## [1.4] - 2020-06-02 - 2020-10-21

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


## [1.3] - 2020-01-23 - 2020-06-02 

### Added

- Redirected Walking modifier

### Changed

- MajorDomo protocol update to v0.5.3 + Flatbuffer/NetMQ updates
- Replaced SynchronisedTransform/Parameters by SynchronisedGameObject
- Adding MaxOutput to PID controller
- Various bugfixes


## [1.2] - 2019-11-25 - 2020-01-23

### Changed

- MajorDomo protocol update to v0.5.2
- Changes to Physics framework (interactivity)


## [1.1] - 2019-08-13 - 2019-11-25

### Changed

- OpenCV client pulls device attributes to construct device name (e.g., "ViveController1" is now "OpenVR_ControllerLeft")


## [1.0] - 2019-08-05 - 2019-08-13

### Fixed

- n.a.

### Added

- n.a.

### Changed

- Separated into separate package

