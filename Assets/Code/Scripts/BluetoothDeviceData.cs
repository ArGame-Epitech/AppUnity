using UnityEngine;

namespace Code.Scripts
{
    public class BluetoothDeviceData
    {
        public BluetoothDeviceData()
        {
            Name = "";
            Address = "";
            IsPaired = false;
        }
        
        public BluetoothDeviceData(string name, string address, bool isPaired)
        {
            Name = name;
            Address = address;
            IsPaired = isPaired;
        }

        public string Name { get; set; }
        public string Address { get; set; }
        public bool IsPaired { get; set; }

    }
}