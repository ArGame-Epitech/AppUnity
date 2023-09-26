using UnityEngine;

namespace Code.Scripts
{
    public class BluetoothDeviceComponent: MonoBehaviour
    {

        public BluetoothDeviceData BluetoothDeviceData;

        BluetoothDeviceComponent()
        {
            BluetoothDeviceData = new BluetoothDeviceData();
        }
        
    }
}