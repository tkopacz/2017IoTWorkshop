'use strict';

var Client = require('azure-iothub').Client;

var connectionString = 'HostName=pltkdpepliot2016.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=V65o7wxP6NispZ+IYef7i4hnMBDk5c6PAd0uUrtdr2k=';
var methodName = 'writeLine';
var deviceId = 'myDeviceId';

var client = Client.fromConnectionString(connectionString);

 var methodParams = {
     methodName: methodName,
     payload: 'a line to be written',
     timeoutInSeconds: 30
 };

 client.invokeDeviceMethod(deviceId, methodParams, function (err, result) {
     if (err) {
         console.error('Failed to invoke method \'' + methodName + '\': ' + err.message);
     } else {
         console.log(methodName + ' on ' + deviceId + ':');
         console.log(JSON.stringify(result, null, 2));
     }
 });