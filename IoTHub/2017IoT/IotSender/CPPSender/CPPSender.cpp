// CPPSender.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "CPPSender.h"

/*String containing Hostname, Device Id & Device Key in the format:                         */
/*  "HostName=<host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"                */
/*  "HostName=<host_name>;DeviceId=<device_id>;SharedAccessSignature=<device_sas_token>"    */
static const char* connectionString = "HostName=pltkdpepliot2016.azure-devices.net;DeviceId=DeviceCPP;SharedAccessKey=iD3NCqzFFQiBFJAaeyhgeqTwAeJ5aye5UG2fIo0XEYU=";

typedef struct EVENT_INSTANCE_TAG
{
	IOTHUB_MESSAGE_HANDLE messageHandle;
	size_t messageTrackingId;  // For tracking the messages within the user callback.
} EVENT_INSTANCE;


IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle;

static int callbackCounter;
static char msgText[1024];
static char propText[1024];
static bool g_continueRunning;
#define MESSAGE_COUNT 5
#define DOWORK_LOOP_NUM     3
EVENT_INSTANCE messages[MESSAGE_COUNT];


static IOTHUBMESSAGE_DISPOSITION_RESULT ReceiveMessageCallback(IOTHUB_MESSAGE_HANDLE message, void* userContextCallback)
{
	int* counter = (int*)userContextCallback;
	const char* buffer;
	size_t size;

	if (IoTHubMessage_GetByteArray(message, (const unsigned char**)&buffer, &size) != IOTHUB_MESSAGE_OK)
	{
		(void)printf("unable to retrieve the message data\r\n");
	}
	else
	{
		(void)printf("Received Message [%d] with Data: <<<%.*s>>> & Size=%d\r\n", *counter, (int)size, buffer, (int)size);
		// If we receive the work 'quit' then we stop running
		if (size == (strlen("quit") * sizeof(char)) && memcmp(buffer, "quit", size) == 0)
		{
			g_continueRunning = false;
		}
	}

	// Retrieve properties from the message
	MAP_HANDLE mapProperties = IoTHubMessage_Properties(message);
	if (mapProperties != NULL)
	{
		const char*const* keys;
		const char*const* values;
		size_t propertyCount = 0;
		if (Map_GetInternals(mapProperties, &keys, &values, &propertyCount) == MAP_OK)
		{
			if (propertyCount > 0)
			{
				for (size_t index = 0; index < propertyCount; index++)
				{
					(void)printf("\tKey: %s Value: %s\r\n", keys[index], values[index]);
				}
				(void)printf("\r\n");
			}
		}
	}

	/* Some device specific action code goes here... */
	(*counter)++;
	return IOTHUBMESSAGE_ACCEPTED;
}

static void SendConfirmationCallback(IOTHUB_CLIENT_CONFIRMATION_RESULT result, void* userContextCallback)
{
	EVENT_INSTANCE* eventInstance = (EVENT_INSTANCE*)userContextCallback;
	(void)printf("Confirmation[%d] received for message tracking id = %zu with result = %s\r\n", callbackCounter, eventInstance->messageTrackingId, ENUM_TO_STRING(IOTHUB_CLIENT_CONFIRMATION_RESULT, result));
	/* Some device specific action code goes here... */
	callbackCounter++;
	IoTHubMessage_Destroy(eventInstance->messageHandle);
}

static int DeviceMethodCallback(const char* method_name, const unsigned char* payload, size_t size, unsigned char** response, size_t* resp_size, void* userContextCallback) {
	int status;
	printf("\r\nDevice Method called\r\n");
	printf("Device Method name:    %s\r\n", method_name);
	printf("Device Method payload: %.*s\r\n", (int)size, (const char*)payload);

	//unsigned char *ret = new unsigned char[100];
	char ret[100];
	size_t ret_size = 100;
	sprintf_s(ret, ret_size, "{\"MSG\":\"WORKING\"}");

	*resp_size = strlen(ret);
	if ((*response = (unsigned char *)malloc(*resp_size)) == NULL)
	{
		status = -1;
	}
	else
	{
		memcpy(*response, ret, *resp_size);
		status = 0;
	}
	return status;
}

int main()
{
	printf("IoTHub, MQTT...\r\n");

	if (platform_init() != 0)
	{
		printf("Failed to initialize the platform.\r\n");
		return -1;
	}

	iotHubClientHandle = IoTHubClient_LL_CreateFromConnectionString(connectionString, MQTT_Protocol);
	if (iotHubClientHandle == NULL) {
		printf("ERROR: iotHubClientHandle is NULL!\r\n");
		return -1;
	}
	bool traceOn = false;
	IoTHubClient_LL_SetOption(iotHubClientHandle, "logtrace", &traceOn);

	int receiveContext = 0;

	if (IoTHubClient_LL_SetMessageCallback(iotHubClientHandle, ReceiveMessageCallback, &receiveContext) != IOTHUB_CLIENT_OK)
	{
		printf("ERROR: IoTHubClient_LL_SetMessageCallback..........FAILED!\r\n");
		return -1;
	}

	if (IoTHubClient_LL_SetDeviceMethodCallback(iotHubClientHandle, DeviceMethodCallback, NULL) != IOTHUB_CLIENT_OK) {
		return-1;
	}

    size_t iterator = 0;
	float avgWindSpeed = 10;
	g_continueRunning = true;
    do
    {
        if (iterator < MESSAGE_COUNT)
        {
            sprintf_s(msgText, sizeof(msgText), "{\"deviceId\":\"DeviceCPPCallback\",\"windSpeed\":%.2f}", avgWindSpeed + (rand() % 4 + 2));
            if ((messages[iterator].messageHandle = IoTHubMessage_CreateFromByteArray((const unsigned char*)msgText, strlen(msgText))) == NULL)
            {
                (void)printf("ERROR: iotHubMessageHandle is NULL!\r\n");
            }
            else
            {
                messages[iterator].messageTrackingId = iterator;
                MAP_HANDLE propMap = IoTHubMessage_Properties(messages[iterator].messageHandle);
                (void)sprintf_s(propText, sizeof(propText), "PropMsg_%zu", iterator);
                if (Map_AddOrUpdate(propMap, "PropName", propText) != MAP_OK)
                {
                    (void)printf("ERROR: Map_AddOrUpdate Failed!\r\n");
                }

				if (IoTHubClient_LL_SendEventAsync(iotHubClientHandle, messages[iterator].messageHandle, SendConfirmationCallback, &messages[iterator]) != IOTHUB_CLIENT_OK)
				{
					printf("ERROR: IoTHubClient_LL_SendEventAsync..........FAILED!\r\n");
					return -1;
				}

				sprintf_s(msgText, sizeof(msgText), "{\"deviceId\":\"DeviceCPP\",\"windSpeed\":%.2f}", avgWindSpeed + (rand() % 4 + 2));
				IOTHUB_MESSAGE_HANDLE msg = IoTHubMessage_CreateFromByteArray((const unsigned char*)msgText, strlen(msgText));
				if (msg == NULL) {
					printf("ERROR: iotHubMessageHandle(2) is NULL!\r\n");
					return -1;
				}
				if (IoTHubClient_LL_SendEventAsync(iotHubClientHandle, msg, NULL, NULL) != IOTHUB_CLIENT_OK) {
					printf("ERROR: IoTHubClient_LL_SendEventAsync, NoCallback..........FAILED!\r\n");
					return -1;
				}
				IoTHubMessage_Destroy(msg);
			}
        }
        IoTHubClient_LL_DoWork(iotHubClientHandle);
        ThreadAPI_Sleep(1);

        iterator++;
    } while (g_continueRunning);

    printf("iothub_client_sample_mqtt has gotten quit message, call DoWork %d more time to complete final sending...\r\n", DOWORK_LOOP_NUM);
    for (size_t index = 0; index < DOWORK_LOOP_NUM; index++)
    {
        IoTHubClient_LL_DoWork(iotHubClientHandle);
        ThreadAPI_Sleep(1);
    }
    IoTHubClient_LL_Destroy(iotHubClientHandle);
    platform_deinit();
    return 0;
}

