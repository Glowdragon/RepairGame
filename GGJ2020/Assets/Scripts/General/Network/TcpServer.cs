﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;


public class TcpServer : MonoBehaviour
{
	private TcpClient client;
	private Thread serverThread;
	private TcpListener tcpListener;
	private byte[] buffer;

	public int Port = 12345;

	public bool SendData = false;
	public game_state State = new game_state();

	[SerializeField]
	private RecEvent OnRecieve;

	// Start is called before the first frame update
	void Start()
	{
		serverThread = new Thread(new ThreadStart(MasterListen));
		serverThread.IsBackground = true;
		serverThread.Start();
	}

	// Update is called once per frame
	void Update()
	{
		if (SendData)
		{
			SendData = false;
			var data = JsonUtility.ToJson(State);
			MasterWrite(NetworkUtility.ToNetwork(State));
			Debug.Log("Master Sent: " + data);
		}
	}


	void MasterListen()
	{
		try
		{
			tcpListener = new TcpListener(IPAddress.Any, Port);
			tcpListener.Start();
			Debug.Log("Server is listening on " + MasterIp + ":" + Port);

			client = tcpListener.AcceptTcpClient();
			var stream = client.GetStream();

			buffer = new byte[2048];

			while (true)
			{
				if (!stream.CanRead)
				{
					Debug.Log("Master Cannot Read");
					continue;
				}
				if (stream.DataAvailable)
				{
					int l = stream.Read(buffer, 0, buffer.Length);
					Debug.Log("Master read " + l + "Bytes");
					var rec = NetworkUtility.FromNetwork(Encoding.ASCII.GetString(buffer));


					OnRecieve.Invoke(rec);

				}
			}
		}
		catch (SocketException)
		{
			Debug.Log("Master Network error");
		}

	}

	void MasterWrite(string message)
	{
		if (client == null)
		{
			Debug.Log("No Client connected.");
			return;
		}

		try
		{
			var stream = client.GetStream();
			if (!stream.CanWrite)
			{
				Debug.Log("Master Cannot write to stream");
				return;
			}

			byte[] messageBytes = Encoding.ASCII.GetBytes(message);
			stream.Write(messageBytes, 0, messageBytes.Length);
		}
		catch (SocketException)
		{
			Debug.Log("Master Sending Failed");
		}
	}
}
