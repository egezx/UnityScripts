using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO.Ports;


public class Accelerometer : MonoBehaviour {

    public string COM;

    public float speed;
    public int arrLen;

    private SerialPort serialPort;
    private bool serialOK = false;

    private Vector3 nextRot;

    private Vector3[] angleBuffer;
    private int bufIndex = 0;

	void Start () 
    {

        serialPort = new SerialPort(COM, 9600,Parity.None, 8,StopBits.One);
        angleBuffer= new Vector3[arrLen];

        try
        {
            serialPort.RtsEnable = true;
            serialPort.Open();
            serialOK = true;
            Debug.Log("Serial OK");
        }
        catch (Exception)
        {
            Debug.LogError("Failed to open serial port for accelero-sensor.");
        }
	}

    void ReadSerial()
    {
        string dataString = serialPort.ReadLine();
        var dataBlocks = dataString.Split(',');

        if (dataBlocks.Length < 3)
        {
            Debug.LogWarning("Invalid data received");
            return; 
        }

        int angleX, angleY, angleZ;

        if (!int.TryParse(dataBlocks[0], out angleX))
        {
            Debug.LogWarning("Failed to parse angleX. RawData: " + dataBlocks[0]);
            return;
        }
        if (!int.TryParse(dataBlocks[1], out angleY))
        {
            Debug.LogWarning("Failed to parse angleY. RawData: " + dataBlocks[1]);
            return;
        }
        if (!int.TryParse(dataBlocks[2], out angleZ))
        {
            Debug.LogWarning("Failed to parse angleZ. RawData: " + dataBlocks[2]);
            return;
        }

        SetRotation(angleX, angleZ, angleY);
    }


    void SetRotation(int x, int y, int z)
    {
        Vector3 newRot = new Vector3((float)x, (float)y, (float)z);
        if (bufIndex < arrLen - 1)
        {
            angleBuffer[bufIndex] = newRot;
            bufIndex++;
        }
        else
        {
            var newArray = new Vector3[angleBuffer.Length];
            Array.Copy(angleBuffer, 1, newArray, 0, angleBuffer.Length - 1);
            newArray[angleBuffer.Length - 1] = newRot;

            angleBuffer = newArray;

            float X = 0f, Z = 0f;

            for (int i = 0; i < angleBuffer.Length; i++)
            {
                X += angleBuffer[i].x;
                Z += angleBuffer[i].z;
            }
            X /= (float)angleBuffer.Length;
            Z /= (float)angleBuffer.Length;

            nextRot = new Vector3(X,0f,Z);
        }    
    }
	
	void Update ()
    {
        if (serialOK)
        {
            try
            {
                ReadSerial();
            }
            catch (Exception)
            {
                Debug.LogWarning("Serial Failed");
            }
        }
		//Yeah.. Should not do it this way.
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(nextRot), Time.deltaTime * speed);
	}


}
