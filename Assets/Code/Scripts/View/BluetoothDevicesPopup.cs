using Code.Scripts.Infrastructure;
using TMPro;
using UnityEngine;

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
            if (!pairedDevices.transform.Find("TPairedDevices"))
            {
                _CreateBluetoothPopupTitle(pairedDevices, "TPairedDevices", "Paired devices", 
                    additionalHeight: 20, setAsFirstSibling: true);
            }
            
            // If there is no text inside available devices panel, add a text to the panel
            if (!availableDevices.transform.Find("TPairedDevices"))
            {
                _CreateBluetoothPopupTitle(availableDevices, "TAvailableDevices", "Available devices",
                    additionalHeight: 20, setAsFirstSibling: true);
            }
            
            _SetPanelStatus();
        }

        private void _SetPanelStatus()
        {
            // If there are no paired devices, add a text to the panel
            if (Gateway.ListDevices.TrueForAll(device => !device.IsPaired)
                && !pairedDevices.transform.Find("TNoPairedDevices"))
            {
                _CreateBluetoothPopupTitle(pairedDevices, "TNoPairedDevices","No paired devices", 
                    TextAlignmentOptions.Center, 20);
            }
            else
            {
                // Remove the text "No paired devices" from the panel
                _DestroyBluetoothPopupTitle(pairedDevices, "TNoPairedDevices");
            }
            
            // If there are no available devices, add a text to the panel
            if (Gateway.ListDevices.TrueForAll(device => device.IsPaired)
                && !availableDevices.transform.Find("TNoAvailableDevices"))
            {
                _CreateBluetoothPopupTitle(availableDevices, "TNoAvailableDevices", "No available devices", 
                    TextAlignmentOptions.Center, 20);
            }
            else
            {
                // Remove the text "No available devices" from the panel
                _DestroyBluetoothPopupTitle(availableDevices, "TNoAvailableDevices");
            }
        }

        private void _CreateBluetoothPopupTitle(GameObject panel, string label, string title, 
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, float additionalHeight = 0, 
            bool setAsFirstSibling = false)
        {            
            var text = Instantiate(bluetoothPopupTitle, panel.transform);
            if (setAsFirstSibling) text.transform.SetAsFirstSibling();
            
            text.GetComponentInChildren<TextMeshProUGUI>().name = label;
            text.GetComponentInChildren<TextMeshProUGUI>().text = title;
            text.GetComponentInChildren<TextMeshProUGUI>().alignment = alignment;
            
            var vector = new Vector2(0, _GetButtonPrefabHeight() + additionalHeight);
            availableDevices.GetComponent<RectTransform>().sizeDelta += vector;
        }
        
        private void _DestroyBluetoothPopupTitle(GameObject panel, string label)
        {
            var text = panel.transform.Find(label);
            if (text != null)
            {
                Destroy(text.gameObject);
                
                // Reduce panel height
                var vector = new Vector2(0, -_GetButtonPrefabHeight());
                panel.GetComponent<RectTransform>().sizeDelta += vector;
            }
        }

        private void _CreateBluetoothDeviceButton(BluetoothDeviceData device)
        {
            var panel = device.IsPaired ? pairedDevices : availableDevices;
            
            var button = Instantiate(bluetoothDeviceButtonPrefab, panel.transform);
            
            // Set button name to the address of the device
            button.name = device.Address;
            
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
                // If there is no button with the name holding the address the device, create a button
                if (!pairedDevices.transform.Find(device.Address) && !availableDevices.transform.Find(device.Address))
                    _CreateBluetoothDeviceButton(device);
            }
            
            _SetPanelStatus();
        }
    }
}