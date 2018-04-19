using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.Net;
using WenSocket;
using System.Threading;
using WenSocketTcpServer.Common;

namespace WenSocketTcpServer
{
    public partial class FrmTCPServer : Form
    {
        //private static string serverIP;
        IPAddress serverIP = IPAddress.Any;
        //private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static int port;
        object obj = new object();
        private int sendInt = 0;
        private static Dictionary<TreeNode, IPEndPoint> DicTreeIPEndPoint = new Dictionary<TreeNode, IPEndPoint>();

        public FrmTCPServer()
        {
            InitializeComponent();

            //serverIP = ConfigurationManager.AppSettings["ServerIP"];
            IPAddress serverIP = IPAddress.Any;
            port = int.Parse(ConfigurationManager.AppSettings["ServerPort"]);
            Control.CheckForIllegalCrossThreadCalls = false;
            init();
        }

        private void init()
        {
            LogHelper.log.Info("启动软件。。。");
            treeViewClientList.Nodes.Clear();
            TreeNode tn = new TreeNode();
            tn.Name = "ClientList";
            tn.Text = "客户端列表";
            tn.ImageIndex = 0;
            tn.ContextMenuStrip = contextMenuStripClientAll;
            treeViewClientList.Nodes.Add(tn);
            DicTreeIPEndPoint.Clear();

            //自已绘制  
            this.treeViewClientList.DrawMode = TreeViewDrawMode.OwnerDrawText;
            this.treeViewClientList.DrawNode += new DrawTreeNodeEventHandler(treeViewClientList_DrawNode);
        }

        private WenSocket.WenTcpServer tcpServer;


        /// <summary>
        /// 绘制颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeViewClientList_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            e.DrawDefault = true; //我这里用默认颜色即可，只需要在TreeView失去焦点时选中节点仍然突显  
            //return;
            //or  自定义颜色  
            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                //演示为绿底白字  
                e.Graphics.FillRectangle(Brushes.DarkBlue, e.Node.Bounds);

                Font nodeFont = e.Node.NodeFont;
                if (nodeFont == null) nodeFont = ((TreeView)sender).Font;
                e.Graphics.DrawString(e.Node.Text, nodeFont, Brushes.White, Rectangle.Inflate(e.Bounds, 2, 0));
            }
            else
            {
                e.DrawDefault = true;
            }

            if ((e.State & TreeNodeStates.Focused) != 0)
            {
                using (Pen focusPen = new Pen(Color.Black))
                {
                    focusPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    Rectangle focusBounds = e.Node.Bounds;
                    focusBounds.Size = new Size(focusBounds.Width - 1,
                    focusBounds.Height - 1);
                    e.Graphics.DrawRectangle(focusPen, focusBounds);
                }
            }

        }

        /// <summary>
        /// 开启服务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (serverIP != null && port > 0)
                {
                    tcpServer.InitSocket(serverIP, port);
                    tcpServer.Start();
                    LogHelper.log.Info(string.Format("{0}服务端程序监听启动成功！监听：{1}:{2}", DateTime.Now.ToString(), serverIP, port.ToString()));
                    listBoxServerInfo.Items.Insert(0, string.Format("{0}服务端程序监听启动成功！监听：{1}:{2}", DateTime.Now.ToString(), serverIP, port.ToString()));
                    StartServerToolStripMenuItem.Enabled = false;
                }


            }
            catch (Exception ex)
            {
                LogHelper.log.Info(string.Format("服务器启动失败！原因：{0}", ex.Message));
                listBoxServerInfo.Items.Insert(0, string.Format("服务器启动失败！原因：{0}", ex.Message));
                StartServerToolStripMenuItem.Enabled = true;
            }
        }

        /// <summary>
        /// 停止服务监听
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //tcpServer.Stop();
            //listBoxServerInfo.Items.Insert("服务器程序停止成功！");
            //StartServerToolStripMenuItem.Enabled = true;

        }

        private void FrmTCPServer_Load(object sender, EventArgs e)
        {
            if (tcpServer == null)
            {
                LogHelper.log.Info(string.Format("服务端监听程序尚未开启！{0}:{1}", serverIP, port));
                listBoxServerInfo.Items.Insert(0, string.Format("服务端监听程序尚未开启！{0}:{1}", serverIP, port));
            }

            treeViewClientList.ExpandAll();
            WenTcpServer.pushSockets = new PushSockets(Rev);
            tcpServer = new WenTcpServer();
            StopServerToolStripMenuItem.Enabled = false;

        }

        /// <summary>
        /// 处理接收到客户端的请求和数据
        /// </summary>
        /// <param name="sks"></param>
        private void Rev(WenSocket.Sockets sks)
        {
            this.Invoke(new ThreadStart(
                delegate
                {
                    if (treeViewClientList.Nodes[0] != null)
                    {

                    }

                    if (sks.ex != null)
                    {
                        if (sks.ClientDispose)
                        {
                            LogHelper.log.Info(string.Format("{0}客户端：{1}下线！", DateTime.Now.ToString(), sks.Ip));
                            listBoxServerInfo.Items.Insert(0, string.Format("{0}客户端：{1}下线！", DateTime.Now.ToString(), sks.Ip));
                            if (treeViewClientList.Nodes[0].Nodes.ContainsKey(sks.Ip.ToString()))
                            {
                                if (DicTreeIPEndPoint.Count != 0)
                                {
                                    removTreeIPEndPoint(sks.Ip);
                                    treeViewClientList.Nodes[0].Nodes.RemoveByKey(sks.Ip.ToString());

                                    toolStripStatusLabelClientNum.Text = (int.Parse(toolStripStatusLabelClientNum.Text) - 1).ToString();//treeViewClientList.Nodes[0].Nodes.Count.ToString();

                                }

                            }
                        }
                        listBoxServerInfo.Items.Insert(0, sks.ex.Message);
                    }
                    else
                    {
                        if (sks.NewClientFlag)
                        {
                            LogHelper.log.Info(string.Format("{0}新的客户端：{1}链接成功", DateTime.Now.ToString(), sks.Ip));
                            listBoxServerInfo.Items.Insert(0, string.Format("{0}新的客户端：{1}链接成功", DateTime.Now.ToString(), sks.Ip));

                            TreeNode tn = new TreeNode();
                            tn.Name = sks.Ip.ToString();
                            tn.Text = sks.Ip.ToString();
                            tn.ContextMenuStrip = contextMenuStripClientSingle;
                            tn.Tag = "客户端";
                            tn.ImageIndex = 1;

                            treeViewClientList.Nodes[0].Nodes.Add(tn);

                            //treeview节点和IPEndPoint绑定
                            DicTreeIPEndPoint.Add(tn, sks.Ip);

                            if (treeViewClientList.Nodes[0].Nodes.Count > 0)
                            {
                                treeViewClientList.ExpandAll();
                            }
                            toolStripStatusLabelClientNum.Text = (int.Parse(toolStripStatusLabelClientNum.Text) + 1).ToString();
                        }
                        else if (sks.Offset == 0)
                        {
                            LogHelper.log.Info(string.Format("{0}客户端:{1}下线.!", DateTime.Now.ToString(), sks.Ip));
                            listBoxServerInfo.Items.Insert(0, string.Format("{0}客户端:{1}下线.!", DateTime.Now.ToString(), sks.Ip));
                            if (treeViewClientList.Nodes[0].Nodes.ContainsKey(sks.Ip.ToString()))
                            {
                                if (DicTreeIPEndPoint.Count != 0)
                                {
                                    removTreeIPEndPoint(sks.Ip);
                                    treeViewClientList.Nodes[0].Nodes.RemoveByKey(sks.Ip.ToString());
                                    toolStripStatusLabelClientNum.Text = (int.Parse(toolStripStatusLabelClientNum.Text) - 1).ToString();
                                }
                            }
                        }
                        else
                        {
                            byte[] buffer = new byte[sks.Offset];
                            Array.Copy(sks.RecBuffer, buffer, sks.Offset);
                            string str = Encoding.ASCII.GetString(buffer);

                            LogHelper.log.Info(string.Format("{0}客户端 {1} 发来消息：{2}", DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"), sks.Ip, str));
                            listBox1.Items.Insert(0, string.Format("{0} 客户端 {1} 发来消息：{2}", DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"), sks.Ip, str));

                            //将接收到客户端的数据重新发送给客户端
                            //string SendStr = "TCPServers at [" + DateTime.Now.ToString("yyy-MM-dd HH:mm:ss") + "] Received your Data:" + str;
                            tcpServer.SendToClient(sks.Ip, str);
                            LogHelper.log.Info(string.Format("{0} 回复给 {1} 客户端消息：{2}", DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"), sks.Ip, str));
                            listBox1.Items.Insert(0, string.Format("{0} 回复给 {1} 客户端消息：{2}", DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"), sks.Ip, str));

                        }
                    }
                }
                )
                );
        }

        /// <summary>
        /// 关闭程序钱停止服务器实例
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmTCPServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            tcpServer.Stop();
        }

        private void treeViewClientList_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //
        }

        private void treeViewClientList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeViewClientList.Focus();
                treeViewClientList.SelectedNode = treeViewClientList.GetNodeAt(e.X, e.Y);
            }

        }


        private void toolStripMenuSendSingle_Click(object sender, EventArgs e)
        {
            if (treeViewClientList.SelectedNode != null)
            {
                tcpServer.SendToClient(DicTreeIPEndPoint[treeViewClientList.SelectedNode], string.Format("A{0}", sendInt.ToString()));
                LogHelper.log.Info(string.Format("发送给 {0} 客户端的内容：A{1}", DicTreeIPEndPoint[treeViewClientList.SelectedNode],sendInt.ToString()));
                sendInt++;
            }
        }

        private void toolStripMenuSendAll_Click(object sender, EventArgs e)
        {
            tcpServer.SendToAll("B" + sendInt);
            sendInt++;
        }

        private void removTreeIPEndPoint(IPEndPoint ipendPoint)
        {

            if (DicTreeIPEndPoint.Count <= 0) return;
            //foreach遍历Dictionary时候不能对字典进行Remove
            TreeNode[] keys = new TreeNode[DicTreeIPEndPoint.Count];
            DicTreeIPEndPoint.Keys.CopyTo(keys, 0);
            lock (obj)
            {
                foreach (TreeNode item in keys)
                {
                    if (DicTreeIPEndPoint[item] == ipendPoint)
                    {
                        DicTreeIPEndPoint.Remove(item);
                    }
                }
            }
        }

    }
}
