using EyeXFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tobii.Interaction;
using WebSocket4Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StreamEnginee
{
    public class KirimDataEye
    {

        static string webSocketUri = "ws://192.168.43.129:8000/gazethru/";

        static void Main(string[] args)
        {
            //inisialisasi websocket (instantiasi object Websocket)
            var socketManager = new WebSocketManager(webSocketUri);

            //inisialisasi host Tobii Eyex (instantiasi object dari Host Tobii.Interaction)
            var host = new Host(); 

            //memasukkan datastream dari object host ke dalam variabel bernama gazePointDataStream
            var gazePointDataStream = host.Streams.CreateGazePointDataStream(Tobii.Interaction.Framework.GazePointDataMode.LightlyFiltered);
            //lightlyfiltered agar pergerakan mata sedikit dimuluskan (reducing sensitivity (?))

            //variabel ini untuk mengatur datastream yang muncul
            int pembagilama = 0; 
            
            //delegate method. Perulangan terjadi di sini. Setiap datastream yang masuk akan melalui proses di bawah:
            gazePointDataStream.Next += (o, gazing) =>
             {
                 //Datastream mengandung banyak data, salah tiganya x, y, dan timestamp.

                 //mengambil koordinat mata x dari Data.X ke variabel bernama gazePointX.
                 var gazePointX = gazing.Data.X;
                 //mengambil koordinat mata y dari Data.Y ke variabel bernama gazePointY.
                 var gazePointY = gazing.Data.Y;
                 //Timestamp diambil untuk dibagi dengan selisih detik yang diinginkan agar datastream tidak terlalu deras.
                 var timestamping = gazing.Data.Timestamp;

                 //konversi untuk membuang angka di belakang koma yang terlalu banyak.
                 int koordX = (int)gazePointX;
                 int koordY = (int)gazePointY;

                 //variabel pembagi baru mendapatkan timestamp i.
                 int pembagibaru = (int)timestamping / 1000; //how many interval

                 
                 if (pembagibaru != pembagilama)
                 {
                     //pembagilama mendapatkan timestamp i (dari pembagibaru).
                     //Ini berguna saat gazestream diulang; data dengan timestamp yang sama tidak akan dikirim.
                     pembagilama = pembagibaru;

                     //membuat string dengan data yang didapat.
                     string datatext = "{\"id\": 2, \"x_eye\":" + koordX.ToString() + ", \"y_eye\":" + koordY.ToString() + "}";
                     
                     //mengirimkan data menggunakan websocket.
                     socketManager.Send(datatext);
                 }
                 //kalau pembagibaru sama dengan pembagilama, artinya timestamp sama. Data tidak perlu dikirim.
             };

//                Console.WriteLine("Console can be closed...");
//                socketManager.Close();

            //neccesity delay
            Console.ReadKey();
        }

    }
    public class WebSocketManager
    {
        private AutoResetEvent messageReceiveEvent = new AutoResetEvent(false);
        private string lastMessageReceived;
        private WebSocket webSocket;

        public WebSocketManager(string webSocketUri)
        {
            Console.WriteLine("Initializing websocket. Uri: " + webSocketUri);
            webSocket = new WebSocket(webSocketUri);
            webSocket.Opened += new EventHandler(websocket_Opened);
            webSocket.Closed += new EventHandler(websocket_Closed);
            webSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);

            webSocket.Open();
            while (webSocket.State == WebSocketState.Connecting) { };   // by default webSocket4Net has AutoSendPing=true, 
                                                                        // so we need to wait until connection established
            if (webSocket.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not opened.");
            }
        }
        
        public string Send(string data)
        {
            Console.WriteLine("Client wants to send data.");
            JObject json = JObject.Parse(data);
            webSocket.Send(json.ToString());
            if (!messageReceiveEvent.WaitOne(500))                         // waiting for the response with 5 secs timeout
                Console.WriteLine("Cannot receive the response. Timeout.");

            return lastMessageReceived;
        }

        public void Close()
        {
            Console.WriteLine("Closing websocket...");
            webSocket.Close();
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            Console.WriteLine("Websocket is opened.");
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("Websocket is closed.");

            //websocket dibuka paksa
            Console.WriteLine("Reopening websocket...");
            webSocket.Open();
        }

        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Mengirim data: " + e.Message);
            lastMessageReceived = e.Message;
            messageReceiveEvent.Set();
        }
    }
}