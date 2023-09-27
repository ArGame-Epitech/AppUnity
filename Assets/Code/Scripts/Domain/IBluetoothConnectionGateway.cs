using System.Collections.Generic;

namespace Code.Scripts.Domain
{
    public interface IBluetoothConnectionGateway
    {

        /// <summary>
        /// Instance of the gateway, a singleton.
        /// </summary>
        public static IBluetoothConnectionGateway Instance { get; private set; }
        
        /// <summary>
        /// List of devices discovered by the gateway, either paired or available.
        /// </summary>
        List<BluetoothDeviceData> ListDevices { get; }
        
        /// <summary>
        /// Sets the gateway (get permissions, create natives bluetooth objects and launch scan).
        /// </summary>
        void SetGateway();
        
        /// <summary>
        /// Scan for paired devices (stored in the device).
        /// </summary>
        void ScanForPairedDevices();
        
        /// <summary>
        /// Scan for available devices (not paired, but in range).
        /// </summary>
        void ScanForAvailableDevices();

        /// <summary>
        /// Connects to a device through its address. It plugs a gatt callback to the device.
        /// </summary>
        /// <param name="deviceAddress"></param>
        void ConnectToDevice(string deviceAddress);
        
        /// <summary>
        /// Handles the discovery of a Bluetooth device.
        /// </summary>
        /// <param name="deviceInfo">Information about the discovered device, generally split by a char.</param>
        void HandleDeviceDiscovered(string deviceInfo);
        
        /// <summary>
        /// Destroys the gateway by closing the gatt connection and killing the gateway.
        /// </summary>
        void KillGateway();

    }  
}

