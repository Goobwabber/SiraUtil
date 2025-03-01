﻿using SiraUtil.Services;
using SiraUtil.Zenject;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using VRUIControls;

namespace SiraUtil.Tools.FPFC
{
    internal class FPFCToggle : IAsyncInitializable, IDisposable
    {
        public const string Argument = "fpfc";

        private Pose? _lastPose = new();
        private readonly FPFCState _initialState = new();
        private SimpleCameraController _simpleCameraController = null!;

        private readonly MainCamera _mainCamera;
        private readonly IFPFCSettings _fpfcSettings;
        private readonly VRInputModule _vrInputModule;
        private readonly List<IFPFCListener> _fpfcListeners;
        private readonly IMenuControllerAccessor _menuControllerAccessor;

        public FPFCToggle(MainCamera mainCamera, IFPFCSettings fpfcSettings, VRInputModule vrInputModule, List<IFPFCListener> fpfcListeners, IMenuControllerAccessor menuControllerAccessor)
        {
            _mainCamera = mainCamera;
            _fpfcSettings = fpfcSettings;
            _vrInputModule = vrInputModule;
            _fpfcListeners = fpfcListeners;
            _menuControllerAccessor = menuControllerAccessor;
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            _fpfcSettings.Changed += FPFCSettings_Changed;

            if (_mainCamera.camera == null)
                while (_mainCamera.camera == null)
                    await Task.Yield();

            _initialState.Aspect = _mainCamera.camera.aspect;
            _initialState.CameraFOV = _mainCamera.camera.fieldOfView;
            _initialState.StereroTarget = _mainCamera.camera.stereoTargetEye;

            if (_fpfcSettings.Enabled)
                _mainCamera.camera.transform.parent.gameObject.transform.position = new Vector3(0f, 1.7f, 0f);
            _simpleCameraController = _mainCamera.camera.transform.parent.gameObject.AddComponent<SimpleCameraController>();
            if (_fpfcSettings.Enabled)
                EnableFPFC();
        }

        private void FPFCSettings_Changed(IFPFCSettings fpfcSettings)
        {
            if (fpfcSettings.Enabled)
            {
                if (!_simpleCameraController.AllowInput)
                    EnableFPFC();
                else
                {
                    _mainCamera.camera.fieldOfView = fpfcSettings.FOV;
                    _simpleCameraController.MouseSensitivity = _fpfcSettings.MouseSensitivity;
                }
            }
            else if (_simpleCameraController.AllowInput)
            {
                DisableFPFC();
            }
        }

        private void EnableFPFC()
        {
            _simpleCameraController.AllowInput = true;
            _simpleCameraController.MouseSensitivity = _fpfcSettings.MouseSensitivity;
            if (_lastPose.HasValue)
            {
                _simpleCameraController.transform.position = _lastPose.Value.position;
                _simpleCameraController.transform.rotation = _lastPose.Value.rotation;
            }
            _mainCamera.camera.stereoTargetEye = StereoTargetEyeMask.None;
            _mainCamera.camera.aspect = Screen.width / (float)Screen.height;
            _mainCamera.camera.fieldOfView = _fpfcSettings.FOV;

            _menuControllerAccessor.LeftController.transform.SetParent(_simpleCameraController.transform);
            _menuControllerAccessor.RightController.transform.SetParent(_simpleCameraController.transform);
            _menuControllerAccessor.LeftController.transform.localPosition = Vector3.zero;
            _menuControllerAccessor.RightController.transform.localPosition = Vector3.zero;
            _menuControllerAccessor.LeftController.transform.localRotation = Quaternion.identity;
            _menuControllerAccessor.RightController.transform.localRotation = Quaternion.identity;
            _menuControllerAccessor.LeftController.enabled = false;
            _menuControllerAccessor.RightController.enabled = false;

            _vrInputModule.useMouseForPressInput = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            foreach (var listener in _fpfcListeners)
                listener.Enabled();
        }

        private void DisableFPFC()
        {
            _menuControllerAccessor.LeftController!.transform.SetParent(_menuControllerAccessor.Parent);
            _menuControllerAccessor.RightController.transform.SetParent(_menuControllerAccessor.Parent);
            _menuControllerAccessor.LeftController.enabled = true;
            _menuControllerAccessor.RightController.enabled = true;
            _vrInputModule.useMouseForPressInput = false;
            _simpleCameraController.AllowInput = false;

            if (XRSettings.enabled)
            {
                _lastPose = new Pose(_simpleCameraController.transform.position, _simpleCameraController.transform.rotation);
                _simpleCameraController.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                _mainCamera.camera.aspect = _initialState.Aspect;
                _mainCamera.camera.fieldOfView = _initialState.CameraFOV;
                _mainCamera.camera.stereoTargetEye = _initialState.StereroTarget;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            foreach (var listener in _fpfcListeners)
                listener.Disabled();
        }

        public void Dispose()
        {
            _fpfcSettings.Changed -= FPFCSettings_Changed;
        }
    }
}