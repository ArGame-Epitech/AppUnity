using UnityEngine;
using UnityEngine.Events;

namespace Code.Scripts
{
    public class BluetoothDeviceButtonClickHandler: MonoBehaviour
    {
        
        public UnityEvent<string> onClickEvent;

        private BluetoothDeviceComponent _bluetoothDeviceComponent;

        private void Awake()
        {
            // Get a reference to the BluetoothDeviceComponennt component on the same GameObject.
            _bluetoothDeviceComponent = GetComponent<BluetoothDeviceComponent>();
        }

        public void RaiseOnClickEvent()
        {
            Debug.Log("BluetoothDeviceButtonClickHandler: onClickEvent raised");
            onClickEvent?.Invoke(_bluetoothDeviceComponent.BluetoothDeviceData.Address);
        }
        
    }
}