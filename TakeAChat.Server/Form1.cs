using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TakeAChat.Server
{
    public partial class Form1 : Form
    {
        private delegate void AtualizaStatusCallback(string message);
        private const string IP_ADRESS = "127.0.0.1";

        public Form1()
        {
            InitializeComponent();
            LoadServer();
        }

        private void LoadServer()
        {
            try
            {

                IPAddress enderecoIP = IPAddress.Parse(IP_ADRESS);

                ChatServer mainServidor = new ChatServer(enderecoIP);

                ChatServer.StatusChanged += new StatusChangedEventHandler(mainServer_StatusChanged);

                mainServidor.Start();

                txtLog.AppendText("Monitorando as conexões...\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro de conexão : " + ex.Message);
            }
        }

        /// <summary>
        /// Calls the method how updates log in the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void mainServer_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            // Chama o método que atualiza o formulário
            this.Invoke(new AtualizaStatusCallback(this.AtualizaStatus), new object[] { e.EventMessage });
        }


        /// <summary>
        /// Update log in the form.
        /// </summary>
        /// <param name="message"></param>
        private void AtualizaStatus(string message)
        {
            txtLog.AppendText(message + "\r\n");
        }

    }
}
