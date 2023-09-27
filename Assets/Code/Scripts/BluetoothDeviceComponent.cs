using Code.Scripts.Infrastructure;
using UnityEngine;

namespace Code.Scripts
{
    public class BluetoothDeviceComponent: MonoBehaviour
    {

        public BluetoothDeviceData BluetoothDeviceData;
        
        private static AndroidBluetoothConnectionGateway Gateway => AndroidBluetoothConnectionGateway.Instance;

        public void Start()
        {
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(_OnDeviceButtonClicked);
        }
        
        private void _OnDeviceButtonClicked()
        {
            if (BluetoothDeviceData.IsPaired)
            {
                Gateway.ConnectToDevice(BluetoothDeviceData.Address);
            }
            else
            {
                Gateway.PairToDevice(BluetoothDeviceData.Address);   
            }
        }
    }
}