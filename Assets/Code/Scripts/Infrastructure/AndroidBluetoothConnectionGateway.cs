using UnityEngine;
using System.Collections.Generic;
using Code.Scripts.View;
using UnityEngine.Android;

namespace Code.Scripts.Infrastructure
{
    public class AndroidBluetoothConnectionGateway : MonoBehaviour
    //IBluetoothConnectionGateway
     {
         // Singleton instance for easy access
         public static AndroidBluetoothConnectionGateway Instance { get; private set; }
         
         public List<BluetoothDeviceData> ListDevices { get; private set; }
         
         private AndroidJavaObject _bluetoothAdapter;
         private AndroidJavaObject _bluetoothSocket;
         private AndroidJavaObject _bluetoothReceiver;
         
         public BluetoothDevicesPopup bluetoothDevicesPopup;

         public void Start()
         {
             ListDevices = new List<BluetoothDeviceData>();
             
             // Add random devices
             for (var i = 0; i < 5; i++)
             {
                 var bluetoothDeviceData = new BluetoothDeviceData("Device " + i, "Address " + i, true);
                 ListDevices.Add(bluetoothDeviceData);
                 
                 bluetoothDeviceData = new BluetoothDeviceData("Device " + i, "Address " + i, false);
                 ListDevices.Add(bluetoothDeviceData);
             }
             
             // Display the paired devices
             bluetoothDevicesPopup.DisplayDevices();
         }

         
         private void Awake()
         {
             // Ensure only one instance exists
             if (Instance == null)
             {
                 Instance = this;
                 DontDestroyOnLoad(gameObject);
                 
                 // Set the gateway
                 _SetGateway();
             }
             else
             {
                 // Destroy duplicates
                 Destroy(gameObject);
             }
         }

         private void _SetGateway()
         {
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
                     new [] { 
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
         
         private void _UnregisterBluetoothReceiver()
         {
             // Unregister the receiver
             if (_bluetoothReceiver != null)
             {
                 // Register the receiver with the Android system
                 using (AndroidJavaObject unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                 {
                     using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                     {
                         // Unregister the receiver
                         _bluetoothReceiver.Call("unregister", currentActivity);
                     }
                 }
             }
         }

         public void ConnectToDevice(string deviceAddress)
         {
             Debug.Log("Connecting to device: " + deviceAddress);
             
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
            ListDevices = new List<BluetoothDeviceData>();
            
            // Use the Android Bluetooth API to discover devices
            var devices = _bluetoothAdapter.Call<AndroidJavaObject>("getBondedDevices");

            var deviceArray = devices.Call<AndroidJavaObject[]>("toArray");

            foreach (var device in deviceArray)
            {
                var deviceName = device.Call<string>("getName");
                var deviceAddress = device.Call<string>("getAddress");
                
                // Create a dictionary with the device info
                var deviceInfo = new BluetoothDeviceData(deviceName, deviceAddress, true);
                
                // Add the dictionary to the list
                ListDevices.Add(deviceInfo);
            }
            
            // Display the paired devices
            bluetoothDevicesPopup.DisplayDevices();
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
         
         // Method called from CustomBluetoothReceiver plugin
         public void HandleDeviceDiscovered(string deviceInfo)
         {
             var deviceInfoArray = deviceInfo.Split('|');
             var deviceInfoDictionary = new BluetoothDeviceData();
            
             // Cast the device info to a dictionary
             if (deviceInfoArray[0] == "null" || deviceInfoArray[1] == "null")
             {
                 return;
             }
             
             deviceInfoDictionary.Name = deviceInfoArray[0];
             deviceInfoDictionary.Address = deviceInfoArray[1];

             if (ListDevices.TrueForAll(d => d.Address != deviceInfoDictionary.Address))
             {
                 ListDevices.Add(deviceInfoDictionary);
             }
             
             // Display the paired devices
             bluetoothDevicesPopup.DisplayDevices();
         }

         private void _ScanForAvailableDevices()
         {
             ListDevices = new List<BluetoothDeviceData>();
             
             _StartDiscovery();

             // Wait for 5 seconds
             Invoke(nameof(_StopDiscovery), 5f);
         }
         
         private void OnApplicationQuit()
         {
             // Close the Bluetooth socket
             _bluetoothSocket?.Call("close");
             
             // Unregister the receiver
             _UnregisterBluetoothReceiver();
         }
    }

}


