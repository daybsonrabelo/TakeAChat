using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TakeAChat.Server
{
    class Connection
    {
        TcpClient tcpClient;
        private Thread thrSender;
        private StreamReader srReceptor;
        private StreamWriter swEnviador;
        private string currentAtual;
        private string strResponse;

        private const string USERS_PREFIX = "_@_|";
        private const string USER_DEST_PREFIX = "!#_%_";

        public Connection(TcpClient tcpCon)
        {
            tcpClient = tcpCon;
            thrSender = new Thread(AcceptClient);
            thrSender.Start();
        }

        /// <summary>
        /// Close all oppned objects .
        /// </summary>
        private void CloseConnection()
        {
            tcpClient.Close();
            srReceptor.Close();
            swEnviador.Close();
        }

        private void AcceptClient()
        {
            srReceptor = new System.IO.StreamReader(tcpClient.GetStream());
            swEnviador = new System.IO.StreamWriter(tcpClient.GetStream());

            // Lê a informação da conta do cliente
            currentAtual = srReceptor.ReadLine();

            // temos uma resposta do cliente
            if (currentAtual != "")
            {
                // Armazena o nome do usuário na hash table
                if (ChatServer.htUsers.Contains(currentAtual) == true)
                {
                    // 0 => significa não conectado
                    swEnviador.WriteLine("0|Este nome de usuário já existe.");
                    swEnviador.Flush();
                    CloseConnection();
                    return;
                }
                else if (currentAtual == "Administrator")
                {
                    // 0 => não conectado
                    swEnviador.WriteLine("0|Este nome de usuário é reservado.");
                    swEnviador.Flush();
                    CloseConnection();
                    return;
                }
                else
                {
                    // 1 => conectou com sucesso
                    swEnviador.WriteLine("1");
                    swEnviador.Flush();

                    // Inclui o usuário na hash table e inicia a escuta de suas mensagens
                    ChatServer.AddUser(tcpClient, currentAtual);
                }
            }
            else
            {
                CloseConnection();
                return;
            }
            //
            try
            {
                // Continua aguardando por uma mensagem do usuário
                while ((strResponse = srReceptor.ReadLine()) != "")
                {
                    // Se for inválido remove-o
                    if (strResponse == null)
                    {
                        ChatServer.RemoveUser(tcpClient);
                    }
                    else
                    {
                        if(strResponse == USERS_PREFIX || strResponse.IndexOf(USER_DEST_PREFIX) > -1)
                        {
                            string userDest = currentAtual;
                            if (strResponse.IndexOf(USER_DEST_PREFIX) > -1)
                            {
                                strResponse = strResponse.Replace(USER_DEST_PREFIX, "");
                                userDest = strResponse.Split(';')[0];//Captura usuário destino
                                strResponse = strResponse.Split(';')[1];//Captura apenas a mensagem a ser enviada
                            }
                            ChatServer.EnviaMensagem(currentAtual, strResponse, userDest);
                        }
                        else
                        {
                            // envia a mensagem para todos os outros usuários
                            ChatServer.EnviaMensagem(currentAtual, strResponse);
                        }
                    }
                }
            }
            catch
            {
                // Se houve um problema com este usuário desconecta-o
                ChatServer.RemoveUser(tcpClient);
            }
        }
    }
}
