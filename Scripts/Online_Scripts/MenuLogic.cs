using AssemblyCSharp;
using com.shephertz.app42.gaming.multiplayer.client;
using com.shephertz.app42.gaming.multiplayer.client.events;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MenuLogic : MonoBehaviour
{
    #region Variables
    private string apiKey = "e4b1ddc0c08cd7ad5089e5e835d06ea90b1bc682dc493ad9a9ee835cddf67988"; //ziv
    private string secretKey = "ff08234dadfe5c908d0ef3a5d1949bf2a6e8de51f6b0724ce255002c32184570";  //ziv
    private Listener myListner;
    public GameObject game;
    public GameObject menu;
    public Slider diffSlider;
    public TextMeshProUGUI diffText;
    private float cachedSliderValue;
    private Dictionary<string, GameObject> unityObjects;
    public TextMeshProUGUI curStatus;
    public TextMeshProUGUI curRoomId;
    public TextMeshProUGUI curUserId;
    public Button setRoundsButton;     // Btn_SetRounds button
    public GameObject Button_Play;
    public GameControl GameControl;
    public TextMeshProUGUI txt_userId;
    private List<string> roomIds;
    private int maxUsers = 2;
    private string roomId = string.Empty;
    private Dictionary<string, object> matchRoomData;
    private string password;
    private int roomIndex = 0;
    #endregion
    #region Monobehaviour
    private void OnEnable()
    {
        Listener.OnConnect += OnConnect;
        Listener.OnRoomsInRange += OnRoomsInRange;
        Listener.OnCreateRoom += OnCreateRoom;
        Listener.OnJoinRoom += OnJoinRoom;
        Listener.OnGetLiveRoomInfo += OnGetLiveRoomInfo;
        Listener.OnUserJoinRoom += OnUserJoinRoom;
        Listener.OnGameStarted += OnGameStarted;
    }
    private void OnDisable()
    {
        Listener.OnConnect -= OnConnect;
        Listener.OnRoomsInRange -= OnRoomsInRange;
        Listener.OnCreateRoom -= OnCreateRoom;
        Listener.OnJoinRoom -= OnJoinRoom;
        Listener.OnGetLiveRoomInfo -= OnGetLiveRoomInfo;
        Listener.OnUserJoinRoom -= OnUserJoinRoom;
        Listener.OnGameStarted -= OnGameStarted;
    }
    private void Awake()
    {
        unityObjects = new Dictionary<string, GameObject>();
        GameObject[] foundObjects = GameObject.FindGameObjectsWithTag("UnityObject");
        foreach (GameObject obj in foundObjects)
        {
            unityObjects.Add(obj.name, obj);
        }
        matchRoomData = new Dictionary<string, object>();
        myListner = new Listener();
        WarpClient.initialize(apiKey, secretKey);
        WarpClient.GetInstance().AddConnectionRequestListener(myListner);
        WarpClient.GetInstance().AddChatRequestListener(myListner);
        WarpClient.GetInstance().AddUpdateRequestListener(myListner);
        WarpClient.GetInstance().AddLobbyRequestListener(myListner);
        WarpClient.GetInstance().AddNotificationListener(myListner);
        WarpClient.GetInstance().AddRoomRequestListener(myListner);
        WarpClient.GetInstance().AddZoneRequestListener(myListner);
        WarpClient.GetInstance().AddTurnBasedRoomRequestListener(myListner);
    }
    void Start()
    {
        UpdateRoundsText(diffSlider.value);
        diffSlider.interactable = true;
        // Disable the Play button initially
        unityObjects["Btn_Play"].GetComponent<Button>().interactable = false;
        diffSlider.onValueChanged.AddListener(delegate { UpdateRoundsText(diffSlider.value); });
        // Generate a unique user ID and display it
        GlobalVariables.UserId = System.Guid.NewGuid().ToString();
        curUserId.text = GlobalVariables.UserId;

        // Add listener to the Set Rounds button
        setRoundsButton.onClick.AddListener(OnSetRoundsButtonPressed);
    }
    void UpdateRoundsText(float sliderValue)
    {
        int rounds = Mathf.RoundToInt(sliderValue);
        diffText.text = "Rounds: " + rounds.ToString();
    }
    // Method called when the Set Rounds button is pressed
    public void OnSetRoundsButtonPressed()
    {
        // Retrieve the value from the slider
        float cachedSliderValue = diffSlider.value;

        // Convert the slider value to an integer (if needed)
        int rounds = Mathf.RoundToInt(cachedSliderValue);

        // Use the slider value as the password
        password = rounds.ToString();

        // Update or add the password in matchRoomData
        if (matchRoomData.ContainsKey("Password"))
        {
            matchRoomData["Password"] = password;
        }
        else
        {
            matchRoomData.Add("Password", password);
        }

        // Make the Play button interactable
        unityObjects["Btn_Play"].GetComponent<Button>().interactable = true;

        // Optionally, disable the slider and Set Rounds button to prevent further changes
        diffSlider.interactable = false;
        setRoundsButton.interactable = false;

        // Optional: Provide feedback to the user
        Debug.Log("Rounds set to: " + rounds);
    }

public void TryToConnect()
    {
        Button_Play.SetActive(true);
        curRoomId.gameObject.SetActive(true);
        curStatus.gameObject.SetActive(true);
        curUserId.gameObject.SetActive(true);
        Debug.Log($"Attempting to connect with User ID: {GlobalVariables.UserId}");
        WarpClient.GetInstance().Connect(GlobalVariables.UserId);
        UpdateStatus("Connecting...");
    }

    public void UpdatePasswordValue()
    {
        // Update the cached value when slider changes
        cachedSliderValue = diffSlider.value;

        // Optionally update the password in matchRoomData immediately
        string password = cachedSliderValue.ToString();
        matchRoomData["Password"] = password;
    }
    private void OnConnect(bool _IsSuccess)
    {
        Debug.Log("OnConnect Callback: " + (_IsSuccess ? "Success" : "Failure"));
        if (_IsSuccess)
            UpdateStatus("Connected!");
        else
            UpdateStatus("Couldn't Connect...");
        unityObjects["Btn_Play"].GetComponent<Button>().interactable = _IsSuccess;
    }
    private void OnRoomsInRange(bool _IsSuccess, MatchedRoomsEvent eventObj)
    {
        if (_IsSuccess)
        {
            UpdateStatus("Room found.");
            roomIds = new List<string>();
            foreach (var roomData in eventObj.getRoomsData())
            {
                roomIds.Add(roomData.getId());
            }
        }
        else
        UpdateStatus("Couldn't find a room.");
        roomIndex = 0;
        DoRoomSearchLogic();
    }
    private void OnCreateRoom(bool _IsSuccess, string _RoomId)
    {
        Debug.Log("OnCreateRoom " + _IsSuccess + ", roomId: " + _RoomId);
        if (_IsSuccess)
        {
            JoinRoomLogic(_RoomId, "Room was created (" + _RoomId + "), waiting for an opponent");
        }
    }
    private void OnJoinRoom(bool _IsSuccess, string _RoomId)
    {
        if (_IsSuccess)
        {
            UpdateStatus("Joined Room: " + _RoomId);
            curRoomId.text = "RoomId: " + _RoomId;
        }
        else UpdateStatus("Failed to join Room: " + _RoomId);
    }
    private void OnGetLiveRoomInfo(LiveRoomInfoEvent eventObj)
    {
        if (eventObj != null && eventObj.getProperties() != null)
        {
            Dictionary<string, object> properties = eventObj.getProperties();
            if (properties.ContainsKey("Password") &&
                properties["Password"].ToString() == matchRoomData["Password"].ToString())
            {
                JoinRoomLogic(eventObj.getData().getId(), "Received Room Info, joining room: " + eventObj.getData().getId());
            }
            else
            {
                roomIndex++;
                DoRoomSearchLogic();
            }
        }
    }
    private void OnUserJoinRoom(RoomData eventObj, string joinedUserId)
    {
        if (GlobalVariables.UserId == eventObj.getRoomOwner() && GlobalVariables.UserId != joinedUserId)
        {
            //StartCoroutine(DelayStartGame(1f));
            WarpClient.GetInstance().startGame();
        }
    }
    private void OnGameStarted(string sender, string roomId, string curTurn)
    {
        UpdateStatus("Game Started, Current Turn: " + curTurn);
        curUserId.gameObject.SetActive(true);
        txt_userId.text = "UserId: " + curUserId.text;
        menu.SetActive(false);
        game.SetActive(true);
        GameControl.Init();
    }
    #endregion
    #region Logic
    private void UpdateStatus(string newString)
    {
        curStatus.gameObject.SetActive(true);
        curStatus.text = "Status: " + newString;
    }
    internal void Btn_Play()
    {
        unityObjects["Btn_Play"].GetComponent<Button>().interactable = false;
        WarpClient.GetInstance().GetRoomsInRange(1, 2);
        UpdateStatus("Searching for a room...");
    }
    private void JoinRoomLogic(string newRoomId, string message)
    {
        roomId = newRoomId;
        UpdateStatus(message);
        WarpClient.GetInstance().JoinRoom(roomId);
        WarpClient.GetInstance().SubscribeRoom(roomId);
    }
    private void DoRoomSearchLogic()
    {
        if (roomIndex < roomIds.Count)
        {
            WarpClient.GetInstance().GetLiveRoomInfo(roomIds[roomIndex]);
        }
        else
        {
            UpdateStatus("Creating Room...");
            WarpClient.GetInstance().CreateTurnRoom("Ziv Room", GlobalVariables.UserId, maxUsers,
                matchRoomData, GlobalVariables.TurnTime);
        }
    }
    private IEnumerator DelayStartGame(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdateStatus("Starting Game...");
        WarpClient.GetInstance().startGame();
    }
    #endregion
}
