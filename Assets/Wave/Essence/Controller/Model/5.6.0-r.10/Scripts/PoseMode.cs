// "WaveVR SDK 
// © 2017 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the WaveVR SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using UnityEngine;
using Wave.Essence;
using Wave.Native;

public class PoseMode : MonoBehaviour
{
	const string LOG_TAG = "Controller.Model.CPoseMode";
	void DEBUG(string msg) { Log.d(LOG_TAG, msg, true); }
	public XR_Hand WhichHand = XR_Hand.Dominant;
	private XR_ControllerPoseMode poseMode = XR_ControllerPoseMode.Raw;
	private WVR_DeviceType deviceType = WVR_DeviceType.WVR_DeviceType_Invalid;
	private Vector3 currPosOffset = Vector3.zero;
	private Quaternion currRotOffset = Quaternion.identity;

	// Start is called before the first frame update
	void Start()
    {
		if (WhichHand == XR_Hand.Dominant)
			deviceType = WVR_DeviceType.WVR_DeviceType_Controller_Right;
		else
			deviceType = WVR_DeviceType.WVR_DeviceType_Controller_Left;
			
		if (WXRDevice.GetControllerPoseMode(WhichHand, out poseMode))
		{
			DEBUG(WhichHand + " is using " + poseMode + " on start.");
		} else
		{
			DEBUG(WhichHand + " get pose mode fail");
		}
	}

	//private void OnApplicationPause(bool pause)
	//{
	//	if (!pause)
	//	{
	//		if (Interop.WVR_GetControllerPoseMode(deviceType, ref poseMode))
	//		{
	//			DEBUG("onResume" + WhichHand + " is using " + poseMode);
	//		}
	//		else
	//		{
	//			DEBUG(WhichHand + " get pose mode fail");
	//		}
	//	}
	//}

	// Update is called once per frame
	void Update()
    {
		var vec = WXRDevice.GetCurrentControllerPositionOffset(WhichHand);
		var rot = WXRDevice.GetCurrentControllerRotationOffset(WhichHand);
		if (vec != currPosOffset || rot != currRotOffset)
		{
			DEBUG("Controller pose mode changed");
			if (WXRDevice.GetControllerPoseMode(WhichHand, out poseMode))
			{
				DEBUG(WhichHand + " is changed to " + poseMode);
			}
			else
			{
				DEBUG(WhichHand + " get pose mode fail");
			}

			currPosOffset = vec;
			currRotOffset = rot;

			this.transform.localPosition = currPosOffset;
			this.transform.localRotation = currRotOffset;

			DEBUG(WhichHand + " Pos offset: " + vec.x + ", " + vec.y + ", " + vec.z + ", Rot offset: " + rot.x + ", " + rot.y + ", " + rot.z + ", " + rot.w);
			
		}
    }
}
