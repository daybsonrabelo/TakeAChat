using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TakeAChat.Client
{
    public partial class Form1 : Form
    {
        // Trata o nome do usuário
        private string NomeUsuario = "Desconhecido";
        private StreamWriter stwSender;
        private StreamReader strReceptor;
        private TcpClient tcpServer;
        // Necessário para atualizar o formulário com mensagens da outra thread
        private delegate void AtualizaLogCallBack(string strMensagem);
        // Necessário para definir o formulário para o estado "disconnected" de outra thread
        private delegate void FechaConexaoCallBack(string strMotivo);
        private Thread mensagemThread;
        private IPAddress ipAddress;
        private bool conected;

        private const string IP_ADDRESS = "127.0.0.1";
        private const int PORT = 2502;
        private const string USERS_PREFIX = "_@_|";
        private const string USER_DEST_PREFIX = "!#_%_";


        public Form1()
        {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            InitializeComponent();
            //LoadUsers("");
        }

        private void InitializeConnection()
        {
            try
            {
                ipAddress = IPAddress.Parse(IP_ADDRESS);
                tcpServer = new TcpClient();
                tcpServer.Connect(ipAddress, PORT);

                conected = true;

                NomeUsuario = txtUsername.Text;

                txtUsername.Enabled = false;
                txtMessage.Enabled = true;
                btnSendMessage.Enabled = true;
                btnConnect.Text = "Desconectado";

                stwSender = new StreamWriter(tcpServer.GetStream());
                stwSender.WriteLine(txtUsername.Text);
                stwSender.Flush();

                mensagemThread = new Thread(new ThreadStart(RecebeMensagens));
                mensagemThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro : " + ex.Message, "Erro na conexão com servidor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RecebeMensagens()
        {
            strReceptor = new StreamReader(tcpServer.GetStream());
            string ConResposta = strReceptor.ReadLine();
            // Se o primeiro caracter da resposta é 1 a conexão foi feita com sucesso
            if (ConResposta[0] == '1')
            {
                this.Invoke(new AtualizaLogCallBack(this.UpdateLog), new object[] { "Conectado com sucesso!" });
            }
            else // Se o primeiro caractere não for 1 a conexão falhou
            {
                string Motivo = "Não Conectado: ";
                Motivo += ConResposta.Substring(2, ConResposta.Length - 2);
                this.Invoke(new FechaConexaoCallBack(this.CloseConnection), new object[] { Motivo });
                return;
            }

            while (conected)
            {
                string messageReceived = strReceptor.ReadLine();
                if (messageReceived.StartsWith(USERS_PREFIX))
                {
                    LoadUsers(messageReceived);
                } else
                {
                    this.Invoke(new AtualizaLogCallBack(this.UpdateLog), new object[] { messageReceived });
                }
            }
        }

        private void UpdateLog(string strMensagem)
        {
            txtChat.AppendText(strMensagem + "\r\n");
        }

        private void SendMessage()
        {
            if (txtMessage.Lines.Length >= 1)
            {
                if (oUsers.Text == "Todos" || string.IsNullOrEmpty(oUsers.Text))
                {
                    SendMessageToServer(txtMessage.Text);
                } else
                {
                    //Selecionou usuário no combobox
                    SendMessageToServer(USER_DEST_PREFIX + oUsers.Text + ";" + txtMessage.Text);
                }
            }
            txtMessage.Text = "";
        }

        private void SendMessageToServer(string message)
        {
            if(stwSender != null)
            {
                stwSender.WriteLine(message);
                stwSender.Flush();
                txtMessage.Lines = null;
            }
        }

        private void LoadUsers(string message)
        {
            oUsers.Items.Clear();
            oUsers.Items.Add("Todos");
            if(!string.IsNullOrEmpty(message))
            {
                message = message.Replace(USERS_PREFIX, "");
                oUsers.Items.AddRange(message.Split(";"));
            }
        }

        private void CloseConnection(string Motivo)
        {
            txtChat.AppendText(Motivo + "\r\n");
            txtUsername.Enabled = true;
            txtMessage.Enabled = false;
            btnSendMessage.Enabled = false;
            btnConnect.Text = "Conectado";

            conected = false;
            stwSender.Close();
            strReceptor.Close();
            tcpServer.Close();
        }

        // O tratador de evento para a saida da aplicação
        public void OnApplicationExit(object sender, EventArgs e)
        {
            if (conected == true)
            {
                // Fecha as conexões, streams, etc...
                conected = false;
                stwSender.Close();
                strReceptor.Close();
                tcpServer.Close();
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            // se não esta conectando aguarda a conexão
            if (conected == false)
            {
                // Inicializa a conexão
                InitializeConnection();
            }
            else // Se esta conectado entao desconecta
            {
                CloseConnection("Desconectado a pedido do usuário.");
            }
        }

        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void oUsers_Click(object sender, EventArgs e)
        {
            SendMessageToServer(USERS_PREFIX);
        }
    }
}
