using UnityEngine;
using System.Collections.Generic;
using Code.Scripts.Domain;
using Code.Scripts.View;
using UnityEngine.Android;

namespace Code.Scripts.Infrastructure
{
    public class AndroidBluetoothConnectionGateway : MonoBehaviour, IBluetoothConnectionGateway
     {
         // Singleton instance for easy access
         public static AndroidBluetoothConnectionGateway Instance { get; private set; }
         
         public List<BluetoothDeviceData> ListDevices { get; } = new List<BluetoothDeviceData>();
         
         private AndroidJavaObject _bluetoothAdapter;
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
                 SetGateway();
             }
             else
             {
                 // Destroy duplicates
                 Destroy(gameObject);
             }
         }

         /// <summary>
         /// Sets the gateway. It is called when the gateway is created. It gets the permissions, creates the
         /// bluetooth adapter and registers the bluetooth receiver before starting the discovery process.
         /// </summary>
         public void SetGateway()
         {
             // Call the method to grant permissions
             _GrantPermission();
             
             // Create an instance of the Android Bluetooth Adapter
             var bluetoothAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter");
             _bluetoothAdapter = bluetoothAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");
             
             if (_bluetoothAdapter != null)
             {
                 // Enable _bluetoothAdapter
                 _bluetoothAdapter.Call<bool>("enable");
                 
                 ScanForPairedDevices();
                 
                 _RegisterBluetoothReceiver();
             }
             else
             {
                 // Bluetooth is not available on the device
                 Debug.Log("Bluetooth is not available on this device.");
             }
         }

         /// <summary>
         /// Grants permissions to the application. It is called when the gateway is set.
         /// The permissions are granted with the Android API "Permission.RequestUserPermissions".
         /// </summary>
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
         
         /// <summary>
         /// Registers the Bluetooth receiver. It is called when the gateway is set. The registration is specific since
         /// we pass an intent filter to the receiver. The intent filter is used to filter the devices found by the
         /// receiver.
         /// </summary>
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
                     var foundFilter = new AndroidJavaObject("android.content.IntentFilter", "android.bluetooth.device.action.FOUND");
                     var bondStateChangedFilter = new AndroidJavaObject("android.content.IntentFilter", "android.bluetooth.device.action.BOND_STATE_CHANGED");
                        
                     // Register the receiver using the CustomBluetoothReceiver instance and the FOUND filter
                     _bluetoothReceiver.Call("register", currentActivity, foundFilter);
                     
                     // Register the receiver using the CustomBluetoothReceiver instance and the BOND_STATE_CHANGED filter
                     _bluetoothReceiver.Call("register", currentActivity, bondStateChangedFilter);
                     
                     // Once the receiver is registered, you can start the discovery process
                     ScanForAvailableDevices();
                 }
             }
         }
         
         /// <summary>
         /// Unregisters the Bluetooth receiver. It is called when the gateway is killed.
         /// </summary>
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
         
         /// <summary>
         /// Connects to a device through its address. It plugs a gatt callback to the device. The GattCallback
         /// is defined in the CustomBluetoothGattCallback class. The connection is made with the "connectGatt"
         /// method of the Android BluetoothDevice class.
         /// </summary>
         /// <param name="deviceAddress">Address of the device to connect with</param>
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
         
         /// <summary>
         /// Pair to a device through its address. It retrieves the device by its address and calls the
         /// "createBond" method of the Android BluetoothDevice class. It will ask the user to pair with
         /// the device. When the pairing is done, the CustomBluetoothReceiver will catch the event and
         /// send the device info to the HandleDeviceBonded method.
         /// </summary>
         /// <param name="deviceAddress">Address of the device to pair with</param>
         public void PairToDevice(string deviceAddress)
         {
             // Get the Bluetooth device by its address
             var device = _bluetoothAdapter.Call<AndroidJavaObject>("getRemoteDevice", deviceAddress);
             
             // Pair the device
             device.Call<bool>("createBond");
         }
         
         /// <summary>
         /// Scans for paired devices (stored in the device). It fills the ListDevices with the devices found
         /// with the Android method "getBondedDevices".
         /// </summary>
         public void ScanForPairedDevices()
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
         
         /// <summary>
         /// Starts the discovery process if it is not already running.
         /// </summary>
         private void _StartDiscovery()
         {
             var isDiscovering = _bluetoothAdapter.Call<bool>("isDiscovering");

             if (!isDiscovering)
             {
                 _bluetoothAdapter.Call<bool>("startDiscovery");
             }
         }

         /// <summary>
         /// Stops the discovery process if it is running.
         /// </summary>
         private void _StopDiscovery()
         {
             var isDiscovering = _bluetoothAdapter.Call<bool>("isDiscovering");
             
                if (isDiscovering)
                {
                    // Stop the discovery process
                    _bluetoothAdapter.Call<bool>("cancelDiscovery");
                }
         }
         
         /// <summary>
         /// Handles the discovery of a Bluetooth device. In the Android implementation,
         /// the method is called by the CustomBluetoothReceiver which sends the device info
         /// with a string containing the device name and address separated by a pipe.
         /// </summary>
         /// <param name="deviceInfo">Information about the discovered device split by a pipe.</param>
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

         /// <summary>
         /// Handles the pairing to a device. It changes the IsPaired value of the device in the ListDevices.
         /// </summary>
         /// <param name="deviceInfo">Information about the discovered device split by a pipe.</param>
         public void HandleDeviceBonded(string deviceInfo)
         {
             var deviceInfoArray = deviceInfo.Split('|');
             var deviceInfoDictionary = new BluetoothDeviceData();
            
             // Cast the device info to a dictionary
             if (deviceInfoArray[0] == "null" || deviceInfoArray[1] == "null")
             {
                 return;
             }
             
             deviceInfoDictionary.Address = deviceInfoArray[1];
             
             // Replace BluetoothDeviceData IsPaired value
             foreach (var deviceData in ListDevices)
             {
                 if (deviceData.Address == deviceInfoDictionary.Address)
                 {
                     deviceData.IsPaired = true;
                 }
             }
             
             // Display the devices again
             bluetoothDevicesPopup.DisplayDevices();
             
             // Connect to the device
             ConnectToDevice(deviceInfoDictionary.Address);
         }

         /// <summary>
         /// Scan for available devices (not paired, but in range).
         /// It starts the discovery process and stops it after 5 seconds.
         /// </summary>
         public void ScanForAvailableDevices()
         {
             _StartDiscovery();

             // Wait for 5 seconds
             Invoke(nameof(_StopDiscovery), 5f);
         }

         /// <summary>
         /// Kills the gateway by closing the gatt connection and unregistering the bluetooth receiver.
         /// </summary>
         public void KillGateway()
         {
             // Close the Bluetooth GATT
             _bluetoothGatt?.Call("close");
             
             // Unregister the receiver
             _UnregisterBluetoothReceiver();
         }
         
         private void OnApplicationQuit()
         {
             KillGateway();
         }
         
         private void OnDestroy()
         {
             KillGateway();
         }
    }

}


