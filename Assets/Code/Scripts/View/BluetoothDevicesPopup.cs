using System;
using System.Linq;
using Code.Scripts.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.View
{
    
    public class BluetoothDevicesPopup: MonoBehaviour
    {
        public GameObject pairedDevices;
        public GameObject availableDevices;
        
        public GameObject bluetoothDeviceButtonPrefab;
        public GameObject bluetoothPopupTitle;
        
        private static AndroidBluetoothConnectionGateway Gateway => AndroidBluetoothConnectionGateway.Instance;

        private float _GetButtonPrefabHeight()
        {
            return bluetoothDeviceButtonPrefab.GetComponent<RectTransform>().sizeDelta.y;
        }

        public void Start()
        {
            // If there is no text inside paired devices panel, add a text to the panel
            if (pairedDevices.transform.childCount == 0)
            {
                _CreateBluetoothPopupTitle(pairedDevices, "Paired devices", TextAlignmentOptions.Left, 20);
            }
            
            // If there is no text inside available devices panel, add a text to the panel
            if (availableDevices.transform.childCount == 0)
            {
                _CreateBluetoothPopupTitle(availableDevices, "Available devices", TextAlignmentOptions.Left, 20);
            }
        }

        private void _CreateBluetoothPopupTitle(GameObject panel, string title, 
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, float additionalHeight = 0)
        {
            var text = Instantiate(bluetoothPopupTitle, panel.transform);
            text.GetComponentInChildren<TextMeshProUGUI>().text = title;
            text.GetComponentInChildren<TextMeshProUGUI>().alignment = alignment;
            
            var vector = new Vector2(0, _GetButtonPrefabHeight() + additionalHeight);
            availableDevices.GetComponent<RectTransform>().sizeDelta += vector;
        }

        private void _CreateBluetoothDeviceButton(BluetoothDeviceData device)
        {
            var panel = device.IsPaired ? pairedDevices : availableDevices;
            
            var button = Instantiate(bluetoothDeviceButtonPrefab, panel.transform);
            
            // Get BluetoothDeviceComponent component on the button
            var bluetoothDeviceComponent = button.GetComponent<BluetoothDeviceComponent>();
            
            // Set DeviceButton name and address
            bluetoothDeviceComponent.BluetoothDeviceData = device;
                    
            // Make the button display the name of the device
            button.GetComponentInChildren<TextMeshProUGUI>().text = device.Name;
            
            // Add height to panel with current height + height of the prefab
            panel.GetComponent<RectTransform>().sizeDelta += new Vector2(0, _GetButtonPrefabHeight());
        }
        
        public void DisplayDevices()
        {
            foreach (var device in Gateway.ListDevices)
            {
                _CreateBluetoothDeviceButton(device);
            }
            
            // If there are no paired devices, add a text to the panel
            if (Gateway.ListDevices.TrueForAll(device => !device.IsPaired))
            {
                _CreateBluetoothPopupTitle(pairedDevices, "No paired devices", TextAlignmentOptions.Center, 20);
            }
            
            // If there are no available devices, add a text to the panel
            if (Gateway.ListDevices.TrueForAll(device => device.IsPaired))
            {
                _CreateBluetoothPopupTitle(availableDevices, "No available devices", TextAlignmentOptions.Center, 20);
            }
        }
    }
}