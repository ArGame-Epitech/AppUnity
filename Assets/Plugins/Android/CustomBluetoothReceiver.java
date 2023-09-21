package com.argames.bluetooth;

import com.unity3d.player.UnityPlayer;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.util.Log;

public class CustomBluetoothReceiver extends BroadcastReceiver {

    public CustomBluetoothReceiver() {
        super();
        Log.d("CustomBluetoothReceiver", "Created");
    }

    @Override
    public void onReceive(Context context, Intent intent) {
        String action = intent.getAction();

        Log.d("CustomBluetoothReceiver", "Received action: " + action);        

        if (BluetoothDevice.ACTION_FOUND.equals(action)) {
            BluetoothDevice device = intent.getParcelableExtra(BluetoothDevice.EXTRA_DEVICE);
            String deviceName = device.getName();
            String deviceAddress = device.getAddress();
            
            UnityPlayer.UnitySendMessage("AndroidBluetoothConnectionGateway", "HandleDeviceDiscovered", deviceName + "|" + deviceAddress);   
        }
    }
    
    public void register(Context context, IntentFilter filter) {
       context.registerReceiver(this, filter);
    }
    
    public void unregister(Context context) {
        context.unregisterReceiver(this);
    }
    
}
