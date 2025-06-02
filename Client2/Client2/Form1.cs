using System.IO;
using System.Net.Sockets;
using System;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Client2
{
    public partial class Form1 : Form
    {
        private bool isOn1 = false;
        private bool isOn2 = false;
        private bool isOn3 = false;
        private bool isOn4 = false;
        private bool isOn5 = false;
        private bool isOn6 = false;
        private bool isOn7 = false;
        private bool isOn8 = false;
        private bool isOn9 = false;
        private bool isOn10 = false;

        private TcpClient client;
        private NetworkStream stream;
        private Thread receiveThread;

        private bool[] buttonStates = new bool[10];

        private bool isBusy = false;
        private readonly object lockObject = new object(); // Lock for busy state management    
        private readonly object streamLock = new object(); // Separate lock for stream operations
        public Form1()
        {
            InitializeComponent();
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            button1.BackColor = Color.Red;
            button1.Text = "OFF";

            button2.BackColor = Color.Red;
            button2.Text = "OFF";

            button3.BackColor = Color.Red;
            button3.Text = "OFF";

            button4.BackColor = Color.Red;
            button4.Text = "OFF";

            button5.BackColor = Color.Red;
            button5.Text = "OFF";

            button6.BackColor = Color.Red;
            button6.Text = "OFF";

            button7.BackColor = Color.Red;
            button7.Text = "OFF";

            button8.BackColor = Color.Red;
            button8.Text = "OFF";

            button9.BackColor = Color.Red;
            button9.Text = "OFF";

            button10.BackColor = Color.Red;
            button10.Text = "OFF";

            // Connect to server
            try
            {
                client = new TcpClient("127.0.0.1", 5000);  // Change to server IP if remote
                stream = client.GetStream();

                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not connect to server: " + ex.Message);
            }
        }

        //private void ReceiveData()
        //{
        //    byte[] buffer = new byte[1024];
        //    while (true)
        //    {
        //        try
        //        {
        //            int bytesRead = stream.Read(buffer, 0, buffer.Length);
        //            if (bytesRead == 0) continue;

        //            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        //            if (message.Trim() == "ALL" || message.Trim() == "CLIENT1")
        //            {
        //                string status = GetButtonStatuses();
        //                byte[] dataToSend = Encoding.ASCII.GetBytes("CLIENT1"+status);
        //                stream.Write(dataToSend, 0, dataToSend.Length);
        //            }
        //        }
        //        catch
        //        {
        //            break;
        //        }
        //    }
        //}

        private void ReceiveData()
        {
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    // Check if connection was closed by server
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                    // Handle each message in a separate thread
                    Thread messageThread = new Thread(() => HandleMessage(message))
                    {
                        IsBackground = true,
                        Name = $"MessageHandler-{DateTime.Now.Ticks}"
                    };
                    messageThread.Start();
                }
                catch (System.IO.IOException ioEx)
                {
                    break;
                }
                catch (System.Net.Sockets.SocketException sockEx)
                {
                    break;
                }
                catch (Exception ex)
                {
                    break;
                }
            }

            // Clean up connection
            try
            {
                stream?.Close();
                client?.Close();
            }
            catch
            {
                
            }
        }

        private async void HandleMessage(string message)
        {
            try
            {
                // Handle status request commands
                if (message.Trim() == "ALL")
                {
                    bool shouldProcess = false;

                    lock (lockObject)
                    {
                        // Check if already processing a request
                        if (isBusy)
                        {
                            // Send busy message immediately
                            byte[] busyMessage = Encoding.ASCII.GetBytes("CLIENT2BUSY");
                            lock (streamLock) // Use dedicated stream lock
                            {
                                stream.Write(busyMessage, 0, busyMessage.Length);
                                stream.Flush();
                            }
                            return;
                        }

                        // Set busy flag
                        isBusy = true;
                        shouldProcess = true;
                    }

                    if (shouldProcess)
                    {
                        try
                        {
                            // Step 1: Send Acknowledgement immediately
                            byte[] ackMessage = Encoding.ASCII.GetBytes("CLIENT2ACK");
                            lock (streamLock) // Use dedicated stream lock
                            {
                                stream.Write(ackMessage, 0, ackMessage.Length);
                                stream.Flush();
                            }

                            // Step 2: Wait 3 seconds
                            await Task.Delay(3000);

                            // Step 3: Send Reply with button status
                            string status = GetButtonStatuses();
                            byte[] dataToSend = Encoding.ASCII.GetBytes("CLIENT2" + status);
                            lock (streamLock) // Use dedicated stream lock
                            {
                                stream.Write(dataToSend, 0, dataToSend.Length);
                                stream.Flush();
                            }
                        }
                        finally
                        {
                            lock (lockObject)
                            {
                                isBusy = false;
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(message))
                {
                    // Handle other messages - send acknowledgement
                    byte[] ackMessage = Encoding.ASCII.GetBytes("CLIENT2ACK");
                    lock (streamLock) // Use dedicated stream lock
                    {
                        stream.Write(ackMessage, 0, ackMessage.Length);
                        stream.Flush();
                    }
                }
            }
            catch
            {
                
            }
        }
        private string GetButtonStatuses()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                sb.Append($"Button{i + 1}={(buttonStates[i] ? "ON" : "OFF")},");
            }
            return sb.ToString().TrimEnd(',');
        }


        private void button1_Click(object sender, EventArgs e)
        {
            // Toggle the boolean state isOn1: if true, becomes false; if false, becomes true
            isOn1 = !isOn1;
            button1.BackColor = isOn1 ? Color.Green : Color.Red;
            button1.Text = isOn1 ? "ON" : "OFF";
            buttonStates[0] = isOn1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            isOn2 = !isOn2;
            button2.BackColor = isOn2 ? Color.Green : Color.Red;
            button2.Text = isOn2 ? "ON" : "OFF";
            buttonStates[1] = isOn2;
        }


        private void button3_Click(object sender, EventArgs e)
        {
            isOn3 = !isOn3;
            button3.BackColor = isOn3 ? Color.Green : Color.Red;
            button3.Text = isOn3 ? "ON" : "OFF";
            buttonStates[2] = isOn3;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            isOn4 = !isOn4;
            button4.BackColor = isOn4 ? Color.Green : Color.Red;
            button4.Text = isOn4 ? "ON" : "OFF";
            buttonStates[3] = isOn4;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            isOn5 = !isOn5;
            button5.BackColor = isOn5 ? Color.Green : Color.Red;
            button5.Text = isOn5 ? "ON" : "OFF";
            buttonStates[4] = isOn5;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            isOn6 = !isOn6;
            button6.BackColor = isOn6 ? Color.Green : Color.Red;
            button6.Text = isOn6 ? "ON" : "OFF";
            buttonStates[5] = isOn6;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            isOn7 = !isOn7;
            button7.BackColor = isOn7 ? Color.Green : Color.Red;
            button7.Text = isOn7 ? "ON" : "OFF";
            buttonStates[6] = isOn7;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            isOn8 = !isOn8;
            button8.BackColor = isOn8 ? Color.Green : Color.Red;
            button8.Text = isOn8 ? "ON" : "OFF";
            buttonStates[7] = isOn8;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            isOn9 = !isOn9;
            button9.BackColor = isOn9 ? Color.Green : Color.Red;
            button9.Text = isOn9 ? "ON" : "OFF";
            buttonStates[8] = isOn9;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            isOn10 = !isOn10;
            button10.BackColor = isOn10 ? Color.Green : Color.Red;
            button10.Text = isOn10 ? "ON" : "OFF";
            buttonStates[9] = isOn10;
        }
    }
}
