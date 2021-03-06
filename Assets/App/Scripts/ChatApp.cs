﻿//************************************************************//
// using following credentials for PubNub in the chatapp
// publish key 	 => demo
// subscribe key => demo
// channel 		 => my_world
//************************************************************//

#if UNITY_WSA || UNITY_WSA_8_1 || UNITY_WSA_10_0
#else
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
#endif
using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using PubNubMessaging.Core;
using System;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using MiniJSON;

//used to create json object
[Serializable]
public class serializeJson
{
	public string text;
}

public class ChatApp : MonoBehaviour {
	
	[Header("Publish key")]
	[Tooltip("Enter publish key")]
	public String publishKey   = "demo";
	[Header("Enter subscribe key")]
	public String subscribeKey = "demo";

	[Header("Channel Name")]
	[Tooltip("enter channel name")]
	public String channelName= "my_world";

	[Space(15)]

	[Header("Other")]
	[Tooltip("chat bubble starts from this gameobjects transform")]
	public Transform chatBubble_origin;  //Origin point of populating the chat box with chat bubble
	[Tooltip("prefab layout of chat bubble")]
	public GameObject chatBubble_prefab; //Premade layout of chatbubble
	[Tooltip("status panel 'Connect_Status' reference")]
	public Button statusPanel;			 //access to "Connect_Status"
	[Tooltip("reference to chat input field")]
	public InputField inputField; 		 //Input field of chatbox
	[Tooltip("reference to scrollbar")]
	public ScrollRect scrollRect;		//reference to scrollbar

	private String sendMessage;         //json string object variable
	private String tempInputfield;		//store the text content of input field temporarly

	static Pubnub pubnub;

	void Start () 
	{
		pubnub = new Pubnub(publishKey, subscribeKey);
		Subscribe ();  //subscribe to channel "my_world"
	}
		
/***************************/
//User Defined Functions
/***************************/

    //Initialize Pubnub with keys
    public void init()
	{
		pubnub = new Pubnub (publishKey, subscribeKey);
	}

	//close application
	public void exitApp()
	{
		Application.Quit();
	}
		

    //Subscribe to channel
	public void Subscribe()
	{
		//subscribing to channel "my_world"
		pubnub.Subscribe<string>(
			channelName, 
			DisplaySubscribeReturnMessage,
			DisplaySubscribeConnectStatusMessage,
			DisplayErrorMessage
		);

	}

    //Publish to channel
	public void Publish()
	{
		if (inputField.text != "")  // check if the inputfield text is empty
		{
			pubnub.EnableJsonEncodingForPublish = false;  //disable pubnub json encoding
			serializeJson myObject = new serializeJson(); 
			myObject.text= inputField.text;				  // assigning input field text to "text"
			sendMessage = JsonUtility.ToJson(myObject);

			pubnub.Publish<string> (
				channelName,
				sendMessage,
				DisplayReturnMessage,
				DisplayErrorMessage
			);
       		DisplayChat(inputField.text,false); //pass the input field text to be published to the chatbox
		}
	}

	//display status of connection or error "status panel"
	public void DisplayStatus(String statusMessage, bool isSucess) 
	{
		//statusMessage => contains string/text
		//IsSucess      => to check if status message is sucess or error related
		statusPanel.GetComponentInChildren<Text> ().text = statusMessage;
		if (isSucess)  
		{
			statusPanel.GetComponent<Image> ().color = Color.green; //statusbtn color is changed to green in Sucess
		} 
		else
		{
			statusPanel.GetComponent<Image> ().color = Color.red;  //statusbtn color is changed to green in Failure
		}
	}

	//Clear input field of texts
	public void ClearField()
	{
		tempInputfield = inputField.text;
		inputField.text = "";
	}

	//Generate chatbubble in chatbox
	public void DisplayChat(String chatContent, bool isServer) //populates messages to chatbox using chatBubble_prefab
	{
		//chatContent => contains chat messages from user/server
		//isServer    => check if the respective chatContent is from server or not
		if (chatContent != "" && chatContent != inputField.text) 					  //check if passed string variable is null/empty
		{
			//instantiate or make copy of prefab and assign respective content to it
			GameObject newChatBubble = Instantiate (chatBubble_prefab) as GameObject; //creating a copy
			newChatBubble.transform.SetParent (chatBubble_origin, false);			  //Assign parent of the newly created element and keep its local orientation
			newChatBubble.GetComponentInChildren<Text> ().text = chatContent;		  //Assign the text the new element should contain

			if (isServer)															  //Check if chat message received is from server
            {
				newChatBubble.GetComponent<Image> ().color = Color.yellow;			  //Assign a colour to chat bubble
			}	
		}

		scrollBar_autoadjust ();													  //Autoadjust the scrollbar to show lastest message
	}

	//auto adjust scrollbar to show the latest message
	public void scrollBar_autoadjust()
	{
		Canvas.ForceUpdateCanvases();
		scrollRect.verticalNormalizedPosition =0f;
		Canvas.ForceUpdateCanvases();
	}



/***************************/
//PubNub Functions
/***************************/

void DisplaySubscribeReturnMessage(string result) {
		if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(result.Trim()))
		{
			List<object> deserializedMessage = pubnub.JsonPluggableLibrary.DeserializeToListOfObject(result);
			if (deserializedMessage != null && deserializedMessage.Count > 0)
			{
				object subscribedObject = (object)deserializedMessage[0];
				if (subscribedObject != null)
				{
					string resultActualMessage = pubnub.JsonPluggableLibrary.SerializeToJsonString(subscribedObject);

					//Strip json to obtain the chat or message received from server side
					//Json format {"text":"message"}
					var dict = Json.Deserialize(resultActualMessage) as Dictionary<string,object>;
					String serverMessage = (string)dict ["text"];
						
					if (serverMessage != tempInputfield)   //check if message origin is server or user side
					{ 
						DisplayChat (serverMessage, true); //pass it to function to display it in chatbox
					} 
					else 
					{
						DisplayChat (serverMessage, false); //pass it to function to display it in chatbox
					}
					DisplayStatus ("Connected ! channel : "+channelName, true);
				}
			}
		}
	}

	void DisplaySubscribeConnectStatusMessage(string result)
	{
		DisplayStatus ("Connected with UUID : " + pubnub.SessionUUID, true);
	}

	void DisplayErrorMessage(PubnubClientError pubnubError)
    {
		DisplayStatus ("Connection Failed ! Error Code : " + pubnubError.StatusCode, false);
	}

	void DisplayReturnMessage(string result)
	{
		//DisplayStatus ("Connected !", true);
	}
}
