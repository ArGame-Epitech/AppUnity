using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Android;

namespace Code.Scripts.Infrastructure
{
    public class AndroidBluetoothConnectionGateway : MonoBehaviour
    //IBluetoothConnectionGateway
     {
         // Reference to the pop-up dialog
         public GameObject bluetoothPopup;

         // Reference to the UI text that displays the list of devices
         public Text devicePairedListText;
         public Text deviceAvailableListText;

         private List<Dictionary<string, string>> _deviceAvailableList;
         
         private AndroidJavaObject _bluetoothAdapter;
         private AndroidJavaObject _bluetoothManager;
         private AndroidJavaObject _bluetoothSocket;
         private AndroidJavaObject _bluetoothReceiver;

         public void Start()
         {
             _deviceAvailableList = new List<Dictionary<string, string>>();
             
             // Call the method to grant permissions
             _GrantPermission();
             
             // Create an instance of the Android Bluetooth Adapter
             AndroidJavaClass bluetoothAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter");
             _bluetoothAdapter = bluetoothAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");
             
             if (_bluetoothAdapter != null)
             {
                 _ScanForPairedDevices();
                 
                 _RegisterBluetoothReceiver();
             }
             else
             {
                 // Bluetooth is not available on the device
                 Debug.Log("Bluetooth is not available on this device.");
             }
         }

         private static void _GrantPermission()
         {
            #if UNITY_2020_2_OR_NEWER
            #if UNITY_ANDROID
             if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation)
                 || !Permission.HasUserAuthorizedPermission(Permission.FineLocation)
                 || !Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN")
                 || !Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_ADVERTISE")
                 || !Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT")
                 || !Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH")
                 || !Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_ADMIN")
                 || !Permission.HasUserAuthorizedPermission("android.permission.ACCESS_NETWORK_STATE")
                )
                 
                 Permission.RequestUserPermissions(
                     new string[] { 
                         Permission.CoarseLocation,
                         Permission.FineLocation,
                         "android.permission.BLUETOOTH",
                         "android.permission.BLUETOOTH_ADMIN",
                         "android.permission.BLUETOOTH_SCAN",
                         "android.permission.BLUETOOTH_ADVERTISE",
                         "android.permission.BLUETOOTH_CONNECT",
                         "android.permission.ACCESS_NETWORK_STATE"
                     }
                 );
            #endif
            #endif
         }

         public void ConnectToDevice(string deviceAddress)
         {
             // Get the Bluetooth device by its address
             AndroidJavaObject device = _bluetoothAdapter.Call<AndroidJavaObject>("getRemoteDevice", deviceAddress);
             
             // Get the UUIDs associated with the device
             AndroidJavaObject uuids = device.Call<AndroidJavaObject>("getUuids");
         
             // Check if UUIDs are available
             if (uuids != null)
             {
                 // Get the first UUID (you can modify this logic based on your needs)
                 AndroidJavaObject[] uuidArray = AndroidJNIHelper.ConvertFromJNIArray<AndroidJavaObject[]>(uuids.GetRawObject());
                 AndroidJavaObject firstUuid = uuidArray[0];
                 var uuidString = firstUuid.Call<string>("toString");

                 // Now, you can use the UUID string to create the socket
                 _bluetoothSocket = device.Call<AndroidJavaObject>("createRfcommSocketToServiceRecord", uuidString);
                 _bluetoothSocket.Call("connect");
             }
             else
             {
                 Debug.LogError("UUIDs not available for the device.");
             }
         }
         
         // Method to scan for available Bluetooth devices
         private void _ScanForPairedDevices()
         {
             // Create List of dictionaries
            List<Dictionary<string, string>> devicesInfo = new List<Dictionary<string, string>>();
            
            // Use the Android Bluetooth API to discover devices
            AndroidJavaObject devices = _bluetoothAdapter.Call<AndroidJavaObject>("getBondedDevices");

            AndroidJavaObject[] deviceArray = devices.Call<AndroidJavaObject[]>("toArray");

            foreach (AndroidJavaObject device in deviceArray)
            {
                var deviceName = device.Call<string>("getName");
                var deviceAddress = device.Call<string>("getAddress");
                
                // Create a dictionary with the device info
                Dictionary<string, string> deviceInfo = new Dictionary<string, string>();
                deviceInfo.Add("name", deviceName);
                deviceInfo.Add("address", deviceAddress);
                
                // Add the dictionary to the list
                devicesInfo.Add(deviceInfo);
            }

            // Display the list of devices in the UI text element
            _UpdatePairedDeviceList(devicesInfo);
         }
         
         private void _StartDiscovery()
         {
             var isDiscovering = _bluetoothAdapter.Call<bool>("isDiscovering");

             if (!isDiscovering)
             {
                 _bluetoothAdapter.Call<bool>("startDiscovery");
             }
         }

         private void _StopDiscovery()
         {
             var isDiscovering = _bluetoothAdapter.Call<bool>("isDiscovering");
             
                if (isDiscovering)
                {
                    // Stop the discovery process
                    _bluetoothAdapter.Call<bool>("cancelDiscovery");
                }
         }
         
         private void _RegisterBluetoothReceiver()
         {
             // Create an instance of CustomBluetoothReceiver
             _bluetoothReceiver = new AndroidJavaObject("com.argames.bluetooth.CustomBluetoothReceiver");

             // Register the receiver with the Android system
             using (AndroidJavaObject unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
             {
                 using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                 {
                     // Create IntentFilter instance
                     var filter = new AndroidJavaObject("android.content.IntentFilter", "android.bluetooth.device.action.FOUND");
                        
                     // Register the receiver using the CustomBluetoothReceiver instance
                     _bluetoothReceiver.Call("register", currentActivity, filter);
                     
                     // Once the receiver is registered, you can start the discovery process
                     _ScanForAvailableDevices();
                 }
             }
         }
         
         // Method called from CustomBluetoothReceiver plugin
         public void HandleDeviceDiscovered(string deviceInfo)
         {
             var deviceInfoArray = deviceInfo.Split('|');
             var deviceInfoDictionary = new Dictionary<string, string>();
            
             // Cast the device info to a dictionary
             if (deviceInfoArray[0] != "null" && deviceInfoArray[1] != "null")
             {
                 deviceInfoDictionary.Add("name", deviceInfoArray[0]);
                 deviceInfoDictionary.Add("address", deviceInfoArray[1]);
             
                 _UpdateAvailableDeviceList(deviceInfoDictionary);   
             }
         }

         private void _ScanForAvailableDevices()
         {
             _StartDiscovery();

             // Wait for 5 seconds
             Invoke(nameof(_StopDiscovery), 5f);
         }
         
         public bool GetDiscoveringStatus()
         {
             return _bluetoothAdapter.Call<bool>("isDiscovering");
         }
         
         private void _UpdatePairedDeviceList(List<Dictionary<string, string>> deviceInfos)
         {
             devicePairedListText.text = "Paired Bluetooth Devices:\n";
             
             foreach (var deviceInfo in deviceInfos)
             { 
                 string deviceName = deviceInfo["name"]; 
                 string deviceAddress = deviceInfo["address"];
                    
                 devicePairedListText.text += deviceName + " (" + deviceAddress + ")\n";
             }
         }
         
         private void _UpdateAvailableDeviceList(Dictionary<string, string> deviceInfos)
         {
             deviceAvailableListText.text = "Available Bluetooth Devices:\n";

             var isNew = true;
             
             foreach (var deviceInfo in _deviceAvailableList)
             { 
                 string deviceName = deviceInfo["name"]; 
                 string deviceAddress = deviceInfo["address"];

                 if (deviceAddress == deviceInfos["address"])
                 {
                     isNew = false;
                 }
                 
                 deviceAvailableListText.text += deviceName + " (" + deviceAddress + ")\n";
             }
             
             if (isNew)
             {
                 _deviceAvailableList.Add(deviceInfos);
                 deviceAvailableListText.text += deviceInfos["name"] + " (" + deviceInfos["address"] + ")\n";
             }
         }

         // Method to close the Bluetooth device list popup
         public void CloseBluetoothDeviceList()
         {
             // Hide the pop-up dialog
             bluetoothPopup.SetActive(false);
         }
         
         private void OnApplicationQuit()
         {
             // Close the Bluetooth socket
             if (_bluetoothSocket != null)
             {
                 _bluetoothSocket.Call("close");
             }
         }
    }

}


