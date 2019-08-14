#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for a MoCap client that uses OpenVR compatible devices.
	/// </summary>
	/// 
	class OpenVR_Client : IMoCapClient
	{
		/// <summary>
		/// Constructs a MoCap client that tracks OpenVR compatible HMDs and controllers.
		/// </summary>
		///
		public OpenVR_Client()
		{
			scene          = new Scene();
			trackedDevices = new List<TrackedDevice>();
			deviceNames    = new Dictionary<string, int>();
			connected      = false;
		}


		private class TrackedDevice
		{
			public TrackedDevice(string _name)
			{
				this.name          = _name;
				this.controllerIdx = -1;
				this.deviceClass   = ETrackedDeviceClass.Invalid;
				this.device        = null;
			}

			public readonly string     name;
			public int                 controllerIdx;
			public ETrackedDeviceClass deviceClass;
			public Device              device;
		}


		public bool Connect(IMoCapClient_ConnectionInfo connectionInfo)
		{
			connected = XRDevice.isPresent;

			if (connected)
			{
				try
				{
					system = OpenVR.System;
					if (system == null)
					{
						connected = false;
						Debug.LogWarning("Could not find OpenVR System instance.");
					}

					compositor = OpenVR.Compositor;
					if ((system != null) && (compositor == null))
					{
						connected = false;
						Debug.LogWarning("Could not find OpenVR Compositor instance.");
					}
				}
				catch (DllNotFoundException)
				{
					// well, can't do anything about this
					connected = false;
				}
			}

			if (connected)
			{
				// query refresh rate
				updateRate = XRDevice.refreshRate;
				if (updateRate == 0) { updateRate = 60; } // fallback

				// allocate structures
				state = new VRControllerState_t();
				poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
				gamePoses = new TrackedDevicePose_t[0];

				// find HMDs, trackers and controllers
				trackedDevices.Clear();
				deviceNames.Clear();
				
				int inputDeviceIdx  = 0;
				for (int index = 0; index < OpenVR.k_unMaxTrackedDeviceCount; index++)
				{
					ETrackedDeviceClass deviceClass = system.GetTrackedDeviceClass((uint)index);
					TrackedDevice trackedDevice = null;

					string deviceName = GetPropertyString(index, ETrackedDeviceProperty.Prop_ModelNumber_String);
					ProcessDeviceName(ref deviceName);
					DetermineDeviceIndex(ref deviceName);

					if (deviceClass == ETrackedDeviceClass.Controller)
					{
						trackedDevice = new TrackedDevice(deviceName);
						
						// controller has 12 input channels and 1 output
						Device dev = new Device(scene, trackedDevice.name, inputDeviceIdx);
						dev.channels = new Channel[13];
						dev.channels[0]  = new Channel(dev, "button1");  // fire
						dev.channels[1]  = new Channel(dev, "button2");  // menu
						dev.channels[2]  = new Channel(dev, "button3");  // grip
						dev.channels[3]  = new Channel(dev, "button4");  // touchpad press
						dev.channels[4]  = new Channel(dev, "axis1");    // touchpad + press
						dev.channels[5]  = new Channel(dev, "axis2");
						dev.channels[6]  = new Channel(dev, "axis1raw"); // touchpad touch
						dev.channels[7]  = new Channel(dev, "axis2raw");
						dev.channels[8]  = new Channel(dev, "right");    // touchpad as buttons
						dev.channels[9]  = new Channel(dev, "left");
						dev.channels[10] = new Channel(dev, "up");
						dev.channels[11] = new Channel(dev, "down");
						dev.channels[12] = new Channel(dev, "rumble");   // rumble output
						trackedDevice.device = dev;
					}
					else if (deviceClass == ETrackedDeviceClass.GenericTracker)
					{
						trackedDevice = new TrackedDevice(deviceName);

						// tracker has 4 input channels and 1 output channel
						Device dev = new Device(scene, trackedDevice.name, inputDeviceIdx);
						dev.channels = new Channel[5];
						dev.channels[0] = new Channel(dev, "input1"); // pin 3: grip
						dev.channels[1] = new Channel(dev, "input2"); // pin 4: trigger
						dev.channels[2] = new Channel(dev, "input3"); // pin 5: touchpad press
						dev.channels[3] = new Channel(dev, "input4"); // pin 6: menu
						dev.channels[4] = new Channel(dev, "output1"); // pin 1: rumble output
						trackedDevice.device = dev; 
					}
					else if (deviceClass == ETrackedDeviceClass.HMD)
					{
						trackedDevice = new TrackedDevice(deviceName);
					}

					if (trackedDevice != null)
					{
						/*
						Debug.Log("Device " + index + ": "
							+   "Prop_ModelNumber_String " + GetPropertyString(index, ETrackedDeviceProperty.Prop_ModelNumber_String)
							+ ", Prop_SerialNumber_String " + GetPropertyString(index, ETrackedDeviceProperty.Prop_SerialNumber_String)
							+ ", Prop_RenderModelName_String " + GetPropertyString(index, ETrackedDeviceProperty.Prop_RenderModelName_String)
							+ ", Prop_ManufacturerName_String " + GetPropertyString(index, ETrackedDeviceProperty.Prop_ManufacturerName_String)
							+ "Prop_TrackingSystemName_String " + GetPropertyString(index, ETrackedDeviceProperty.Prop_TrackingSystemName_String)
							+ ", Prop_TrackingFirmwareVersion_String " + GetPropertyString(index, ETrackedDeviceProperty.Prop_TrackingFirmwareVersion_String)
						);
						*/

						trackedDevice.controllerIdx = index;
						trackedDevice.deviceClass   = deviceClass;
						trackedDevices.Add(trackedDevice);

						if (trackedDevice.device != null)
						{
							inputDeviceIdx++;
						}
					}
				}

				// construct scene description
				scene.actors.Clear();
				scene.devices.Clear();
				foreach (TrackedDevice td in trackedDevices)
				{
					Actor actor = new Actor(scene, td.name);
					actor.bones = new Bone[1];
					actor.bones[0] = new Bone(actor, "root", 0);
					scene.actors.Add(actor);

					// is this an input device, too?
					if (td.device != null)
					{
						scene.devices.Add(td.device);
					}
				}
			}
			return connected;
		}


		private void ProcessDeviceName(ref string deviceName)
		{
			// remove spaces and dashes
			deviceName = deviceName.Replace(" ", "").Replace("-", "");
			// handle product names
			deviceName = deviceName.Replace("MixedReality", "MR").Replace("WindowsMR", "WMR");
			deviceName = deviceName.Replace("VIVE", "Vive");
			// handle IDs for left/right handed
			if (deviceName.Contains(":0x045E/0x065B"))
			{
				deviceName = deviceName.Replace(":0x045E/0x065B", "Controller");
				deviceName = deviceName.Replace("/0/1", "Left").Replace("/0/2", "Right");
			}
		}


		/// <summary>
		/// Determine a postfix index for a device.
		/// If a device is not handed, it will automatically receive an index starting with 1.
		/// If a device is handed (left/right), it will only receive an index if there is more than one.
		/// </summary>
		/// <param name="deviceName">the name to add a postfix to</param>
		private void DetermineDeviceIndex(ref string deviceName)
		{
			int idx = -1;
			if (deviceNames.TryGetValue(deviceName, out idx))
			{
				idx++;
				deviceNames[deviceName] = idx;
			}
			else
			{
				deviceNames.Add(deviceName, 1);
				idx = 1;
			}
			string lowercase = deviceName.ToLower();
			bool   handed    = lowercase.Contains("right") || lowercase.Contains("left");
			if ( (idx >= 2) || !handed)
			{
				deviceName += idx;
			}
		}


		public bool IsConnected()
		{
			return connected;
		}


		public void Disconnect()
		{
			connected = false; 
			system     = null;
			compositor = null;
		}
		
		
		public string GetDataSourceName()
		{
			return "OpenVR/" + XRDevice.model;
		}


		private string GetPropertyString(int device, ETrackedDeviceProperty property)
		{
			StringBuilder b = new StringBuilder(1024);
			ETrackedPropertyError err = ETrackedPropertyError.TrackedProp_Success;
			system.GetStringTrackedDeviceProperty((uint)device, property, b, (uint) b.Capacity, ref err);
			return b.ToString();
		}


		public float GetFramerate()
		{
			return updateRate;
		}


		public void SetPaused(bool pause)
		{
			// ignored
		}


		public void Update(ref bool dataChanged, ref bool sceneChanged)
		{
			compositor.GetLastPoses(poses, gamePoses);

			// frame number and timestamp
			scene.frameNumber = Time.frameCount;
			scene.timestamp   = Time.time;

			for (int idx = 0; idx < trackedDevices.Count; idx++)
			{
				// update position, orientation, and tracking state
				int controllerIdx = trackedDevices[idx].controllerIdx;
				Bone bone = scene.actors[idx].bones[0];

				HmdMatrix34_t pose = poses[controllerIdx].mDeviceToAbsoluteTracking;
				Matrix4x4     m    = Matrix4x4.identity;
				m[0,0] = pose.m0; m[0,1] = pose.m1; m[0,2] = pose.m2;  m[0,3] = pose.m3;
				m[1,0] = pose.m4; m[1,1] = pose.m5; m[1,2] = pose.m6;  m[1,3] = pose.m7;
				m[2,0] = pose.m8; m[2,1] = pose.m9; m[2,2] = pose.m10; m[2,3] = pose.m11;
				MathUtil.ToggleLeftRightHandedMatrix(ref m);
				bone.CopyFrom(MathUtil.GetTranslation(m), MathUtil.GetRotation(m));

				bone.tracked = poses[controllerIdx].bDeviceIsConnected && poses[controllerIdx].bPoseIsValid;

				// if this is also an input device, update inputs 
				Device dev = trackedDevices[idx].device;
				if (dev != null)
				{
					system.GetControllerState(
						(uint)controllerIdx,
						ref state, 
						(uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t)));

					if (trackedDevices[idx].deviceClass == ETrackedDeviceClass.Controller)
					{
						// hand controller
						// trigger button
						dev.channels[0].value = state.rAxis1.x;
						// menu button
						dev.channels[1].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu)) != 0 ? 1 : 0;
						// grip button
						dev.channels[2].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_Grip)) != 0 ? 1 : 0;
						// touchpad (button4, axis1/2 and axis1raw/2raw)
						float touchpadPressed = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad)) != 0 ? 1 : 0;
						dev.channels[3].value = touchpadPressed;
						dev.channels[4].value = state.rAxis0.x * touchpadPressed;
						dev.channels[5].value = state.rAxis0.y * touchpadPressed;
						dev.channels[6].value = state.rAxis0.x;
						dev.channels[7].value = state.rAxis0.y;

						// touchpad as buttons
						Vector2 touchpad = new Vector2(state.rAxis0.x, state.rAxis0.y) * touchpadPressed;
						float distance = touchpad.magnitude;
						if (distance < 0.3f) touchpad = Vector2.zero;
						// using angle to determine which circular segment the finger is on
						// instead of purely <> comparisons on coordinates
						float angle = Mathf.Rad2Deg * Mathf.Atan2(touchpad.y, touchpad.x);
						//    +135  +90  +45
						// +180/-180       0   to allow for overlap, angles are 67.5 around a direction
						//    -135  -90  -45
						dev.channels[8].value  = ((angle >   0 - 67.5f) && (angle <    0 + 67.5f)) ? touchpadPressed : 0; // right
						dev.channels[9].value  = ((angle > 180 - 67.5f) || (angle < -180 + 67.5f)) ? touchpadPressed : 0; // left
						dev.channels[10].value = ((angle >  90 - 67.5f) && (angle <   90 + 67.5f)) ? touchpadPressed : 0; // up
						dev.channels[11].value = ((angle > -90 - 67.5f) && (angle <  -90 + 67.5f)) ? touchpadPressed : 0; // down
						// rumble output > convert value [0...1] to time 																						  // rumble output > convert value [0...1] to time 
						float duration = Mathf.Clamp01(dev.channels[12].value) * 4000f; // 4000us (4ms) is maximum length
						system.TriggerHapticPulse((uint) controllerIdx, 0, (ushort) duration);
					}
					else if (trackedDevices[idx].deviceClass == ETrackedDeviceClass.GenericTracker)
					{
						// generic tracker
						// grip button
						dev.channels[0].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_Grip)) != 0 ? 1 : 0;
						// trigger button
						dev.channels[1].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Trigger)) != 0 ? 1 : 0;
						// touchpad 
						dev.channels[2].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad)) != 0 ? 1 : 0;
						// menu button
						dev.channels[3].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu)) != 0 ? 1 : 0;
						// rumble output > convert value [0...1] to time 
						float duration = Mathf.Clamp01(dev.channels[4].value) * 1f; // no timing, just on/off
						system.TriggerHapticPulse((uint) controllerIdx, 0, (ushort) duration);
					}
				}
			}
			dataChanged = true;
		}


		public Scene GetScene()
		{
			return scene;
		}


		private bool                     connected;
		private Scene                    scene;
		private CVRCompositor            compositor;
		private CVRSystem                system;
		private List<TrackedDevice>      trackedDevices;
		private Dictionary<string, int>  deviceNames;
		private TrackedDevicePose_t[]    poses, gamePoses;
		private VRControllerState_t      state;
		private float                    updateRate;
	}

}
