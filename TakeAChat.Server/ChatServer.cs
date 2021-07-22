using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TakeAChat.Server
{
    class ChatServer
    {
        public static Hashtable htUsers = new Hashtable(30); // users limit
        public static Hashtable htConnections = new Hashtable(30);
        private IPAddress ip_address;
        private TcpClient tcpClient;
        public static event StatusChangedEventHandler StatusChanged;
        private static StatusChangedEventArgs e;

        private const string USERS_PREFIX = "_@_|";
        private const string USER_DEST_PREFIX = "!#_%_";

        public ChatServer(IPAddress address)
        {
            ip_address = address;
        }

        private Thread thrListener;
        private TcpListener tlsClient;
        bool serverIsRun = false;

        // Inclui o usuário nas tabelas hash
        public static void AddUser(TcpClient tcpUser, string strUsername)
        {
            // Primeiro inclui o nome e conexão associada para ambas as hash tables
            ChatServer.htUsers.Add(strUsername, tcpUser);
            ChatServer.htConnections.Add(tcpUser, strUsername);

            // Informa a nova conexão para todos os usuário e para o formulário do servidor
            SendAdminMessage(htConnections[tcpUser] + " entrou..");
        }

        // Remove o usuário das tabelas (hash tables)
        public static void RemoveUser(TcpClient tcpUser)
        {
            // Se o usuário existir
            if (htConnections[tcpUser] != null)
            {
                // Primeiro mostra a informação e informa os outros usuários sobre a conexão
                SendAdminMessage(htConnections[tcpUser] + " saiu...");

                // Removeo usuário da hash table
                ChatServer.htUsers.Remove(ChatServer.htConnections[tcpUser]);
                ChatServer.htConnections.Remove(tcpUser);
            }
        }

        // Este evento é chamado quando queremos disparar o evento StatusChanged
        public static void OnStatusChanged(StatusChangedEventArgs e)
        {
            StatusChangedEventHandler statusHandler = StatusChanged;
            if (statusHandler != null)
            {
                // invoca o  delegate
                statusHandler(null, e);
            }
        }

        /// <summary>
        /// Messages send by server.
        /// </summary>
        /// <param name="message"></param>
        public static void SendAdminMessage(string message)
        {
            StreamWriter swSenderSender;

            // Exibe primeiro na aplicação
            e = new StatusChangedEventArgs("Administrador: " + message);
            OnStatusChanged(e);

            // Cria um array de clientes TCPs do tamanho do numero de clientes existentes
            TcpClient[] tcpClients = new TcpClient[ChatServer.htUsers.Count];
            // Copia os objetos TcpClient no array
            ChatServer.htUsers.Values.CopyTo(tcpClients, 0);
            // Percorre a lista de clientes TCP
            for (int i = 0; i < tcpClients.Length; i++)
            {
                // Tenta enviar uma mensagem para cada cliente
                try
                {
                    // Se a mensagem estiver em branco ou a conexão for nula sai...
                    if (message.Trim() == "" || tcpClients[i] == null)
                    {
                        continue;
                    }
                    // Envia a mensagem para o usuário atual no laço
                    swSenderSender = new StreamWriter(tcpClients[i].GetStream());
                    swSenderSender.WriteLine("Administrador: " + message);
                    swSenderSender.Flush();
                    swSenderSender = null;
                }
                catch // Se houver um problema , o usuário não existe , então remove-o
                {
                    RemoveUser(tcpClients[i]);
                }
            }
        }

        // Envia mensagens de um usuário para todos os outros
        public static void EnviaMensagem(string origin, string message, string userFilter = "")
        {
            StreamWriter swSenderSender;
            //string fullMessage = origin + " disse : " + message;
            string fullMessage = origin + " disse ";
            if (!string.IsNullOrEmpty(userFilter))
            {
                fullMessage += "para " + userFilter;
            }
            fullMessage += " :" + message;

            // Primeiro exibe a mensagem na aplicação
            e = new StatusChangedEventArgs(fullMessage);
            OnStatusChanged(e);

            TcpClient[] tcpClients;

            if(!string.IsNullOrEmpty(userFilter))
            {
                if (message == USERS_PREFIX)
                {
                    tcpClients = new TcpClient[1];
                    tcpClients[0] = (TcpClient)ChatServer.htUsers[userFilter];

                    fullMessage = USERS_PREFIX;
                    foreach (string k in htUsers.Keys)
                    {
                        if (k != userFilter)
                        {
                            fullMessage += k + ";";
                        }
                    }
                } else
                {
                    tcpClients = new TcpClient[2];
                    tcpClients[0] = (TcpClient)ChatServer.htUsers[userFilter];
                    tcpClients[1] = (TcpClient)ChatServer.htUsers[origin];
                }
            }else
            {
                tcpClients = new TcpClient[ChatServer.htUsers.Count];
                // Copia os objetos TcpClient no array
                ChatServer.htUsers.Values.CopyTo(tcpClients, 0);
            }

            // Percorre a lista de clientes TCP
            for (int i = 0; i < tcpClients.Length; i++)
            {
                try
                {
                    // Se a mensagem estiver em branco ou a conexão for nula sai...
                    if (message.Trim() == "" || tcpClients[i] == null)
                    {
                        continue;
                    }
                    swSenderSender = new StreamWriter(tcpClients[i].GetStream());
                    swSenderSender.WriteLine(fullMessage);
                    swSenderSender.Flush();
                    swSenderSender = null;
                }
                catch // Se houver um problema , o usuário não existe , então remove-o
                {
                    RemoveUser(tcpClients[i]);
                }
            }
        }

        public void Start()
        {
            try
            {

                // Pega o IP do primeiro dispostivo da rede
                IPAddress ipaLocal = ip_address;

                // Cria um objeto TCP listener usando o IP do servidor e porta definidas
                tlsClient = new TcpListener(ipaLocal, 2502);

                // Inicia o TCP listener e escuta as conexões
                tlsClient.Start();

                // O laço While verifica se o servidor esta rodando antes de checar as conexões
                serverIsRun = true;

                // Inicia uma nova tread que hospeda o listener
                thrListener = new Thread(Keep);
                thrListener.Start();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void Keep()
        {
            // Enquanto o servidor estiver rodando
            while (serverIsRun == true)
            {
                // Aceita uma conexão pendente
                tcpClient = tlsClient.AcceptTcpClient();
                // Cria uma nova instância da conexão
                Connection newConnection = new Connection(tcpClient);
            }
        }
    }
}
