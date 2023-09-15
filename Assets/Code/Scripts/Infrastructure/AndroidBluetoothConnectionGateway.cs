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
         public Text deviceListText;

         private AndroidJavaObject _bluetoothAdapter;
         private AndroidJavaObject _bluetoothManager;
         private AndroidJavaObject _bluetoothSocket;

         public void Start()
         {
             _GrantPermission();
             
             // Create an instance of the Android Bluetooth Adapter
             AndroidJavaClass bluetoothAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter");
             _bluetoothAdapter = bluetoothAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");

             if (_bluetoothAdapter != null)
             {
                 // Bluetooth is available on the device
                 var deviceName = _bluetoothAdapter.Call<string>("getName");
                 Debug.Log("Bluetooth is available on this device: " + deviceName);
                 
                 _ScanForDevices();
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
                 string uuidString = firstUuid.Call<string>("toString");

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
         private void _ScanForDevices()
         {
             // Create List of dictionaries
            List<Dictionary<string, string>> devicesInfo = new List<Dictionary<string, string>>();
            
            // Use the Android Bluetooth API to discover devices
            AndroidJavaObject devices = _bluetoothAdapter.Call<AndroidJavaObject>("getBondedDevices");

            AndroidJavaObject[] deviceArray = devices.Call<AndroidJavaObject[]>("toArray");

            foreach (AndroidJavaObject device in deviceArray)
            {
                string deviceName = device.Call<string>("getName");
                string deviceAddress = device.Call<string>("getAddress");
                
                // Create a dictionary with the device info
                Dictionary<string, string> deviceInfo = new Dictionary<string, string>();
                deviceInfo.Add("name", deviceName);
                deviceInfo.Add("address", deviceAddress);
                
                // Add the dictionary to the list
                devicesInfo.Add(deviceInfo);
            }

            // Display the list of devices in the UI text element
            _UpdateDeviceList(devicesInfo);
         }

         // Method to update the UI with the list of discovered devices
         private void _UpdateDeviceList(List<Dictionary<string, string>> deviceNames)
         {
             deviceListText.text = "Available Bluetooth Devices:\n";
             
             foreach (Dictionary<string, string> deviceInfo in deviceNames)
             { 
                 string deviceName = deviceInfo["name"]; 
                 string deviceAddress = deviceInfo["address"];
                    
                 deviceListText.text += deviceName + " (" + deviceAddress + ")\n";
             }
         }

         // Method to close the Bluetooth device list popup
         public void CloseBluetoothDeviceList()
         {
             // Hide the pop-up dialog
             bluetoothPopup.SetActive(false);
         }
    }

}


