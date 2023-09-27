package com.argames.bluetooth;

import java.util.UUID;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothProfile;
import android.util.Log;

public class CustomBluetoothGattCallback extends BluetoothGattCallback {

    @Override
    public void onConnectionStateChange(BluetoothGatt gatt, int status, int newState) {
        super.onConnectionStateChange(gatt, status, newState);

        // Handle connection state changes (connected, disconnected, etc.)
        if (newState == BluetoothProfile.STATE_CONNECTED) {
            // Discover GATT services and characteristics
            gatt.discoverServices();
        }
    }

    @Override
    public void onServicesDiscovered(BluetoothGatt gatt, int status) {
        super.onServicesDiscovered(gatt, status);
        
        // Log status
        Log.d("CustomBluetoothGattCallback", "onServicesDiscovered: " + status + " -> " + BluetoothGatt.GATT_SUCCESS);

        if (status == BluetoothGatt.GATT_SUCCESS) {
            // Adding GATT services and characteristics
            BluetoothGattCharacteristic characteristic = gatt.getService(UUID.fromString("e94b456b-3786-4f1a-9c2a-a495cc4bc48f"))
                    .getCharacteristic(UUID.fromString("ca49ea0c-4bd7-11ee-be56-0242ac120001"));

            // Read or write data to the characteristic
            // gatt.readCharacteristic(characteristic);
            // gatt.writeCharacteristic(characteristic);
        }
    }
}
