using System;
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
         
         public List<BluetoothDeviceData> ListDevices { get; private set; } = new List<BluetoothDeviceData>();
         
         private AndroidJavaObject _bluetoothAdapter;
         private AndroidJavaObject _bluetoothSocket;
         private AndroidJavaObject _bluetoothReceiver;
         private AndroidJavaObject _bluetoothGatt;
         
         public BluetoothDevicesPopup bluetoothDevicesPopup;

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
                 // Enable _bluetoothAdapter
                 _bluetoothAdapter.Call<bool>("enable");
                 
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
             // Get the Bluetooth device by its address
             var device = _bluetoothAdapter.Call<AndroidJavaObject>("getRemoteDevice", deviceAddress);
                 
             // Create a BluetoothGattCallback instance (check the CustomBluetoothGattCallback class)
             var gattCallback = new AndroidJavaObject("com.argames.bluetooth.CustomBluetoothGattCallback");
                 
             // Get current context
             using (AndroidJavaObject unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
             {
                 using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                 {
                     // Connect to the device through the BluetoothGattCallback instance
                     _bluetoothGatt = device.Call<AndroidJavaObject>("connectGatt", currentActivity, false, gattCallback);
                 }
             }
         }
         
         // Method to scan for available Bluetooth devices
         private void _ScanForPairedDevices()
         {
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


