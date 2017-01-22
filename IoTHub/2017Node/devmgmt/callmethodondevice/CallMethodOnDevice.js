'use strict';

var Client = require('azure-iothub').Client;
var Registry = require('azure-iothub').Registry;
var JobClient = require('azure-iothub').JobClient;
var uuid = require('uuid');

var connectionString = 'HostName=pltkdpepliot2016.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=V65o7wxP6NispZ+IYef7i4hnMBDk5c6PAd0uUrtdr2k=';
var methodName = 'writeLine';
var deviceId = 'myDeviceId';
var deviceToUpdate = 'myDeviceId';

var client = Client.fromConnectionString(connectionString);
var registry = Registry.fromConnectionString(connectionString);


//--------------------------------------------------------------------------------------------------------------
//------------Just invoke------------------------------------------
var methodParams = {
    methodName: methodName,
    payload: 'a line to be written',
    timeoutInSeconds: 30
};


// client.invokeDeviceMethod(deviceId, methodParams, function (err, result) {
//     if (err) {
//         console.error('Failed to invoke method \'' + methodName + '\': ' + err.message);
//     } else {
//         console.log(methodName + ' on ' + deviceId + ':');
//         console.log(JSON.stringify(result, null, 2));
//     }
// });


//--------------------------------------------------------------------------------------------------------------
//------------Reboot + twin + info------------------------------------------

var deviceToReboot = 'myDeviceId';

var startRebootDevice = function (twin) {

    var methodName = "reboot";

    var methodParams = {
        methodName: methodName,
        payload: null,
        timeoutInSeconds: 30
    };

    client.invokeDeviceMethod(deviceToReboot, methodParams, function (err, result) {
        if (err) {
            console.error("Direct method error: " + err.message);
        } else {
            console.log("Successfully invoked the device to reboot.");
        }
    });
};

var queryTwinLastReboot = function () {

    registry.getTwin(deviceToReboot, function (err, twin) {

        if (twin.properties.reported.iothubDM != null) {
            if (err) {
                console.error('Could not query twins: ' + err.constructor.name + ': ' + err.message);
            } else {
                var lastRebootTime = twin.properties.reported.iothubDM.reboot.lastReboot;
                console.log('Last reboot time: ' + JSON.stringify(lastRebootTime, null, 2));
            }
        } else
            console.log('Waiting for device to report last reboot time.');
    });
};


//--------------------------------------------------------------------------------------------------------------
//------------Firmware update------------------------------------------


var startFirmwareUpdateDevice = function () {
    var params = {
        fwPackageUri: 'https://secureurl'
    };

    var methodName = "firmwareUpdate";
    var payloadData = JSON.stringify(params);

    var methodParams = {
        methodName: methodName,
        payload: payloadData,
        timeoutInSeconds: 30
    };

    client.invokeDeviceMethod(deviceToUpdate, methodParams, function (err, result) {
        if (err) {
            console.error('Could not start the firmware update on the device: ' + err.message)
        }
    });
};

var queryTwinFWUpdateReported = function () {
    registry.getTwin(deviceToUpdate, function (err, twin) {
        if (err) {
            console.error('Could not query twins: ' + err.constructor.name + ': ' + err.message);
        } else {
            console.log((JSON.stringify(twin.properties.reported.iothubDM.firmwareUpdate)) + "\n");
        }
    });
};
//--------------------------------------------------------------------------------------------------------------
//------------Schedule Job------------------------------------------

function monitorJob(jobId, callback) {
    var jobMonitorInterval = setInterval(function () {
        jobClient.getJob(jobId, function (err, result) {
            if (err) {
                console.error('Could not get job status: ' + err.message);
            } else {
                console.log('Job: ' + jobId + ' - status: ' + result.status);
                if (result.status === 'completed' || result.status === 'failed' || result.status === 'cancelled') {
                    clearInterval(jobMonitorInterval);
                    callback(null, result);
                }
            }
        });
    }, 5000);
}





//Call Reboot
//startRebootDevice();
//setInterval(queryTwinLastReboot, 2000);

//Call FW
//startFirmwareUpdateDevice();
//setInterval(queryTwinFWUpdateReported, 500);

//Call schedule
var uuid = require('uuid');

var startTime = new Date();
var maxExecutionTimeInSeconds = 3600;
var jobClient = JobClient.fromConnectionString(connectionString);

var methodParams = {
    methodName: 'lockDoor',
    payload: null,
    responseTimeoutInSeconds: 15 // Timeout after 15 seconds if device is unable to process method
};

var methodJobId = uuid.v4();
console.log('scheduling Device Method job with id: ' + methodJobId);
jobClient.scheduleDeviceMethod(methodJobId,
    queryCondition,
    methodParams,
    startTime,
    maxExecutionTimeInSeconds,
    function (err) {
        if (err) {
            console.error('Could not schedule device method job: ' + err.message);
        } else {
            monitorJob(methodJobId, function (err, result) {
                if (err) {
                    console.error('Could not monitor device method job: ' + err.message);
                } else {
                    console.log(JSON.stringify(result, null, 2));
                }
            });
        }
    });


//Play with twins
var query = registry.createQuery('SELECT * FROM devices', 100);
var onResults = function (err, results) {
    if (err) {
        console.error('Failed to fetch the results: ' + err.message);
    } else {
        // Do something with the results
        results.forEach(function (twin) {
            console.log(twin.deviceId);
        });

        if (query.hasMoreResults) {
            query.nextAsTwin(onResults);
        }
    }
};
query.nextAsTwin(onResults);


var twinJobId = uuid.v4();

var queryCondition = "deviceId IN ['myDeviceId']";
var twinPatch = {
    etag: '*'
    ,
     desired: {
         building: '43',
         floor: 3
     }
};

console.log('scheduling Twin Update job with id: ' + twinJobId);
jobClient.scheduleTwinUpdate(twinJobId,
    queryCondition,
    twinPatch,
    startTime,
    maxExecutionTimeInSeconds,
    function (err) {
        if (err) {
            console.error('Could not schedule twin update job: ' + err.message);
        } else {
            monitorJob(twinJobId, function (err, result) {
                if (err) {
                    console.error('Could not monitor twin update job: ' + err.message);
                } else {
                    console.log(JSON.stringify(result, null, 2));
                }
            });
        }
    });
