using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using MPSLibrary;
using MPSLibrary.Interfaces;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Threading;

public class Chat : MonoBehaviour
{
    private string ip;
    private int port;
    public InputField ipInput;
    public InputField portInput;
    public Button connectButton;
    public Button reconnectButton;
    public Text globalText;
    public Button sendButton;
    public InputField myMessage;
    private Socket socket;
    private string globalString;
    private Thread receiver;
    private System.Object locker = new System.Object();
    private void SendMyMessage()
    {
        if (myMessage.text.Equals(""))
        {
            return;
        }
        socket.Send(Encoding.ASCII.GetBytes(myMessage.text),
            Encoding.ASCII.GetBytes(myMessage.text).Length,
            SocketFlags.None);
        lock (locker)
        {
            globalString += "\nMe: " + myMessage.text;
            myMessage.text = "";
        }
    }
    private void SendMyMessage(string message)
    {
        if (Input.GetButtonDown("Submit"))
        {
            SendMyMessage();
        }
    }
    private void HideUI()
    {
        sendButton.transform.localScale = new Vector3(0, 0, 0);
        reconnectButton.transform.localScale = new Vector3(0, 0, 0);
        myMessage.transform.localScale = new Vector3(0, 0, 0);
        globalText.transform.localScale = new Vector3(0, 0, 0);
    }
    private void ShowUI()
    {

        connectButton.transform.localScale = new Vector3(0, 0, 0);
        reconnectButton.transform.localScale = new Vector3(0, 0, 0);
        ipInput.transform.localScale = new Vector3(0, 0, 0);
        portInput.transform.localScale = new Vector3(0, 0, 0);
        sendButton.transform.localScale = new Vector3(1, 1, 1);
        myMessage.transform.localScale = new Vector3(1, 1, 1);
        globalText.transform.localScale = new Vector3(1, 1, 1);
    }
    void Awake()
    {
        sendButton.GetComponent<Button>().onClick.AddListener(SendMyMessage);
        myMessage.GetComponent<InputField>().onEndEdit.AddListener(SendMyMessage);
        connectButton.GetComponent<Button>().onClick.AddListener(ConnectToServer);
        reconnectButton.GetComponent<Button>().onClick.AddListener(Reconnect);
        HideUI();
        globalString = globalText.text;
    }

    private void Reconnect()
    {
        if (receiver != null)
            receiver.Abort();
        if (socket != null)
        {
            socket.Close();
        }
        ConnectToServer();
    }
    void Update()
    {
        if (socket != null && socket.Connected == false  && reconnectButton.transform.localScale ==new Vector3(0, 0, 0))
        {
            globalString += "\nYou have been disconnected. Please reconnect.";
            sendButton.transform.localScale = new Vector3(0,0,0);
            reconnectButton.transform.localScale = new Vector3(1, 1, 1);
        }
        lock (locker) {
            if (globalString.Split('\n').Length > 14)
            {
                globalString = globalString.Remove(0, globalString.IndexOf('\n',1)+1);
            }
            }
        globalText.text = globalString;
    }
    private void Receiver()
    {
        byte[] bytes = new byte[1024];
        int size = 0;
        while (true)
        {
            if (socket.Connected)
            {
                while ((size = socket.Receive(bytes, SocketFlags.None)) > 0)
                {
                    if (bytes[0] == 0)
                    {
                        lock (locker)
                        {

                            socket.Send(bytes);
                            globalString += "\nOther user has been disconnected.";
                        }
                        socket.Close();
                        return;
                    }
                    else
                    {
                        lock (locker)
                        {

                            globalString += "\n" + Encoding.ASCII.GetString(bytes, 0, size);
                        }

                    }
                }
            }
            else
            {
                return;
            }
        }
    }
    void OnApplicationQuit() {
        if (receiver != null)
            receiver.Abort();
        if (socket != null && socket.Connected == true)
        {
            byte[] closeInfo = new byte[1];
            closeInfo[0] = 0;
            socket.Send(closeInfo);
            socket.Close();
        }
    }

    private void ConnectToServer()
    {
        try
        {

            IPAddress ipAddress = IPAddress.Parse(ipInput.text);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, Int32.Parse(portInput.text));

            socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(remoteEP);
                receiver = new Thread(Receiver);
                receiver.Start();
                ShowUI();

            }
            catch (ArgumentNullException ane)
            {
                Debug.Log("ArgumentNullException : " + ane.ToString());
            }
            catch (SocketException se)
            {
                Debug.Log("SocketException : " + se.ToString());
            }
            catch (Exception e)
            {
                Debug.Log("Unexpected exception : " + e.ToString());
            }

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

}
