using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace switter_client
{
    public partial class Form1 : Form
    {

        bool terminating = false;
        bool connected = false;
        Socket clientSocket;
        string username;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }
        // After input username, port and ip, connecting to server
        private void button_connect_Click(object sender, EventArgs e)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox_ip.Text;
            username = textBox_username.Text;

            int portNum;

            if (username == "")
            {
                logs.AppendText("Username cannot be empty !!\r\n");
            }
            
            else if (Int32.TryParse(textBox_port.Text, out portNum) && IP != "")
            {

                try
                {
                    // trying to connect to server
                    clientSocket.Connect(IP, portNum);
                    button_connect.Enabled = false;
                    button_send.Enabled = true;
                    connected = true;
                    logs.AppendText("Trying to connect the server!\n");

                    Byte[] buffer = Encoding.Default.GetBytes(username);
                    clientSocket.Send(buffer);

                    Console.WriteLine("sent username");

                    // connected and started to listen server
                    Thread receiveThread = new Thread(Receive);
                    receiveThread.Start();

                }
                catch
                {
                    logs.AppendText("Could not connect to the server.\r\n");
                }
            }
            else
            {
                logs.AppendText("Check the port and IP number.\n");
            }

        }
        // Receives messages from the server
        private void Receive()
        {
            while (connected)
            {
                try
                {
                    // buffer saves incoming messages as bytes
                    Byte[] buffer = new Byte[4096];
                    clientSocket.Receive(buffer);
                    

                    // bytes to string incoming message
                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

                    // if login is failed
                    if (incomingMessage.Equals("This user does not exist.") || incomingMessage.Equals("This user is already connected."))
                    {   // If the user is not in the server's userlist or already connected to the server

                        logs.AppendText("Server: " + incomingMessage + "\n");
                        connected = false;
                        logs.AppendText("The server has disconnected\n");
                        button_connect.Enabled = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                        textBox_follow.Enabled = false;
                        button_follow.Enabled = false;
                        button_getUsers.Enabled = false;
                        button_getFollows.Enabled = false;
                        button_block.Enabled = false;
                        textBox_block.Enabled = false;
                        button_getFollowers.Enabled = false;
                        button_getMySweets.Enabled = false;
                        button_deleteSweet.Enabled = false;
                    }
                    // if login successful, fix the buttons and continue listening
                    else if (incomingMessage.Equals("Connected successfully.")){
                        logs.AppendText("Server: " + incomingMessage + "\n");
                        button_disconnect.Enabled = true;
                        textBox_message.Enabled = true;
                        textBox_follow.Enabled = true;
                        button_follow.Enabled = true;
                        button_getUsers.Enabled = true;
                        button_getFollows.Enabled = true;
                        button_block.Enabled = true;
                        textBox_block.Enabled = true;
                        button_getFollowers.Enabled = true;
                        button_getMySweets.Enabled = true;
                        button_deleteSweet.Enabled = true;
                        button_requestFollowing.Enabled = true;
                        button_getAllSweets.Enabled = true;
                        button_connect.Enabled = false;

                    }
                    else if (incomingMessage.StartsWith("own:"))
                    {
                        string ownSweets = incomingMessage.Substring(4);
                        listBox1.Items.Clear();
                        char[] sep = { '\n' };
                        listBox1.Items.AddRange(ownSweets.Split(sep, StringSplitOptions.RemoveEmptyEntries));
                        if (ownSweets.Length == 0)
                            logs.AppendText("You have no sweets\n");
                        else
                            logs.AppendText("Server: Your sweets are listed.\n");
                    }
                    else if(incomingMessage != "")  // If incoming message is not empty
                    {
                        logs.AppendText("Server: " + incomingMessage + "\n");
                    }
                }
                catch
                {
                    // any problem with server
                    if (!terminating)
                    {
                        logs.AppendText("The server has disconnected.\n");
                        button_connect.Enabled = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                        button_requestFollowing.Enabled = false;
                        button_disconnect.Enabled = false;
                    }

                    clientSocket.Close();
                    connected = false;
                }

            }
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }

      
        // posting sweet to server
        private void button_send_Click(object sender, EventArgs e)
        {
            string message = "msg:" + textBox_message.Text;

            // checks message
            if (message != "")
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);
                clientSocket.Send(buffer);
                logs.AppendText("Me: " + textBox_message.Text + "\n");
                textBox_message.Text = "";
            }
        }

        // disconnect from the server as logged in user
        private void button_disconnect_Click(object sender, EventArgs e)
        {
            try
            {
                // sending the disconnect request
                Byte[] buffer = Encoding.Default.GetBytes("disconnect");
                clientSocket.Send(buffer);
                
            }
            catch
            {
                logs.AppendText("Error trying to disconnect from the server\n");
            }
            finally
            {
                // disconnects and fixes the button's enableness
                logs.AppendText("Disconnected from the server\n");
                connected = false;
                button_connect.Enabled = true;
                textBox_message.Enabled = false;
                textBox_follow.Enabled = false;
                button_send.Enabled = false;
                button_follow.Enabled = false;
                button_getUsers.Enabled = false;
                button_getFollows.Enabled = false;
                button_getFollowers.Enabled = false;
                button_block.Enabled = false;
                textBox_block.Enabled = false;
                button_disconnect.Enabled = false;
                button_requestFollowing.Enabled = false;
                button_getAllSweets.Enabled = false;
                clientSocket.Close();
            }
           
        }
        // requesting all the sweets from the server
        private void button_requestFollowing_Click(object sender, EventArgs e)
        {
            logs.AppendText("Requested followings' sweets from the server\n");
            Byte[] buffer = Encoding.Default.GetBytes("requestFollowingSweets");
            clientSocket.Send(buffer);

        }

        private void button_follow_Click(object sender, EventArgs e)
        {
            Byte[] buffer = Encoding.Default.GetBytes("follow:" + textBox_follow.Text);
            clientSocket.Send(buffer);
            textBox_follow.Text = "";
        }

        private void button_getAllSweets_Click(object sender, EventArgs e)
        {
            logs.AppendText("Requested all sweets from the server\n");
            Byte[] buffer = Encoding.Default.GetBytes("requestAllSweets");
            clientSocket.Send(buffer);
        }

        private void button_getUsers_Click(object sender, EventArgs e)
        {
            logs.AppendText("Requested all users from the server\n");
            Byte[] buffer = Encoding.Default.GetBytes("getAllUsers");
            clientSocket.Send(buffer);
        }

        private void button_getFollows_Click(object sender, EventArgs e)
        {
            logs.AppendText("Requested users followed from the server\n");
            Byte[] buffer = Encoding.Default.GetBytes("getFollows");
            clientSocket.Send(buffer);
        }

        private void button_getFollowers_Click(object sender, EventArgs e)
        {
            logs.AppendText("Requested followers from the server\n");
            Byte[] buffer = Encoding.Default.GetBytes("getFollowers");
            clientSocket.Send(buffer);
        }

        private void button_block_Click(object sender, EventArgs e)
        {
            logs.AppendText("Blocking " + textBox_block.Text + "\n");
            Byte[] buffer = Encoding.Default.GetBytes("block:" + textBox_block.Text);
            clientSocket.Send(buffer);
        }

        private void button_getMySweets_Click(object sender, EventArgs e)
        {
            logs.AppendText("Requested own sweets from the server\n");
            Byte[] buffer = Encoding.Default.GetBytes("requestMySweets");
            clientSocket.Send(buffer);
        }

        private void button_deleteSweet_Click(object sender, EventArgs e)
        {
            if(listBox1.SelectedIndex!=-1) { 
                int sweetID = Int32.Parse(listBox1.SelectedItem.ToString().Split(']')[0].Substring(1));
                logs.AppendText("Deleting sweet with id " + sweetID + "\n");
                Byte[] buffer = Encoding.Default.GetBytes("delete:"+sweetID);
                clientSocket.Send(buffer);
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            }
            
            else
            {
                logs.AppendText("You must select a sweet to delete.\n");
            }
        }
    }
}
