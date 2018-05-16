using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using NHapi.Base.Parser;
using NHapi.Base.Model;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Datatype;
using System.Data.SqlClient;
using System.Configuration;
using NHapi.Model.V231.Segment;
using NHapi.Model.V231.Group;
using NHapi.Base.Util;
using NHapi.Base;
using System.Text.RegularExpressions;


namespace WindowsFormsApplication1
{
    public partial class frmPrincipal : Form
    {
        

    private TcpListener tcpListener;
    private Thread listenThread;
    private bool canStopListening = false;
    private int contador =1;
    private string numeroProtocolo = "0";
    //private int suma100 = 0;
    private string s_idprotocolo = "";
    private string s_idLinfocitos = "";
    private string s_tipoNumeracion = "";
        
      

    delegate void SetTextCallback(string text);

    private void SetTextRecibido(string text)
    {
        // InvokeRequired required compares the thread ID of the
        // calling thread to the thread ID of the creating thread.
        // If these threads are different, it returns true.
        if (txtLogRecibido.InvokeRequired)
        {
            SetTextCallback d = new SetTextCallback(SetTextRecibido);
            this.Invoke(d, new object[] { text });
        }
        else
        {
            this.txtLogRecibido.Text = text;
            //if (this.txtLogRecibido.Text =="")
            //    this.txtLogRecibido.Text +=  text;
            //else
            //    this.txtLogRecibido.Text += Environment.NewLine+Environment.NewLine + text;
        }
    }

  


    private void SetTextEnviado(string text)
    {
        // InvokeRequired required compares the thread ID of the
        // calling thread to the thread ID of the creating thread.
        // If these threads are different, it returns true.
        if (txtLogEnviado.InvokeRequired)
        {
            SetTextCallback d = new SetTextCallback(SetTextEnviado);
            
            this.Invoke(d, new object[] { text });
        }
        else
        {
            this.txtLogEnviado.Text = text;
            //if (this.txtLogEnviado.Text == "")
            //    this.txtLogEnviado.Text += text;
            //else
            //    this.txtLogEnviado.Text += Environment.NewLine + Environment.NewLine + text;
        }
    }
   


        public frmPrincipal()
        {
            InitializeComponent();     
        }
      

        private void frmPrincipal_Load(object sender, EventArgs e)
        {
        
            
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string s_puerto = config.AppSettings.Settings["PUERTO"].Value;
            string equipoAutoAnalizador = config.AppSettings.Settings["AUTOANALIZADOR"].Value;
            s_tipoNumeracion = config.AppSettings.Settings["NUMEROPROTOCOLO"].Value;
            
            
            txtPuerto.Text = s_puerto;
            switch (equipoAutoAnalizador)

            {
                case "MINDRAY":
                    {
                        lblEquipo.Text = "Equipo Conectado: MINDRAY BS-300";
                        lblProtocoloComunicacion.Text = "Protocolo Comunicación: HL7 V2.3.1";
                    } break;
                case "SYSMEXXS1000":
                    {
                        lblEquipo.Text = "Equipo Conectado: SYSMEX XS 1000i ";
                        lblProtocoloComunicacion.Text = "Protocolo Comunicación: ASTM E1381-95 - Puerto:" + s_puerto;
                    } break;
                case "SYSMEXXT1800":
                    {
                        lblEquipo.Text = "Equipo Conectado: SYSMEX XT 1800i ";
                        lblProtocoloComunicacion.Text = "Protocolo Comunicación: ASTM E1381-95 - Puerto:" + s_puerto;
                    } break;

                case "COBASB221":
                    {
                        lblEquipo.Text = "Equipo Conectado: COBAS B 221 ";
                        lblProtocoloComunicacion.Text = "Protocolo Comunicación: ASTM E1394-97 - Puerto:" + s_puerto;
                    } break;
            }
         
            Start();                         
        }

        private void Start()
        {
            try
            {                              
                tcpListener = new TcpListener(IPAddress.Any, int.Parse(txtPuerto.Text));
                listenThread = new Thread(new ThreadStart(IniciarEscucha));
                this.listenThread.Start();
                btnIniciar.Enabled = false;
                btnDetener.Enabled = true;
                SetTextRecibido(DateTime.Now.ToShortTimeString() + " - Se ha iniciado el servidor de comunicación. Esperando comunicación con el cliente.");


                
            }
            catch (SocketException e)
            {
                SetTextRecibido(DateTime.Now.ToShortTimeString() + " - Error: " + e.Message);                
            }
        }

        private void IniciarEscucha()
        {
            try
            {
                this.tcpListener.Start();
                while (!canStopListening)
                {
                    //blocks until a client has connected to the server
                    TcpClient client = this.tcpListener.AcceptTcpClient();

                    //create a thread to handle communication 
                    //with connected client
                    Thread clientThread = new Thread(new ParameterizedThreadStart(Escuchar));
                    clientThread.Start(client);
               

                }
            }
            catch (SocketException e)
            {
                SetTextRecibido(DateTime.Now.ToShortTimeString() + " - Error: " + e.Message);
            }
        }

        private void Escuchar(object client)
        {
            ////////////////Escucha y Recibe el mensaje///////////////////

            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            SetTextRecibido(DateTime.Now.ToShortTimeString() + " - Se ha establecido comunicación con el cliente.");
            
            byte[] message = new byte[4096];
            int bytesRead;

            while (!canStopListening)
            {
                bytesRead = 0;

                try
                {  //blocks until a client sends a message
                   bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }
                if (bytesRead > 0)
                {
                    //txtLog.Text +=DateTime.Now.ToShortTimeString()+ " - Recibiendo datos del cliente";
                    //message has successfully been received
                    ASCIIEncoding encoder = new ASCIIEncoding();                 
                    
                    string mensagito = encoder.GetString(message, 0, bytesRead);

                    SetTextRecibido(DateTime.Now.ToShortTimeString() + " - " + mensagito);
                    
                    ////Graba el mensaje en Temp_Mensaje
                    GrabarMensaje(mensagito);
                    ///////////////////////////////

                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    string equipoAutoAnalizador= config.AppSettings.Settings["AUTOANALIZADOR"].Value;

                    switch (equipoAutoAnalizador)
                    {
                        case "MINDRAY": AnalizarHL7(mensagito, clientStream); break;
                        case "SYSMEXXS1000": AnalizarASTM1381(mensagito, clientStream, equipoAutoAnalizador); break;
                        case "SYSMEXXT1800": AnalizarASTM1381(mensagito, clientStream, equipoAutoAnalizador); break;
                        case "COBASB221": AnalizarASTM1394(mensagito, clientStream, equipoAutoAnalizador); break;
                    }
                        
                    

                }              
            }
            tcpClient.Close();
        }

        private void AnalizarASTM1394(string mensagito, NetworkStream clientStream, string equipoAutoAnalizador)
        {
            ///////////////COBAS B 221: CONEXION UNIDIRECCIONAL: SOLO RECEPCION DE RESULTADOS./////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["CN"].ToString(); // @"data source=.;database=SIntegralHChosMalal; USER ID=sa; Password=sa; Integrated Security=false;";                         
            SetTextRecibido(DateTime.Now.ToShortTimeString() + " - Mensaje Recibido: " + mensagito);

            try
            {
                string m_ssql = "";
                SqlConnection conn = new SqlConnection(connString); SqlDataAdapter da = new SqlDataAdapter();

                string[] srecord = mensagito.Split(("\r").ToCharArray());
                bool guardarResultados = false;
                foreach (string r in srecord)
                {
                    if (r.Trim() != "")
                    {
                        ///antes hacer slit para separar por caracterer de separador de lineas
                        string[] arr = r.Split(("|").ToCharArray());
                        string s_CampoIdentItem = "0";

                        if (arr.Length > 0)
                        {
                            string s_tipoSegmento = arr[0].ToString();
                            switch (s_tipoSegmento.Trim())
                            {
                                case "H":
                                    /////Inicio del segmento
                                    numeroProtocolo = "0"; s_idprotocolo = ""; s_idLinfocitos = "";
                                    break;
                                //case "Q":
                                //    ///Este segmento es para pedir al LIS que descargue un protocolo
                                //    numeroProtocolo = "0"; s_idprotocolo = "";
                                //    guardarResultados = false; s_idLinfocitos = "";

                                //    /// sacar  rack y posición para armar el mensaje
                                //    string s_ubicacion = arr[2].ToString();
                                //    string[] arrUbicacion = s_ubicacion.Split(("^").ToCharArray());
                                //    if (arrUbicacion.Length > 0)
                                //    {
                                //        string posicion = arrUbicacion[0].ToString();
                                //        string rack = arrUbicacion[1].ToString();
                                //        if (arrUbicacion.Length > 2)//si es con codigo de barras
                                //        {
                                //            string codigo_barras = arrUbicacion[3].ToString();
                                //            string codigo_control = arrUbicacion[4].ToString();
                                //            if (codigo_control == "B")//si es con codigo de barras
                                //            {
                                //                string mensajeReturnCodigoBarras = GenerarMensajeASTMCodigoBarras(posicion, rack, codigo_barras, equipo);
                                //                EnviarMensajeCliente(mensajeReturnCodigoBarras, clientStream);
                                //                SetTextEnviado(DateTime.Now.ToShortTimeString() + " Se han enviado el mensaje " + mensajeReturnCodigoBarras);
                                //            }
                                //        } ///// longitud menor a dos no es codigo de barras
                                //        else
                                //        {
                                //            string mensajeReturn = GenerarMensajeASTM(posicion, rack, equipo);
                                //            EnviarMensajeCliente(mensajeReturn, clientStream);
                                //            SetTextEnviado(DateTime.Now.ToShortTimeString() + " Se han enviado el mensaje " + mensajeReturn);
                                //        }


                                //    }

                                //    break;

                                case "O":

                                    string s_CampoProtocolo = arr[3].ToString();
                                    string[] arrAux = s_CampoProtocolo.Split(("^").ToCharArray());
                                    if (arrAux.Length > 0)
                                    {
                                        numeroProtocolo = arrAux[2].ToString().Trim().ToUpper();
                                        numeroProtocolo = numeroProtocolo.Replace(" ", "");

                                        ///////////////////////// Busca el protocolo correspondiente //////////////////////////////////////////////
                                        switch (s_tipoNumeracion)
                                        {
                                            case "numero":
                                                m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and  convert(varchar,numero)='" + numeroProtocolo + "'"; //estado<2 and 
                                                break;
                                            case "numeroSector":
                                                m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and  prefijosector+convert(varchar,numerosector)='" + numeroProtocolo + "'"; //estado<2 and 
                                                break;
                                            case "numeroTipoServicio":
                                                m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and idTipoServicio=1 and convert(varchar,numeroTipoServicio)='" + numeroProtocolo + "'"; //estado<2 and 
                                                break;
                                            case "numeroDiario":
                                                m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and fecha=getdate() and convert(varchar,numeroDiario)='" + numeroProtocolo + "'"; //estado<2 and 
                                                break;
                                        }
                                        ////////////////////////////////////////////////////

                                        DataSet Ds = new DataSet();

                                        da.SelectCommand = new SqlCommand(m_ssql, conn);
                                        da.Fill(Ds);
                                        if (Ds.Tables[0].Rows.Count > 0)
                                        {
                                            ////Busca el id del item que corresponde en el LIS
                                            s_idprotocolo = Ds.Tables[0].Rows[0].ItemArray[0].ToString();
                                            guardarResultados = true;
                                        }
                                    }
                                    break;
                                case "R":


                                    string s_idItemA = "";
                //                    string s_redondeo = "";

                                    string s_CampoResultado = arr[2].ToString();
                                    string s_ValorResultado = arr[3].ToString();
                                    string s_UnidadMedida = arr[4].ToString();


                                    string[] arrAux1 = s_CampoResultado.Split(("^").ToCharArray());
                                    if (arrAux1.Length > 0)
                                    {
                                        s_CampoIdentItem = arrAux1[3].ToString();
                                    }

                                    if (s_idprotocolo != "") guardarResultados = true;
                                    // if (s_ValorResultado.Substring(0, 3) == "PNG") guardarResultados = false;

                                    ///
                                    /// 
                                    if (guardarResultados)
                                    {
                                        /////////SACAR ESTA PARTE EN UN FUTURO
                                        ///Por cada uno ingresa un registro en la base                                
                                        m_ssql = @" INSERT INTO LAB_SysmexResultado (protocolo,idItemSysmex,unidadMedida ,valorObtenido ,fechaRegistro)           
                                                    VALUES ('" + numeroProtocolo + "', '" + s_CampoIdentItem + "','" + s_UnidadMedida + "','" + s_ValorResultado + "', getdate())";
                                        SetTextEnviado(m_ssql);

                                        SqlCommand cmd = new SqlCommand(m_ssql, conn);
                                        conn.Open();
                                        da.InsertCommand = cmd;
                                        da.InsertCommand.ExecuteNonQuery();
                                        //////////SACAR HASTA ACA

                                        try
                                        {

                                        //    if (equipo == "SYSMEXXS1000")
                                            m_ssql = @"select top 1 idItem  from  LAB_CobasB221Item where habilitado=1 and idCobas='" + s_CampoIdentItem.Trim() + "'";
                                            //if (equipo == "SYSMEXXT1800")
                                            //    m_ssql = @"select top 1 idItem, redondeo  from  LAB_SysmexItemXT1800 where habilitado=1 and idSysmex='" + s_CampoIdentItem.Trim() + "'";


                                            DataSet DsItem = new DataSet();
                                            da.SelectCommand = new SqlCommand(m_ssql, conn);
                                            da.Fill(DsItem);

                                            if (DsItem.Tables[0].Rows.Count > 0)
                                            {
                                                if (EsNumerico(s_ValorResultado))
                                                {
                                                    s_idItemA = DsItem.Tables[0].Rows[0].ItemArray[0].ToString();
                                               //     s_redondeo = DsItem.Tables[0].Rows[0].ItemArray[1].ToString();

                                                    decimal s_ItemNum = decimal.Parse(s_ValorResultado.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                                                 
                                                    /////Actualiza en valor devuelto en detalleProtocolo
                                                    m_ssql = @" Update  LAB_DetalleProtocolo set resultadonum=" + s_ItemNum.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", conresultado=1, enviado=2, fechaResultado= getdate()  where idUsuarioValida=0 and idProtocolo=" + s_idprotocolo + " and idSubItem=" + s_idItemA;
                                                    SqlCommand cmdUpdate = new SqlCommand(m_ssql, conn);
                                                    da.InsertCommand = cmdUpdate;
                                                    da.InsertCommand.ExecuteNonQuery();
                                                

                                                    SetTextEnviado(DateTime.Now.ToShortTimeString() + " Se ha grabado un registro: " + s_CampoIdentItem + " " + s_ValorResultado);
                                                }
                                            }
                                            if (s_idItemA != "")
                                            {
                                                m_ssql = @" Update  LAB_Protocolo set estado=1  where estado=0 and idProtocolo=" + s_idprotocolo;
                                                SqlCommand cmdUpdateProtocolo = new SqlCommand(m_ssql, conn);
                                                da.InsertCommand = cmdUpdateProtocolo;
                                                da.InsertCommand.ExecuteNonQuery();
                                            }


                                        }
                                        catch (Exception ex)
                                        {
                                            SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error al grabar: " + ex.Message);
                                        }

                                        conn.Close();

                                    }

                                    break;

                                case "L": numeroProtocolo = "0"; guardarResultados = false;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error: " + ex.ToString());
            }
        }


        private void AnalizarASTM1381(string mensagito, NetworkStream clientStream, string equipo)
        {
            ///////////////////////SYSMEX XS y XT: CONEXION DE TIPO BIRECCIONAL CON CODIGO DE BARRAS//////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["CN"].ToString();
            // SetTextRecibido(DateTime.Now.ToShortTimeString() + " - Mensaje Recibido Sysmex: " + mensagito);
            string mensaje = DateTime.Now.ToShortTimeString() + " - Mensaje Recibido Sysmex: " + mensagito;

            try
            {                                
                string m_ssql = "";
                SqlConnection conn = new SqlConnection(connString); SqlDataAdapter da = new SqlDataAdapter();

                string[] srecord = mensagito.Split(("\r").ToCharArray());
                bool guardarResultados = false;
                foreach (string r in srecord)
                {
                    if (r.Trim() != "")
                    {
                        ///antes hacer slit para separar por caracterer de separador de lineas
                        string[] arr = r.Split(("|").ToCharArray());
                        string s_CampoIdentItem = "0";

                        if (arr.Length > 0)
                        {
                            string s_tipoSegmento = arr[0].ToString();
                            switch (s_tipoSegmento.Trim())
                            {
                                case "H":
                                /////Inicio del segmento
                                    numeroProtocolo = "0"; s_idprotocolo = ""; s_idLinfocitos = "";
                                    break;
                                case "Q": 
                                ///Este segmento es para pedir al LIS que descargue un protocolo
                                    numeroProtocolo = "0"; s_idprotocolo = "";
                                    guardarResultados = false; s_idLinfocitos = "";

                                    /// sacar  rack y posición para armar el mensaje
                                    string s_ubicacion = arr[2].ToString();
                                    string[] arrUbicacion = s_ubicacion.Split(("^").ToCharArray());
                                    if (arrUbicacion.Length > 0)
                                    {
                                        string posicion = arrUbicacion[0].ToString();
                                        string rack = arrUbicacion[1].ToString();
                                        if (arrUbicacion.Length > 2)//si es con codigo de barras
                                        {
                                            string codigo_barras = arrUbicacion[3].ToString();
                                            string codigo_control= arrUbicacion[4].ToString();
                                            if (codigo_control == "B")//si es con codigo de barras
                                            { 
                                                string mensajeReturnCodigoBarras = GenerarMensajeASTMCodigoBarras(posicion, rack,codigo_barras, equipo);
                                            EnviarMensajeCliente(mensajeReturnCodigoBarras, clientStream);
                                            SetTextEnviado(DateTime.Now.ToShortTimeString() + " Se han enviado el mensaje " + mensajeReturnCodigoBarras);
                                            }
                                        } ///// longitud menor a dos no es codigo de barras
                                            else
                                            {
                                        string mensajeReturn = GenerarMensajeASTM(posicion, rack, equipo);
                                        EnviarMensajeCliente(mensajeReturn, clientStream);
                                        SetTextEnviado(DateTime.Now.ToShortTimeString() + " Se han enviado el mensaje " + mensajeReturn);
                                            }
                                     
                                        
                                    }                                                                        

                                    break;

                                case "O":
                                  
                                    string s_CampoProtocolo = arr[3].ToString();
                                    string[] arrAux = s_CampoProtocolo.Split(("^").ToCharArray());
                                    if (arrAux.Length > 0)
                                    {
                                        numeroProtocolo = arrAux[2].ToString().Trim().ToUpper();

                                        ///////////////////////// Busca el protocolo correspondiente //////////////////////////////////////////////
                                        switch (s_tipoNumeracion)
                                        {
                                            case "numero":
                                                m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and  convert(varchar,numero)='" + numeroProtocolo + "'"; //estado<2 and 
                                                break;
                                            case "numeroSector":
                                                m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and  prefijosector+convert(varchar,numerosector)='" + numeroProtocolo + "'"; //estado<2 and 
                                                break;
                                            case "numeroTipoServicio":
                                                m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and idTipoServicio=1 and convert(varchar,numeroTipoServicio)='" + numeroProtocolo + "'"; //estado<2 and 
                                                break;
                                            case "numeroDiario":
                                                m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and fecha=getdate() and convert(varchar,numeroDiario)='" + numeroProtocolo + "'"; //estado<2 and 
                                                break;
                                        }
                                        ////////////////////////////////////////////////////

                                        
                                        DataSet Ds = new DataSet();

                                        da.SelectCommand = new SqlCommand(m_ssql, conn);
                                        da.Fill(Ds);

                                        if (Ds.Tables[0].Rows.Count > 0)
                                        {
                                            ////Busca el id del item que corresponde en el LIS
                                            s_idprotocolo = Ds.Tables[0].Rows[0].ItemArray[0].ToString();
                                            guardarResultados = true;
                                        }
                                    }
                                    break;
                                case "R":

                               
                                    string s_idItemA = "";
                                    string s_redondeo = "";

                                    string s_CampoResultado = arr[2].ToString();
                                    string s_ValorResultado = arr[3].ToString();
                                    string s_UnidadMedida = arr[4].ToString();


                                    string[] arrAux1 = s_CampoResultado.Split(("^").ToCharArray());
                                    if (arrAux1.Length > 0)
                                    {
                                        s_CampoIdentItem = arrAux1[4].ToString();
                                    }

                                    if (s_idprotocolo != "") guardarResultados = true;
                                  // if (s_ValorResultado.Substring(0, 3) == "PNG") guardarResultados = false;

                                    ///
                                    /// 
                                    if (guardarResultados)
                                    {
//                                        /////////SACAR ESTA PARTE EN UN FUTURO
                                        ///Por cada uno ingresa un registro en la base                                
                                        m_ssql = @" INSERT INTO LAB_SysmexResultado (protocolo,idItemSysmex,unidadMedida ,valorObtenido ,fechaRegistro)           
                                                    VALUES ('" + numeroProtocolo + "', '" + s_CampoIdentItem + "','" + s_UnidadMedida + "','" + s_ValorResultado + "', getdate())";

                                        SqlCommand cmd = new SqlCommand(m_ssql, conn);
                                        conn.Open();
                                        da.InsertCommand = cmd;
                                        da.InsertCommand.ExecuteNonQuery();


                                        try
                                        {

                                            if (equipo == "SYSMEXXS1000")
                                                m_ssql = @"select top 1 idItem, redondeo  from  LAB_SysmexItem where habilitado=1 and idSysmex='" + s_CampoIdentItem.Trim() + "'";
                                            if (equipo == "SYSMEXXT1800")
                                                m_ssql = @"select top 1 idItem, redondeo  from  LAB_SysmexItemXT1800 where habilitado=1 and idSysmex='" + s_CampoIdentItem.Trim() + "'";


                                                DataSet DsItem = new DataSet();                                                                                             
                                                da.SelectCommand = new SqlCommand(m_ssql, conn);
                                                da.Fill(DsItem);

                                                if (DsItem.Tables[0].Rows.Count > 0)
                                                {
                                                    if  (EsNumerico(  s_ValorResultado))
                                                    {
                                                        s_idItemA = DsItem.Tables[0].Rows[0].ItemArray[0].ToString();
                                                        s_redondeo = DsItem.Tables[0].Rows[0].ItemArray[1].ToString();

                                                        decimal s_ItemNum = decimal.Parse(s_ValorResultado.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                                                        //if (s_CampoIdentItem != "LYMPH%")
                                                        //{
                                                        //    s_ItemNum = AnalizaRedondeoContadorHemtologicoSysmex(s_CampoIdentItem, s_ItemNum, s_UnidadMedida, s_redondeo);
                                                            /////Actualiza en valor devuelto en detalleProtocolo
                                                            m_ssql = @" Update  LAB_DetalleProtocolo set resultadonum=" + s_ItemNum.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", conresultado=1, enviado=2, fechaResultado= getdate()  where idUsuarioValida=0 and idProtocolo=" + s_idprotocolo + " and idSubItem=" + s_idItemA;
                                                            SqlCommand cmdUpdate = new SqlCommand(m_ssql, conn);
                                                            da.InsertCommand = cmdUpdate;
                                                            da.InsertCommand.ExecuteNonQuery();
                                                       // }

                                                        /////Calcula para Linfocitos la diferencia entre la suma 100/////////////////////////////
                                                        //if (s_CampoIdentItem == "LYMPH%")  s_idLinfocitos = s_idItemA;
                                                        //if (s_CampoIdentItem == "NEUT%") suma100 =  suma100 + int.Parse(s_ItemNum.ToString());
                                                        //if (s_CampoIdentItem == "MONO%") suma100 = suma100 + int.Parse(s_ItemNum.ToString());
                                                        //if (s_CampoIdentItem == "EO%") suma100 = suma100 + int.Parse(s_ItemNum.ToString());
                                                        //if (s_CampoIdentItem == "BASO%")
                                                        //{
                                                        //    if (s_idLinfocitos != "")
                                                        //    {
                                                        //        suma100 += int.Parse(s_ItemNum.ToString());
                                                        //        if (suma100 <= 100)
                                                        //        {
                                                        //            int valorLinfocitos = 100 - suma100;
                                                        //            /////Actualiza en valor devuelto en detalleProtocolo
                                                        //            m_ssql = @" Update  LAB_DetalleProtocolo set resultadonum=" + valorLinfocitos.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", conresultado=1, enviado=2, fechaResultado= getdate()  where idUsuarioValida=0 and idProtocolo=" + s_idprotocolo + " and idSubItem=" + s_idLinfocitos;
                                                        //            SqlCommand cmdUpdate = new SqlCommand(m_ssql, conn);
                                                        //            da.InsertCommand = cmdUpdate;
                                                        //            da.InsertCommand.ExecuteNonQuery();
                                                        //        }
                                                        //    }
                                                        //}
                                                        //////////////////////Fin calculo para Linfocitos.////////////////////////////////////
                                                    
                                                        SetTextEnviado(DateTime.Now.ToShortTimeString() + " Se ha grabado un registro: " + s_CampoIdentItem + " " + s_ValorResultado);
                                                    }
                                                }
                                                if (s_idItemA != "") 
                                                {
                                                    m_ssql = @" Update  LAB_Protocolo set estado=1  where estado=0 and idProtocolo=" + s_idprotocolo;
                                                    SqlCommand cmdUpdateProtocolo = new SqlCommand(m_ssql, conn);
                                                    da.InsertCommand = cmdUpdateProtocolo;
                                                    da.InsertCommand.ExecuteNonQuery();
                                                }
                                            

                                        }
                                        catch (Exception ex)
                                        {
                                            SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error al grabar: " + ex.Message);
                                        }

                                        conn.Close();

                                    }

                                    break;

                                case "L": numeroProtocolo = "0"; guardarResultados = false;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error: " + ex.ToString());
            }
        }

        private string GenerarMensajeASTMCodigoBarras(string posicion, string rack, string m_codigoBarras, string equipo)
        {
          
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["CN"].ToString();

            string mensajeReturn = "";
            string m_ssql = "";
            if (equipo == "SYSMEXXS1000")
                m_ssql = @" SELECT     dbo.NumeroProtocolo(P.idProtocolo) AS numero, Pac.numerodocumento , Pac.apellido as apellido, 
                            Pac.fechaNacimiento AS fechaNacimiento, P.prefijoSector AS sector, Sol.solicitante, SI.idSysmex, Pac.nombre as nombrePaciente, P.sexo, P.observacion
                      FROM         LAB_Protocolo AS P INNER JOIN
                      Sys_Paciente AS Pac ON P.idPaciente = Pac.idPaciente  INNER JOIN
                      vta_LAB_SolicitanteProtocolo AS Sol ON P.idProtocolo = Sol.idProtocolo INNER JOIN
                      LAB_DetalleProtocolo AS D ON P.idProtocolo = D.idProtocolo INNER JOIN              
                      LAB_SysmexItem AS SI ON D.idSubItem = SI.idItem           ";         
            
            if (equipo == "SYSMEXXT1800")
                m_ssql = @" SELECT     dbo.NumeroProtocolo(P.idProtocolo) AS numero, Pac.numerodocumento , Pac.apellido as apellido, 
                            Pac.fechaNacimiento AS fechaNacimiento, P.prefijoSector AS sector, Sol.solicitante, SI.idSysmex, Pac.nombre as nombrePaciente, P.sexo, P.observacion
                      FROM         LAB_Protocolo AS P INNER JOIN
                      Sys_Paciente AS Pac ON P.idPaciente = Pac.idPaciente  INNER JOIN
                      vta_LAB_SolicitanteProtocolo AS Sol ON P.idProtocolo = Sol.idProtocolo INNER JOIN
                      LAB_DetalleProtocolo AS D ON P.idProtocolo = D.idProtocolo INNER JOIN
                      LAB_SysmexItemXT1800 AS SI ON D.idSubItem = SI.idItem           ";

            switch (s_tipoNumeracion)
            {
                case "numero":
                    m_ssql += @" where  P.estado<2 and convert(varchar,P.numero)='" + m_codigoBarras + "'"; //estado<2 and 
                    break;
                case "numeroSector":
                    m_ssql += @" where P.estado<2 and P.prefijosector+convert(varchar,P.numerosector)='" + m_codigoBarras + "'"; //estado<2 and 
                    break;
                case "numeroTipoServicio":
                    m_ssql += @" where P.estado<2 and P.idTipoServicio=1 and convert(varchar,P.numeroTipoServicio)='" + m_codigoBarras + "'"; //estado<2 and 
                    break;
                case "numeroDiario":
                    m_ssql += @" where P.estado<2 and P.fecha=getdate() and convert(varchar,P.numeroDiario)='" + m_codigoBarras + "'"; //estado<2 and 
                    break;
            }


            DataSet Ds = new DataSet();
            SqlConnection conn = new SqlConnection(connString);
            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(m_ssql, conn);
            da.Fill(Ds);

            ////Toma los datos de la tabla-**********
            int totalMensajes = Ds.Tables[0].Rows.Count;
            string m_FechaProtocolo = DateTime.Now.ToString("yyyyMMddhhmmss");                                        
            ///////////////////////


            if (totalMensajes == 0)///Si no hay datos responde que no hay
            {
                mensajeReturn = @"H|\^&|||||||||||E1394-97
P|1
O|1|posicion^rack^m_codigoBarras^B||||m_FechaProtocolo|||||||||||||||||||Y
L|1|N
";
                mensajeReturn = mensajeReturn.Replace("posicion", posicion);
                mensajeReturn = mensajeReturn.Replace("rack", rack);
                mensajeReturn = mensajeReturn.Replace("m_codigoBarras", m_codigoBarras);
                mensajeReturn = mensajeReturn.Replace("m_FechaProtocolo", m_FechaProtocolo);
                mensajeReturn = mensajeReturn.Replace(@"^~\\&", @"^~\&");
            }
            else  ///Si encontró un protocolo lo envia
            {
                string m_numero = Ds.Tables[0].Rows[0][0].ToString();
                string m_numero_documento = Ds.Tables[0].Rows[0][1].ToString();
                string m_apellidopaciente = Ds.Tables[0].Rows[0][2].ToString();
                string m_anioNacimiento = Ds.Tables[0].Rows[0][3].ToString();
                //string m_sector = Ds.Tables[0].Rows[0][4].ToString();
                string m_medico = Ds.Tables[0].Rows[0][5].ToString();
                string m_nombrepaciente = Ds.Tables[0].Rows[0][7].ToString();
                string m_sexo = Ds.Tables[0].Rows[0][8].ToString();
                string m_comentarios = Ds.Tables[0].Rows[0][9].ToString();
                if (m_sexo == "I") m_sexo = "U"; //Unknown
                string m_listaItem = "";
                for (int i = 0; i < totalMensajes; i++)
                {
                    string m_idItemSyxmex = Ds.Tables[0].Rows[i][6].ToString();
                    if (m_listaItem == "")
                        m_listaItem = m_idItemSyxmex;
                    else
                        m_listaItem += "|" + m_idItemSyxmex;

                    switch (m_idItemSyxmex)
                    {
                        case "NEUT%": m_listaItem += "|NEUT#"; break;
                        case "LYMPH%": m_listaItem += "|LYMPH#"; break;
                        case "MONO%": m_listaItem += "|MONO#"; break;
                        case "EO%": m_listaItem += "|EO#"; break;
                        case "BASO%": m_listaItem += "|BASO#"; break;
                    }
                }                
                                
                string aux = "";
                string[] arr = m_listaItem.Split(("|").ToCharArray());
                foreach (string m in arr)
                {
                     if (m.Trim() != "")
                    {
                        if (aux == "")
                            aux = @"^^^^" + m;
                        else
                            aux += @"\^^^^" + m;
                    }
                }

                mensajeReturn = @"H|\^&|||||||||||E1394-97
P|1|||m_numero_documento|^m_apellidopaciente^m_nombrepaciente||m_anioNacimiento|m_sexo|||||m_medico|||||||||||||^^^
C|1||m_comentarios
O|1|posicion^rack^m_numero^B||aux||m_FechaProtocolo|||||N||||||||||||||Q
L|1|N
";
                mensajeReturn = mensajeReturn.Replace("m_numero_documento", m_numero_documento);
                mensajeReturn = mensajeReturn.Replace("m_apellidopaciente", m_apellidopaciente);
                mensajeReturn = mensajeReturn.Replace("m_nombrepaciente", m_nombrepaciente);
                mensajeReturn = mensajeReturn.Replace("m_anioNacimiento", m_anioNacimiento);
                mensajeReturn = mensajeReturn.Replace("m_sexo", m_sexo);
                mensajeReturn = mensajeReturn.Replace("m_medico", m_medico);
                mensajeReturn = mensajeReturn.Replace("posicion", posicion);
                mensajeReturn = mensajeReturn.Replace("rack", rack);
                mensajeReturn = mensajeReturn.Replace("m_numero", m_numero);
                mensajeReturn = mensajeReturn.Replace("aux", aux);
                mensajeReturn = mensajeReturn.Replace("m_FechaProtocolo", m_FechaProtocolo);
                mensajeReturn = mensajeReturn.Replace("m_comentarios", m_comentarios);
                
                mensajeReturn = mensajeReturn.Replace(@"^~\\&", @"^~\&");                                   
            }
            conn.Close();

            return mensajeReturn;
        }

        private string GenerarMensajeASTM(string posicion, string rack, string equipo)
        {   ////Envio de muestras en lote 

            ///Por cada mensaje de solicitud se envia un protocolo.            
            string mensajeReturn = "";
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["CN"].ToString(); // @"data source=.;database=SIntegralHChosMalal; USER ID=sa; Password=sa; Integrated Security=false;";   

            ///Buscar en la tabla LAB_TempProtocoloEnvio y generar un mensaje para cada registro 
            string m_ssql =      @" SELECT top 1  numeroProtocolo,  paciente, anioNacimiento, sexo, 
                               sectorSolicitante,  iditem, urgente,medicoSolicitante,idMuestra
                               FROM  LAB_TempProtocoloEnvio ";

            if (equipo == "SYSMEXXS1000") m_ssql +=   " where Equipo='SysmexXS1000' ";
            if (equipo == "SYSMEXXT1800") m_ssql += " where Equipo='SysmexXT1800' ";



            DataSet Ds = new DataSet();
            SqlConnection conn = new SqlConnection(connString);
            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter();

            da.SelectCommand = new SqlCommand(m_ssql, conn);
            da.Fill(Ds);                    
              
            
            ////Toma los datos de la tabla-**********
            int totalMensajes = Ds.Tables[0].Rows.Count; 
            string m_FechaProtocolo = DateTime.Now.ToString("yyyyMMddhhmmss");
            ///////////////////////
            if (totalMensajes == 0)///Si no hay datos responde que no hay
            {
                mensajeReturn = @"H|\^&|||||||||||E1394-97
P|1
O|1|posicion^rack^^C||||m_FechaProtocolo|||||||||||||||||||Y
L|1|N
";
                mensajeReturn = mensajeReturn.Replace("posicion", posicion);
                mensajeReturn = mensajeReturn.Replace("rack", rack);
                mensajeReturn = mensajeReturn.Replace("m_FechaProtocolo", m_FechaProtocolo);
                mensajeReturn = mensajeReturn.Replace(@"^~\\&", @"^~\&");
            }
            else  ///Si encontró un protocolo lo envia
            {
                try
                {
                    string m_numero = Ds.Tables[0].Rows[0][0].ToString();

                    string[] aux_paciente = Ds.Tables[0].Rows[0][1].ToString().Split(("-").ToCharArray());
                    string m_numero_documento = aux_paciente[0];
                    string m_paciente = aux_paciente[1];

                    string m_anioNacimiento = Ds.Tables[0].Rows[0][2].ToString();
                    string m_sexo = Ds.Tables[0].Rows[0][3].ToString();

                    string m_sectorSolicitante = Ds.Tables[0].Rows[0][4].ToString();
                    string lista_item = Ds.Tables[0].Rows[0][5].ToString();


                    string aux = "";
                    string[] arr = lista_item.Split(("|").ToCharArray());
                    foreach (string m in arr)
                    {
                        if (m.Trim() != "")
                        {
                            if (aux == "")
                                aux = @"^^^^" + m;
                            else
                                aux += @"\^^^^" + m;
                        }
                    }


                    mensajeReturn = @"H|\^&|||||||||||E1394-97
P|1|||m_numero_documento|^m_paciente^ ||m_anioNacimiento|m_sexo||||||||||||||||||^^^
C|1||sin comentarios
O|1|posicion^rack^m_numero^C||aux||m_FechaProtocolo|||||N||||||||||||||Q
L|1|N
";
                    mensajeReturn = mensajeReturn.Replace("m_numero_documento", m_numero_documento);
                    mensajeReturn = mensajeReturn.Replace("m_paciente", m_paciente);
                    mensajeReturn = mensajeReturn.Replace("m_anioNacimiento", m_anioNacimiento);
                    mensajeReturn = mensajeReturn.Replace("m_sexo", m_sexo);
                    mensajeReturn = mensajeReturn.Replace("posicion", posicion);
                    mensajeReturn = mensajeReturn.Replace("rack", rack);
                    mensajeReturn = mensajeReturn.Replace("m_numero", m_numero);
                    mensajeReturn = mensajeReturn.Replace("aux", aux);
                    mensajeReturn = mensajeReturn.Replace("m_FechaProtocolo", m_FechaProtocolo);

                    mensajeReturn = mensajeReturn.Replace(@"^~\\&", @"^~\&");

                    m_ssql = @" Delete  LAB_TempProtocoloEnvio where numeroProtocolo='" + m_numero;
                    if (equipo=="SYSMEXXS1000")  m_ssql += "' and Equipo='SysmexXS1000'";
                    if (equipo=="SYSMEXXT1800")  m_ssql += "' and Equipo='SysmexXT1800'";

                    SqlCommand cmdUpdate = new SqlCommand(m_ssql, conn);
                    da.DeleteCommand = cmdUpdate;
                    da.DeleteCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error al grabar: " + ex.Message);
                }
               
            }
            conn.Close();

            return mensajeReturn;

        
        }

        private decimal AnalizaRedondeoContadorHemtologicoSysmex(string s_Item, decimal s_ItemNum, string s_unidad, string s_redondeo)
        {
            decimal valor = s_ItemNum;

            if (s_redondeo == "True")
            {
                if (s_Item == "WBC") valor = Math.Round(valor, 1);//recuento de globulos blancos
                if (s_Item == "RBC") valor = Math.Round(valor, 1);//recuento de globulos rojos
            }
            
            if (s_unidad == "%")///Si es porcentaje redodea a numero entero, tomando el 0.5 como redondeo para arriba.
            {
                
                decimal a = Math.Round(valor);
                decimal b = valor;
                decimal c = b - a;
                if (c >= decimal.Parse("0.5")) a = a + 1;

                valor = a;

                              
                if (s_Item == "HCT")
                { 
                    if (s_redondeo == "True")  valor = Math.Round(valor);//hematocrito                
                    else valor = b;//hematocrito                
                }



                if (s_Item == "RDW-CV")
                {
                    if (s_redondeo == "True") valor = Math.Round(valor);//hematocrito                
                    else
                        valor = b;//hematocrito    
                }

                if (s_Item == "NEUT%") valor = Math.Round(valor);///neutrofilos
                if (s_Item == "MONO%") valor = Math.Round(valor);//monocitos
                if (s_Item == "EO%") valor = Math.Round(valor);///eosinofilos
                if (s_Item == "BASO%") valor = Math.Round(valor);//basofilos

              //  if (s_Item == "LYMPH%") valor = Math.Round(valor);///linfocitos.



            }
            ///si no es porcentaje se analiza si tiene un multiplicador en la Unidad por miles o millones, etc. y se aplica el multiplicador.
            ///comento esto para el Castro Rendon por que informan con unidad técnica. 
            
            //else  
            //{
            //    string[] arrAux = s_unidad.Split(("/").ToCharArray());
            //    if (arrAux.Length > 1)
            //    {
            //        if (arrAux[1].ToString() == "uL")
            //        {
            //            string[] multiplicador = arrAux[0].ToString().Split(("*").ToCharArray());
            //            if (multiplicador.Length > 0)
            //            {
            //                if (multiplicador[0].ToString() == "10")
            //                {
            //                    double mult = Math.Pow(double.Parse("10"), double.Parse(multiplicador[1].ToString()));
            //                    valor = valor * decimal.Parse(mult.ToString());
            //                }
            //            }
            //        }
            //    }
            //}

            return valor;
        }


        protected bool EsNumerico(string val)
        {
            bool match;
            //regula expression to match numeric values
            string pattern = "(^[-+]?\\d+(,?\\d*)*\\.?\\d*([Ee][-+]\\d*)?$)|(^[-+]?\\d?(,?\\d*)*\\.\\d+([Ee][-+]\\d*)?$)";
            //generate new Regulsr Exoression eith the pattern and a couple RegExOptions
            Regex regEx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            //tereny expresson to see if we have a match or not
            match = regEx.Match(val).Success ? true : false;
            //return the match value (true or false)
            return match;
        }      


        private void AnalizarHL7(string mensagito, NetworkStream clientStream) 
        {         
            string connString =System.Configuration.ConfigurationManager.ConnectionStrings["CN"].ToString(); // @"data source=.;database=SIntegralHChosMalal; USER ID=sa; Password=sa; Integrated Security=false;";                
            string mensajeReturn = "";
            string m_ssql = "";


//            mensagito =@"MSH|^~\&|Company|ChemistryAnalyzer|||20120730115842||QRY^Q02|7|P|2.3.1|
//            QRD|20120730115842|D|D|7|||RD|A12563|OTH||||
//            QRF|ChemistryAnalyzer|20120730000000|20120730115842|||||||";

            mensagito = mensagito.Replace("", "").Replace("\v", "");
            //SetText(DateTime.Now.ToShortTimeString() + " - Mensaje Depurado: " + mensagito);

            PipeParser parser = new PipeParser();
            IMessage m = parser.Parse(mensagito, "2.3.1");
                        

            try
                {
 
                string tipoMensaje = m.GetStructureName();
                switch (tipoMensaje)
                {
                    case "ORU_R01": //Resultados Recibidos
                        NHapi.Model.V231.Message.ORU_R01 r = (NHapi.Model.V231.Message.ORU_R01)m;
                        try
                        {                          
                            //////////////////Decodifica los campos necesarios y guarda en la base de datos.
                            SetTextRecibido(DateTime.Now.ToShortTimeString() + " Mensaje recibido identificado como ORU_R01 - Comienza decodificación");

                            string idMensaje = "";
                            ORU_R01_PATIENT_RESULT xresult = r.GetPATIENT_RESULT();
                            ORU_R01_ORDER_OBSERVATION xorder = xresult.GetORDER_OBSERVATION();
                            OBR xorderOBR = xorder.OBR;

                            idMensaje = r.MSH.MessageControlID.Value;
                            ///sacar los datos de la orden con xorderOBR                            

                            string m_protocolo = xorderOBR.PlacerOrderNumber.EntityIdentifier.Value;
                            string m_prefijoEspecial = "";

                            if (m_protocolo.Trim() != "")
                            {
                                ////Decodifico el numero de la muestra para sacar el numero de protocolo    
                                string[] arr = m_protocolo.Split(("-").ToCharArray());
                                if (arr.Length > 1) m_protocolo = arr[0].ToString();
                                if (arr.Length == 3) m_prefijoEspecial = arr[2].ToString();
                                ////////////////////////////////////////////////////////////////////////

                                SetTextRecibido(DateTime.Now.ToShortTimeString() + " Protocolo recibido " + m_protocolo);
                                string m_fechaProtocolo = xorderOBR.ObservationDateTime.TimeOfAnEvent.Value;
                                string m_tipoMuestra = xorderOBR.SpecimenSource.SpecimenSourceNameOrCode.Identifier.Value.Trim();
                                ////Si no viene el dato del tipo de muestra por defecto se toma S (Suero)
                                if (m_tipoMuestra == "") m_tipoMuestra = "S";
                                /////////////

                                int cantidadObservaciones = xorder.OBSERVATIONRepetitionsUsed;
                                SetTextRecibido(DateTime.Now.ToShortTimeString() + " Identificado cantidad observaciones " + cantidadObservaciones);
                                int i;
                                for (i = 0; i < cantidadObservaciones; i++)
                                {
                                    bool grabar = true;
                                    ORU_R01_OBSERVATION xobservacion = xorder.GetOBSERVATION(i);
                                    OBX xdetalle = xobservacion.OBX;

                                    //string resultadoReal = sss[0].Data.ToString();

                                    string m_valorItem = "";
                                    //                                ID[] sFlag= xdetalle.GetAbnormalFlags();                                

                                    //sFlag[0].Value=L Bajo
                                    //sFlag[0].Value=N Normal
                                    //sFlag[0].Value=H Alto


                                    ///////Captura del Resultado: si el valor es nagetivo se captura de otro campo////////////
                                    Varies[] sss = xdetalle.GetObservationValue();
                                    m_valorItem = sss[0].Data.ToString();// xdetalle.UserDefinedAccessChecks.Value;
                                    if (m_valorItem.Substring(0, 1) == "*")
                                        grabar = false;

                                    if (grabar)
                                    {
                                        if (m_valorItem.Substring(0, 1) == "-")
                                            m_valorItem = xdetalle.UserDefinedAccessChecks.Value;

                                        ///////////////////////////////


                                        string m_idItem = xdetalle.ObservationIdentifier.Identifier.Value;
                                        string m_descripcionItem = xdetalle.ObservationSubID.Value;
                                        string m_unidadItem = xdetalle.Units.Identifier.Value;

                                        string M_aUX = xdetalle.ObservationResultStatus.Value;

                                        string m_tipoItem = xdetalle.ValueType.Value;
                                        string m_fechaItem = xdetalle.DateTimeOfTheObservation.TimeOfAnEvent.Value;

                                        string s_idprotocolo = "";
                                        string s_idItemA = "";

                                        SqlConnection conn = new SqlConnection(connString);
                                        SqlDataAdapter da = new SqlDataAdapter();

                                        /////////SACAR ESTA PARTE EN UN FUTURO
                                        ///Por cada uno ingresa un registro en la base                                
//                                        m_ssql = @" INSERT INTO LAB_MindrayResultado ([protocolo] ,[fechaProtocolo] ,[tipoMuestra],[idItemMindray] ,[descripcion] ,[unidadMedida] ,
//                                          [valorObtenido] ,[tipoValor],[fechaResultado] ,[fechaRegistro] ,[estado])   
//                                          VALUES ('" + m_protocolo + "', convert(datetime, '" + m_fechaProtocolo + "'), '" + m_tipoMuestra + "', " + m_idItem + ",'" + m_descripcionItem + "','" +
//                                                             m_unidadItem + "','" + m_valorItem + "','" + m_tipoItem + "',convert(datetime,'" + m_fechaItem + "'), getdate(),0)";

                                 
//                                        SqlCommand cmd = new SqlCommand(m_ssql, conn);
//                                        conn.Open();
//                                        da.InsertCommand = cmd;
//                                        da.InsertCommand.ExecuteNonQuery();
                                        //conn.Close();
                                        /////////////////////////////////////////////////////////////////

                                        try
                                        {

                                            switch (s_tipoNumeracion)
                                            {
                                                case "numero":
                                                    m_ssql = @"select top 1 idprotocolo from lab_protocolo where  estado<2 and convert(varchar,numero)='" + m_protocolo + "'"; //estado<2 and 
                                                    break;
                                                case "numeroSector":
                                                    m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and prefijosector+convert(varchar,numerosector)='" + m_protocolo + "'"; //estado<2 and 
                                                    break;
                                                case "numeroTipoServicio":
                                                    m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and idTipoServicio=1 and convert(varchar,numeroTipoServicio)='" + m_protocolo + "'"; //estado<2 and 
                                                    break;
                                                case "numeroDiario":
                                                    m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and fecha=getdate() and convert(varchar,numeroDiario)='" + m_protocolo + "'"; //estado<2 and 
                                                    break;
                                            }


                                            //m_ssql = @"select top 1 idprotocolo from lab_protocolo where estado<2 and prefijosector+convert(varchar,numerosector)='" + m_protocolo + "'";
                                            DataSet Ds = new DataSet();
                                            da.SelectCommand = new SqlCommand(m_ssql, conn);
                                            da.Fill(Ds);
                                            if (Ds.Tables[0].Rows.Count > 0)
                                            {
                                                ////Busca el id del item que corresponde en el LIS
                                                s_idprotocolo = Ds.Tables[0].Rows[0].ItemArray[0].ToString();

                                                m_ssql = @"select top 1 idItem from lab_mindrayitem where habilitado=1 and  idMindray=" + m_idItem + " and tipoMuestra='" + m_tipoMuestra + "' and prefijo='" + m_prefijoEspecial + "'";
                                                DataSet DsItem = new DataSet();

                                                da.SelectCommand = new SqlCommand(m_ssql, conn);
                                                da.Fill(DsItem);
                                                if (DsItem.Tables[0].Rows.Count > 0)
                                                {
                                                    s_idItemA = DsItem.Tables[0].Rows[0].ItemArray[0].ToString();

                                                    decimal s_ItemNum = decimal.Parse(m_valorItem.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                                                    /////Actualiza en valor devuelto por el Mindray en detalleProtocolo
                                                    m_ssql = @" Update  LAB_DetalleProtocolo set resultadonum=" + s_ItemNum.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", conresultado=1, enviado=2, fechaResultado= getdate()  where idUsuarioValida=0 and idProtocolo=" + s_idprotocolo + " and idSubItem=" + s_idItemA;
                                                    SqlCommand cmdUpdate = new SqlCommand(m_ssql, conn);

                                                    da.InsertCommand = cmdUpdate;
                                                    da.InsertCommand.ExecuteNonQuery();

                                                    //          conn.Close();
                                                    SetTextEnviado(DateTime.Now.ToShortTimeString() + " Se ha grabado un registro: " + m_idItem + " " + m_descripcionItem + " " + m_valorItem);
                                                }
                                                if (s_idItemA != "")
                                                {
                                                    m_ssql = @" Update  LAB_Protocolo set estado=1  where estado=0 and idProtocolo=" + s_idprotocolo;
                                                    SqlCommand cmdUpdateProtocolo = new SqlCommand(m_ssql, conn);
                                                    da.InsertCommand = cmdUpdateProtocolo;
                                                    da.InsertCommand.ExecuteNonQuery();
                                                }
                                            }

                                        }
                                        catch (Exception ex)
                                        {
                                            SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error al grabar: " + ex.Message);
                                        }

                                        conn.Close();
                                    }

                                }

                                ///////////////ACK///////////////////////
                                SetTextRecibido(DateTime.Now.ToShortTimeString() + " Fin de Grabado. Generando ACK");
                                ACK resultACK = new ACK();
                                IMessage resultACK1 = MakeACK(m, "AA", resultACK);
                                //result.SetMessage(message);
                                PipeParser parserACK = new PipeParser();
                                mensajeReturn = parserACK.Encode(resultACK1);

                                EnviarMensajeCliente(mensajeReturn, clientStream);

                                //return result.GetAckMessage();

                                //////////////////////////////////////  
                            }

                        }
                        catch (HL7Exception ex)
                        {
                            SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error: " + ex.ToString());
                            ACK result = new ACK();
                            IMessage result2 = MakeACK(m, "AE", result);
                            //result.SetMessage(message);
                            PipeParser parserACK = new PipeParser();
                            mensajeReturn = parserACK.Encode(result2);
                            EnviarMensajeCliente(mensajeReturn, clientStream);
                            
                        }
                  
                        break;

                    case "QRY_Q02": //Descargar protocolos por lotes --- falta hacer a demanda por codigo de barras
                        NHapi.Model.V231.Message.QRY_Q02 qry = (NHapi.Model.V231.Message.QRY_Q02)m;                                                
                        try
                        {
                            string mensaje_qck = "";
                            //mensaje para descargar con codigo de barras
                            //                             MSH|^~\&|Company|ChemistryAnalyzer|||20120730115842||QRY^Q02|7|P|2.3.1|
                            //QRD|20120730115842|D|D|7|||RD|A12563|OTH||||
                            //QRF|ChemistryAnalyzer|20120730000000|20120730115842|||||||

                            string m_FechaHoraInicio =qry.QRF.WhenDataStartDateTime.TimeOfAnEvent.Value.ToString();
                            string m_FechaHoraFin = qry.QRF.WhenDataEndDateTime.TimeOfAnEvent.Value.ToString();
                            string m_mensajeId = qry.MSH.MessageControlID.Value;

                           XCN[] ss=  qry.QRD.GetWhoSubjectFilter();
                           string m_codigoBarras = ss[0].IDNumber.ToString();
                            


                            //////////////////Decodifica los campos necesarios y guarda en la base de datos.
                            SetTextRecibido(DateTime.Now.ToShortTimeString() + " Identificado como QRY_Q02 ");


                            CE ce=   qry.QRD.GetWhatSubjectFilter(0);
                            if (ce.Identifier.Value != "CAN")
                            {
                                

                                ////Si el valor entre RD y OTH del segmento QRD es diferente de vacío : es una descarga por código de barras
                                if (m_codigoBarras != "")
                                {
                                    string m_protocolo = "";
                                    string m_tipoMuestra = "S";
                                    SetTextRecibido(DateTime.Now.ToShortTimeString() + " Solicita descarga Protocolo " + m_codigoBarras);
                                    string[] arrCB = m_codigoBarras.Split(("-").ToCharArray());
                                    if (arrCB.Length >= 1) m_protocolo = arrCB[0].ToString();
                                    if (arrCB.Length >= 2) m_tipoMuestra = arrCB[1].ToString();
                                    //if (arr.Length == 3) m_prefijoEspecial = arr[2].ToString();

                                    string datos = BuscarDatosProtocolo(m_protocolo, m_tipoMuestra);

                                    string[] arr = datos.Split(("&").ToCharArray());
                                    string m_numero =arr[0].ToString();
                                     m_tipoMuestra = getTipoMuestra(m_tipoMuestra);
                                    string m_paciente = arr[1].ToString();
                                    string m_anioNacimiento = arr[2].ToString();
                                    string m_sexo = arr[3].ToString();
                                    string m_sectorSolicitante = arr[4].ToString();
                                    string lista_item = arr[5].ToString();
                                    string m_urgente = "N";
                                    string ID_muestra = "1";///numero correlativo (no se usa)

                                    if (m_numero == "")
                                    {
                                        mensaje_qck = GenerarMensajeQCK_Q02(m_mensajeId, m_FechaHoraInicio, "NF");
                                        EnviarMensajeCliente(mensaje_qck, clientStream);
                                    }
                                    else
                                    {   //Genera mensaje QCK indicando que hay datos para enviar y procede a enviar DRS
                                        mensaje_qck = GenerarMensajeQCK_Q02(m_mensajeId, m_FechaHoraInicio, "OK");
                                        EnviarMensajeCliente(mensaje_qck, clientStream);

                                        mensajeReturn = GenerarMensajeDSR(1, m_codigoBarras, m_tipoMuestra, lista_item, m_paciente,
                                             m_mensajeId, m_FechaHoraInicio, m_FechaHoraFin, m_anioNacimiento, m_sexo,
                                             m_sectorSolicitante, m_urgente, true, ID_muestra, m_numero);
                                        mensajeReturn = mensajeReturn.Replace(@"^~\\&", @"^~\&");

                                        ////Envía al cliente
                                        EnviarMensajeCliente(mensajeReturn, clientStream);
                                    }
                                }
                                ///////////////////////////////////////////////////////////////////////////////////////////////////////////
                                else
                                {
                                    SetTextRecibido(DateTime.Now.ToShortTimeString() + " Solicita descargar Protocolos ");


                                    ///Buscar en la tabla LAB_TempProtocoloEnvio y generar un mensaje para cada registro 
                                    m_ssql = @" SELECT  numeroProtocolo, tipoMuestra, paciente, anioNacimiento, sexo, 
                                            sectorSolicitante,  iditem, urgente,medicoSolicitante,idMuestra
                                            FROM  LAB_TempProtocoloEnvio where Equipo='Mindray' ";

                                    DataSet Ds = new DataSet();
                                    SqlConnection conn = new SqlConnection(connString);
                                    conn.Open();
                                    SqlDataAdapter da = new SqlDataAdapter();

                                    da.SelectCommand = new SqlCommand(m_ssql, conn);
                                    da.Fill(Ds);

                                
                                    if (Ds.Tables[0].Rows.Count == 0)
                                    {  //Genera mensaje QCK indicando no hay datos para enviar NF: Not Found
                                        mensaje_qck = GenerarMensajeQCK_Q02(m_mensajeId, m_FechaHoraInicio, "NF");
                                        EnviarMensajeCliente(mensaje_qck, clientStream);
                                    }
                                    else
                                    {   //Genera mensaje QCK indicando que hay datos para enviar y procede a enviar DRS
                                        mensaje_qck = GenerarMensajeQCK_Q02(m_mensajeId, m_FechaHoraInicio, "OK");
                                        EnviarMensajeCliente(mensaje_qck, clientStream);

                                        ////Toma los datos de la tabla-**********
                                        int totalMensajes = Ds.Tables[0].Rows.Count;
                                        bool ultimo = false;
                                        for (int i = 0; i < totalMensajes; i++)
                                        {
                                            string m_numero = Ds.Tables[0].Rows[i][0].ToString();
                                            string m_tipoMuestra = Ds.Tables[0].Rows[i][1].ToString();
                                            string m_paciente = Ds.Tables[0].Rows[i][2].ToString();
                                            string m_anioNacimiento = Ds.Tables[0].Rows[i][3].ToString();
                                            string m_sexo = Ds.Tables[0].Rows[i][4].ToString();
                                            string m_sectorSolicitante = Ds.Tables[0].Rows[i][5].ToString();
                                            string lista_item = Ds.Tables[0].Rows[i][6].ToString();
                                            string m_urgente = Ds.Tables[0].Rows[i][7].ToString();
                                            string ID_muestra = Ds.Tables[0].Rows[i][9].ToString();

                                            if (i == totalMensajes - 1) ultimo = true;


                                            mensajeReturn = GenerarMensajeDSR(i + 1, m_numero, m_tipoMuestra, lista_item, m_paciente,
                                            m_mensajeId, m_FechaHoraInicio, m_FechaHoraFin, m_anioNacimiento, m_sexo,
                                            m_sectorSolicitante, m_urgente, ultimo, ID_muestra,"");
                                            mensajeReturn = mensajeReturn.Replace(@"^~\\&", @"^~\&");

                                            ////Envía al cliente
                                            EnviarMensajeCliente(mensajeReturn, clientStream);
                                            contador += 1;
                                        }      //  /**************     

                                        SetTextEnviado(DateTime.Now.ToShortTimeString() + " Se han enviado " + (contador - 1).ToString() + " mensajes DRS (con muestras a realizar)");
                                    }
                                    conn.Close();
                                }
                            }
                            else
                            {
                                 SetTextRecibido(DateTime.Now.ToShortTimeString() + " Solicita Cancelar la descarga ");
                            }
                        }                                                    
                        catch (HL7Exception ex)
                        {
                            SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error: " + ex.ToString());
                        }                
                        break;

                 

                }
            }
            catch (HL7Exception ex)
            {
                SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error: " + ex);
               
            }
            //return mensajeReturn;
            
        }

        private string getTipoMuestra(string m_tipoMuestra)
        {
            switch (m_tipoMuestra)
            {
                case "S": m_tipoMuestra = "Suero"; break;
                case "O": m_tipoMuestra = "Orina"; break;
                default: m_tipoMuestra = "Suero"; break;

            }
            return m_tipoMuestra;
        }

        private string BuscarDatosProtocolo(string m_codigoBarras, string m_tipoMuestra)
        {
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["CN"].ToString();
    
            string datos="";
            string m_ssql = @" SELECT  dbo.NumeroProtocolo( P.idProtocolo) as numero, Pac.apellido+ ' ' +  Pac.nombre as  paciente,year( Pac.fechaNacimiento) as anioNacimiento, P.prefijoSector AS sector, Sol.solicitante, MI.idMindray
            FROM         LAB_Protocolo AS P INNER JOIN
            Sys_Paciente AS Pac ON P.idPaciente = Pac.idPaciente INNER JOIN      
            vta_LAB_SolicitanteProtocolo AS Sol ON P.idProtocolo = Sol.idProtocolo INNER JOIN
            LAB_DetalleProtocolo AS D ON P.idProtocolo = D.idProtocolo INNER JOIN
            LAB_MindrayItem AS MI ON D.idSubItem = MI.idItem
           " ;

        

            switch (s_tipoNumeracion)
            {
                case "numero":
                    m_ssql += @" where  P.estado<2 and convert(varchar,P.numero)='" + m_codigoBarras + "' and MI.tipoMuestra='" +getTipoMuestra(m_tipoMuestra) +"'"; //estado<2 and 
                    break;
                case "numeroSector":
                    m_ssql += @"  where P.estado<2 and P.prefijosector+convert(varchar,P.numerosector)='" + m_codigoBarras + "' and MI.tipoMuestra='" +getTipoMuestra(m_tipoMuestra) +"'"; //estado<2 and 
                    break;
                case "numeroTipoServicio":
                    m_ssql += @" where P.estado<2 and P.idTipoServicio=1 and convert(varchar,P.numeroTipoServicio)='" + m_codigoBarras + "' and MI.tipoMuestra='" +getTipoMuestra(m_tipoMuestra) +"'"; //estado<2 and 
                    break;
                case "numeroDiario":
                    m_ssql += @" where P.estado<2 and P.fecha=getdate() and convert(varchar,P.numeroDiario)='" + m_codigoBarras + "' and MI.tipoMuestra='" +getTipoMuestra(m_tipoMuestra) +"'"; //estado<2 and 
                    break;
            }
            

            DataSet Ds = new DataSet();
            SqlConnection conn = new SqlConnection(connString);
            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter();

            da.SelectCommand = new SqlCommand(m_ssql, conn);
            da.Fill(Ds);


            int totalMensajes = Ds.Tables[0].Rows.Count;


            string m_listaItem = "";
            string numero = "";
            string paciente = "";
            string anioNacimiento = "";
            string sector = "";
            string solicitante = "";
            for (int i = 0; i < totalMensajes; i++)
            {
             numero = Ds.Tables[0].Rows[i][0].ToString();
             paciente = Ds.Tables[0].Rows[i][1].ToString();
             anioNacimiento = Ds.Tables[0].Rows[i][2].ToString();
             sector = Ds.Tables[0].Rows[i][3].ToString();
             solicitante = Ds.Tables[0].Rows[i][4].ToString();

                if (m_listaItem == "")
                    m_listaItem = Ds.Tables[0].Rows[i][5].ToString();
                else
                    m_listaItem += "|" + Ds.Tables[0].Rows[i][5].ToString();
                                        

            }
            datos = numero + "&" + paciente + "&" + anioNacimiento + "&" + sector + "&" + solicitante + "&" + m_listaItem;

            return datos;
        }

    

        private void GrabarMensaje(string mensagito)
        {
            try
            {
                ////////////inserta el mensaje en la tabla temporal de mensajes recibidos
                string connString = System.Configuration.ConfigurationManager.ConnectionStrings["CN"].ToString();
                SqlConnection conn = new SqlConnection(connString);
                SqlDataAdapter da = new SqlDataAdapter();
                SqlCommand cmd = new SqlCommand(@"INSERT INTO Temp_Mensaje (mensaje, fechaRegistro) values ('" + mensagito + "', getdate())", conn);


                conn.Open();
                da.InsertCommand = cmd;
                da.InsertCommand.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex)
            {
                SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error: " + ex.ToString());
            }
        }

        private string GenerarMensajeQCK_Q02(string m_mensajeId, string m_fechaProtocolo, string m_respuesta)
        {
            string aux = @"MSH|^~\\&|||Company|ChemistryAnalyzer|m_fechaProtocolo||QCK^Q02|m_mensajeId|P|2.3.1|
MSA|AA|m_mensajeId||||0|
ERR|0|
QAK|SR|m_respuesta|


";           
            return aux.Replace("m_mensajeId", m_mensajeId).Replace("m_fechaProtocolo", m_fechaProtocolo).Replace("m_respuesta",m_respuesta);
        }      
      

        private void EnviarMensajeCliente(string mensajeParaCliente, NetworkStream clientStream)
        {
            try
            {
                ASCIIEncoding encoder = new ASCIIEncoding();
                /////enviar mensaje a cliente                             
                mensajeParaCliente = mensajeParaCliente.Replace(@"^~\\&", @"^~\&");

                byte[] buffer = encoder.GetBytes(mensajeParaCliente);

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
                SetTextEnviado(DateTime.Now.ToShortTimeString() + " : " + mensajeParaCliente);
            }
            catch (Exception e)
            {
                SetTextEnviado(DateTime.Now.ToShortTimeString() + " Error en envío de mensaje: " + e.Message);
            }
            
        }

        private string GenerarMensajeDSR(int numero_DRS,string m_numero, string m_tipoMuestra, 
            string lista_item, string m_paciente, string m_mensajeId, string m_FechaHoraInicio, 
            string m_FechaHoraFin, string m_anioNacimiento, string m_sexo, string m_sectorSolicitante,
            string m_urgente, bool ultimo, string ID_muestra, string m_codigoBarras)
        {

            string count_mensaje = contador.ToString();
           // string inicio_idMuestra = txtInicioIDMuestra.Text;
            //int SAMPLE_ID = int.Parse(inicio_idMuestra) + contador;
            //string ID_muestra = SAMPLE_ID.ToString();

            string ID_drs = numero_DRS.ToString();
            if (ultimo) ID_drs = "";



            //Sin numero
            string mmensajedsr = @"MSH|^~\\&|||Company|ChemistryAnalyzer|m_FechaHoraFin||DSR^Q03|count_mensaje|P|2.3.1|
MSA|AA|count_mensaje||||0|
ERR|0|
QAK|SR|OK|
QRD|m_FechaHoraFin|D|D|m_mensajeId|||RD|m_codigoBarras|OTH||||
DSP|1|||||
DSP|2|||||
DSP|3||m_paciente|||
DSP|4||m_anioNacimiento|||
DSP|5||m_sexo|||
DSP|6|||||
DSP|7|||||
DSP|8|||||
DSP|9|||||
DSP|10|||||
DSP|11|||||
DSP|12|||||
DSP|13|||||
DSP|14|||||
DSP|15|||||
DSP|16|||||
DSP|17|||||
DSP|18|||||
DSP|19|||||
DSP|20|||||
DSP|21||m_numero|||
DSP|22||ID_muestra|||
DSP|23|||||
DSP|24|||||
DSP|25|||||
DSP|26||m_tipoMuestra|||
DSP|27|||||
DSP|28||m_sectorSolicitante|||
";

         

            int i = 29;
            string aux = "";
            string[] arr = lista_item.Split(("|").ToCharArray());
            foreach (string m in arr)
            {
                if (m.Trim() != "")
                {
                    string pos = i.ToString();
                    aux = @"DSP|i||m^^^|||
";
                    aux = aux.Replace("i", pos).Replace("m", m);
                }
                mmensajedsr += aux;
                i = i + 1;
            }


mmensajedsr+= @"DSC|ID_drs|


";
mmensajedsr = mmensajedsr.Replace("count_mensaje", count_mensaje);
mmensajedsr = mmensajedsr.Replace("m_mensajeId", m_mensajeId);
mmensajedsr = mmensajedsr.Replace("m_FechaHoraInicio", m_FechaHoraInicio).Replace("m_FechaHoraFin", m_FechaHoraFin);
mmensajedsr = mmensajedsr.Replace("ID_muestra", ID_muestra);
mmensajedsr = mmensajedsr.Replace("ID_drs", ID_drs);
mmensajedsr = mmensajedsr.Replace("m_tipoMuestra", m_tipoMuestra);
mmensajedsr = mmensajedsr.Replace("m_paciente", m_paciente);
mmensajedsr = mmensajedsr.Replace("m_anioNacimiento", m_anioNacimiento);
mmensajedsr = mmensajedsr.Replace("m_sexo", m_sexo);
mmensajedsr = mmensajedsr.Replace("m_sectorSolicitante", m_sectorSolicitante);
mmensajedsr = mmensajedsr.Replace("m_codigoBarras", m_codigoBarras);


mmensajedsr = mmensajedsr.Replace("m_numero", m_numero);



            return mmensajedsr;
         
        
        }


        

        public static IMessage MakeACK(IMessage inboundMessage, string ackCode, IMessage ackMessage)
        {
            Terser t = new Terser(inboundMessage);
            ISegment inboundHeader = null;
            try
            {
                inboundHeader = t.getSegment("MSH");
            }
            catch (NHapi.Base.HL7Exception)
            {
                throw new NHapi.Base.HL7Exception("Need an MSH segment to create a response ACK");
            }
            return MakeACK(inboundHeader, ackCode, ackMessage);
        }


        public static IMessage MakeACK(ISegment inboundHeader, string ackCode, IMessage ackMessage)
        {
            if (!inboundHeader.GetStructureName().Equals("MSH"))
                throw new NHapi.Base.HL7Exception(
                    "Need an MSH segment to create a response ACK (got " + inboundHeader.GetStructureName() + ")");

            // Find the HL7 version of the inbound message:
            //
            string version = null;
            try
            {
                version = Terser.Get(inboundHeader, 12, 0, 1, 1);
            }
            catch (NHapi.Base.HL7Exception)
            {
                // I'm not happy to proceed if we can't identify the inbound
                // message version.
                throw new NHapi.Base.HL7Exception("Failed to get valid HL7 version from inbound MSH-12-1");
            }

           
            // Create a Terser instance for the outbound message (the ACK).
           Terser terser = new Terser(ackMessage);
            
            // Populate outbound MSH fields using data from inbound message
            ISegment outHeader = (ISegment)terser.getSegment("MSH");
            DeepCopy.copy(inboundHeader, outHeader);

            // Now set the message type, HL7 version number, acknowledgement code
            // and message control ID fields:
            string sendingApp = terser.Get("/MSH-3");
            string sendingEnv = terser.Get("/MSH-4");
            terser.Set("/MSH-3", "LIS-Server");
            terser.Set("/MSH-4", "NanShan Hospital");
            terser.Set("/MSH-5", sendingApp);
            terser.Set("/MSH-6", sendingEnv);
            terser.Set("/MSH-7", DateTime.Now.ToString("yyyyMMddHHmmss"));
            terser.Set("/MSH-9", "ACK");
            terser.Set("/MSH-12", version);           
            terser.Set("/MSA-1", ackCode == null ? "AA" : ackCode);
            terser.Set("/MSA-2", Terser.Get(inboundHeader, 10, 0, 1, 1));
            terser.Set("/MSA-3","");
            terser.Set("/MSA-4", "");
            terser.Set("/MSA-5", "");
            terser.Set("/MSA-6", "0");
            terser.Set("/MSA-7", "");

            return ackMessage;
        }



        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtLogRecibido.Text = "";
            txtLogEnviado.Text = "";
        }

        private void btnDetener_Click(object sender, EventArgs e)
        {
            Detener();
        }

        private void Detener()
        {          
            canStopListening = true;
            tcpListener.Stop();
            SetTextRecibido(DateTime.Now.ToShortTimeString() + " - Se ha detenido el servidor. Para iniciar nuevamente haga clic en Iniciar.");
            btnIniciar.Enabled = true;
            btnDetener.Enabled = false;
          
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
           this.Close();
            Application.ExitThread();
            Application.Exit();
            
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            canStopListening = false;
            Start();
            btnIniciar.Enabled = false;
            btnDetener.Enabled = true;
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void btnGuardar_ClientSizeChanged(object sender, EventArgs e)
        {

        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            contador = 0;
        }
    }
}





// mensagito = @"H|\^&|||XS^00-16^65750^^^^05342311||||||||E1394-97
//P|1
//C|1
//O|1||     6^8^           D770^M|^^^^WBC\^^^^RBC\^^^^HGB\^^^^HCT\^^^^MCV\^^^^MCH\^^^^MCHC\^^^^PLT\^^^^NEUT%\^^^^LYMPH%\^^^^MONO%\^^^^EO%\^^^^BASO%\^^^^NEUT#\^^^^LYMPH#\^^^^MONO#\^^^^EO#\^^^^BASO#\^^^^RDW-SD\^^^^RDW-CV\^^^^MPV|||||||N||||||||||||||F
//C|1
//R|1|^^^^WBC^1|5.42|10*3/uL||N||||||20120507122907
//R|2|^^^^RBC^1|4.51|10*6/uL||N||||||20120507122907
//R|3|^^^^HGB^1|14.3|g/dL||N||||||20120507122907
//R|4|^^^^HCT^1|43.0|%||N||||||20120507122907
//R|5|^^^^MCV^1|95.3|fL||N||||||20120507122907
//R|6|^^^^MCH^1|31.7|pg||N||||||20120507122907
//R|7|^^^^MCHC^1|33.3|g/dL||N||||||20120507122907
//R|8|^^^^PLT^1|198|10*3/uL||N||||||20120507122907
//R|9|^^^^NEUT%^1|61.4|%||N||||||20120507122907
//R|10|^^^^LYMPH%^1|27.1|%||N||||||20120507122907
//R|11|^^^^MONO%^1|10.0|%||N||||||20120507122907
//R|12|^^^^EO%^1|1.1|%||N||||||20120507122907
//R|13|^^^^BASO%^1|0.4|%||N||||||20120507122907
//R|14|^^^^NEUT#^1|3.33|10*3/uL||N||||||20120507122907
//R|15|^^^^LYMPH#^1|1.47|10*3/uL||N||||||20120507122907
//R|16|^^^^MONO#^1|0.54|10*3/uL||N||||||20120507122907
//R|17|^^^^EO#^1|0.06|10*3/uL||N||||||20120507122907
//R|18|^^^^BASO#^1|0.02|10*3/uL||N||||||20120507122907
//R|19|^^^^RDW-SD^1|44.3|fL||N||||||20120507122907
//R|20|^^^^RDW-CV^1|13.2|%||N||||||20120507122907
//R|21|^^^^MPV^1|11.8|fL||N||||||20120507122907
//R|22|^^^^Blasts?|0|||||||||20120507122907
//R|23|^^^^Immature_Gran?|20|||||||||20120507122907
//R|24|^^^^Left_Shift?|0|||||||||20120507122907
//R|25|^^^^NRBC?|0|||||||||20120507122907
//R|26|^^^^Atypical_Lympho?|10|||||||||20120507122907
//R|27|^^^^Abn_Lympho/Blasts?|0|||||||||20120507122907
//R|28|^^^^RBC_Agglutination?|70|||||||||20120507122907
//R|29|^^^^Turbidity/HGB_Interference?|90|||||||||20120507122907
//R|30|^^^^Iron_Deficiency?|70|||||||||20120507122907
//R|31|^^^^HGB_Defect?|70|||||||||20120507122907
//R|32|^^^^Fragments?|0|||||||||20120507122907
//R|33|^^^^PLT_Clumps?|20|||||||||20120507122907
//R|34|^^^^PLT_Clumps(S)?|60|||||||||20120507122907
//R|35|^^^^SCAT_DIFF|PNG&R&20120507&R&2012_05_07_12_29_D770_DIFF.PNG|||N||||||20120507122907
//R|36|^^^^DIST_RBC|PNG&R&20120507&R&2012_05_07_12_29_D770_RBC.PNG|||N||||||20120507122907
//R|37|^^^^DIST_PLT|PNG&R&20120507&R&2012_05_07_12_29_D770_PLT.PNG|||N||||||20120507122907
//L|1|N
//";



//            mensagito = @"H|\^&|||XS^00-16^65750^^^^05342311||||||||E1394-97
//P|1
//C|1
//O|1||     6^8^           44914^M|^^^^WBC\^^^^RBC\^^^^HGB\^^^^HCT\^^^^MCV\^^^^MCH\^^^^MCHC\^^^^PLT\^^^^NEUT%\^^^^LYMPH%\^^^^MONO%\^^^^EO%\^^^^BASO%\^^^^NEUT#\^^^^LYMPH#\^^^^MONO#\^^^^EO#\^^^^BASO#\^^^^RDW-SD\^^^^RDW-CV\^^^^MPV|||||||N||||||||||||||F
//C|1
//R|1|^^^^WBC^1|5.42|10*3/uL||N||||||20120507122907
//R|2|^^^^RBC^1|4.51|10*6/uL||N||||||20120507122907
//R|3|^^^^HGB^1|14.3|g/dL||N||||||20120507122907
//R|4|^^^^HCT^1|43.0|%||N||||||20120507122907
//R|5|^^^^MCV^1|95.3|fL||N||||||20120507122907
//R|6|^^^^MCH^1|31.7|pg||N||||||20120507122907
//R|7|^^^^MCHC^1|33.3|g/dL||N||||||20120507122907
//R|8|^^^^PLT^1|198|10*3/uL||N||||||20120507122907
//R|9|^^^^NEUT%^1|61.4|%||N||||||20120507122907
//R|10|^^^^LYMPH%^1|27.1|%||N||||||20120507122907
//R|11|^^^^MONO%^1|10.0|%||N||||||20120507122907
//R|12|^^^^EO%^1|1.1|%||N||||||20120507122907
//R|13|^^^^BASO%^1|0.4|%||N||||||20120507122907
//R|14|^^^^NEUT#^1|3.33|10*3/uL||N||||||20120507122907
//R|15|^^^^LYMPH#^1|1.47|10*3/uL||N||||||20120507122907
//R|16|^^^^MONO#^1|0.54|10*3/uL||N||||||20120507122907
//R|17|^^^^EO#^1|0.06|10*3/uL||N||||||20120507122907
//R|18|^^^^BASO#^1|0.02|10*3/uL||N||||||20120507122907
//R|19|^^^^RDW-SD^1|44.3|fL||N||||||20120507122907
//R|20|^^^^RDW-CV^1|13.2|%||N||||||20120507122907
//R|21|^^^^MPV^1|11.8|fL||N||||||20120507122907
//R|22|^^^^Blasts?|0|||||||||20120507122907
//R|23|^^^^Immature_Gran?|20|||||||||20120507122907
//R|24|^^^^Left_Shift?|0|||||||||20120507122907
//R|25|^^^^NRBC?|0|||||||||20120507122907
//R|26|^^^^Atypical_Lympho?|10|||||||||20120507122907
//R|27|^^^^Abn_Lympho/Blasts?|0|||||||||20120507122907
//R|28|^^^^RBC_Agglutination?|70|||||||||20120507122907
//R|29|^^^^Turbidity/HGB_Interference?|90|||||||||20120507122907
//R|30|^^^^Iron_Deficiency?|70|||||||||20120507122907
//R|31|^^^^HGB_Defect?|70|||||||||20120507122907
//R|32|^^^^Fragments?|0|||||||||20120507122907
//R|33|^^^^PLT_Clumps?|20|||||||||20120507122907
//R|34|^^^^PLT_Clumps(S)?|60|||||||||20120507122907
//R|35|^^^^SCAT_DIFF|PNG&R&20120507&R&2012_05_07_12_29_D770_DIFF.PNG|||N||||||20120507122907
//R|36|^^^^DIST_RBC|PNG&R&20120507&R&2012_05_07_12_29_D770_RBC.PNG|||N||||||20120507122907
//R|37|^^^^DIST_PLT|PNG&R&20120507&R&2012_05_07_12_29_D770_PLT.PNG|||N||||||20120507122907
//L|1|N
//";
//// mensagito = @"H|\^&|||XS^00-16^65750^^^^05342311||||||||E1394-97
//P|1
//C|1
//O|1||     2^4^           a844^M|^^^^WBC\^^^^RBC\^^^^HGB\^^^^HCT\^^^^MCV\^^^^MCH\^^^^MCHC\^^^^PLT\^^^^NEUT%\^^^^LYMPH%\^^^^MONO%\^^^^EO%\^^^^BASO%\^^^^NEUT#\^^^^LYMPH#\^^^^MONO#\^^^^EO#\^^^^BASO#\^^^^RDW-SD\^^^^RDW-CV\^^^^MPV|||||||N||||||||||||||F
//C|1
//R|1|^^^^WBC^1|11.54|10*3/uL||N||||||20120416095505
//R|2|^^^^RBC^1|5.38|10*6/uL||N||||||20120416095505
//R|3|^^^^HGB^1|15.8|g/dL||N||||||20120416095505
//R|4|^^^^HCT^1|45.9|%||N||||||20120416095505
//R|5|^^^^MCV^1|85.3|fL||L||||||20120416095505
//R|6|^^^^MCH^1|29.4|pg||N||||||20120416095505
//R|7|^^^^MCHC^1|34.4|g/dL||N||||||20120416095505
//R|8|^^^^PLT^1|200|10*3/uL||N||||||20120416095505
//R|9|^^^^NEUT%^1|59.5|%||N||||||20120416095505
//R|10|^^^^LYMPH%^1|31.5|%||N||||||20120416095505
//R|11|^^^^MONO%^1|7.4|%||N||||||20120416095505
//R|12|^^^^EO%^1|1.3|%||N||||||20120416095505
//R|13|^^^^BASO%^1|0.3|%||N||||||20120416095505
//R|14|^^^^NEUT#^1|6.87|10*3/uL||N||||||20120416095505
//R|15|^^^^LYMPH#^1|3.64|10*3/uL||N||||||20120416095505
//R|16|^^^^MONO#^1|0.85|10*3/uL||H||||||20120416095505
//R|17|^^^^EO#^1|0.15|10*3/uL||N||||||20120416095505
//R|18|^^^^BASO#^1|0.03|10*3/uL||N||||||20120416095505
//R|19|^^^^RDW-SD^1|38.4|fL||N||||||20120416095505
//R|20|^^^^RDW-CV^1|12.4|%||N||||||20120416095505
//R|21|^^^^MPV^1|10.8|fL||N||||||20120416095505
//R|22|^^^^Blasts?|0|||||||||20120416095505
//R|23|^^^^Immature_Gran?|20|||||||||20120416095505
//R|24|^^^^Left_Shift?|0|||||||||20120416095505
//R|25|^^^^NRBC?|10|||||||||20120416095505
//R|26|^^^^Atypical_Lympho?|10|||||||||20120416095505
//R|27|^^^^Abn_Lympho/Blasts?|20|||||||||20120416095505
//R|28|^^^^RBC_Agglutination?|60|||||||||20120416095505
//R|29|^^^^Turbidity/HGB_Interference?|90|||||||||20120416095505
//R|30|^^^^Iron_Deficiency?|80|||||||||20120416095505
//R|31|^^^^HGB_Defect?|80|||||||||20120416095505
//R|32|^^^^Fragments?|0|||||||||20120416095505
//R|33|^^^^PLT_Clumps?|10|||||||||20120416095505
//R|34|^^^^PLT_Clumps(S)?|80|||||||||20120416095505
//R|35|^^^^SCAT_DIFF|PNG&R&20120413&R&2012_04_16_09_55_a844_DIFF.PNG|||N||||||20120416095505
//R|36|^^^^DIST_RBC|PNG&R&20120413&R&2012_04_16_09_55_a844_RBC.PNG|||N||||||20120416095505
//R|37|^^^^DIST_PLT|PNG&R&20120413&R&2012_04_16_09_55_a844_PLT.PNG|||N||||||20120416095505
//L|1|N
//";

//            mensagito = @"MSH|^~\&|Company|ChemistryAnalyzer|||20111220084411||ORU^R01|1|P|2.3.1|
//PID|1||||||||||||||||||||||||||||||
//OBR|1|BADILLA .|1|Mindray^BS-300||13932|20111220||||||||Suero|||||||||||||||||||||||||||||||||
//OBX|1|NM|2|Urea|-100000000,00|g/l|||||||0,25|20111220||||
//OBX|2|NM|1|Glucosa|-100000000,00|g/l|||||||0,75|20111220||||
//OBX|3|NM|3|Creatinina|-100000000,00|mg/dl|||||||0,75|20111220||||
//";


//            mensagito = @"MSH|^~\&|Company|ChemistryAnalyzer|||20120105174140||ORU^R01|40|P|2.3.1|
//PID|40||||||||||||||||||||||||||||||
//OBR|1|a10844|17|Mindray^BS-300||93913|20120105||||||||Suero|||||||||||||||||||||||||||||||||
//OBX|1|NM|9|GOT|-100000000|UI/L|||||||15|20120105||||
//OBX|2|NM|10|GPT|-100000000|UI/L|||||||19|20120105||||
//OBX|3|NM|2|Urea|-100000000,00|g/l|||||||0,42|20120105||||
//OBX|4|NM|11|FAL|-100000000|UI/L|||||||206|20120105||||
//OBX|5|NM|4|Urico|-100000000,0|mg/l|||||||33,9|20120105||||
//OBX|6|NM|5|Colesterol|-100000000,00|g/dl|||||||2,19|20120105||||
//OBX|7|NM|1|Glucosa|-100000000,00|g/l|||||||0,81|20120105||||
//OBX|8|NM|6|Trigliceridos|-100000000,00|g/l|||||||1,30|20120105||||
//OBX|9|NM|3|Creatinina|-100000000,00|mg/dl|||||||0,86|20120105||||
//OBX|10|NM|7|HDL|-100000000,00|g/l|||||||0,37|20120105||||
//";


//mensagito = @"MSH|^~\&|Company|ChemistryAnalyzer|||20120412104409||ORU^R01|519|P|2.3.1|
//
//PID|519||||BULNES VICENTE||1944||||||||||||||||||||||||
//
//OBR|1|A14669-S|15|Mindray^BS-300||94102|20120412||||||||Suero|||||||||||||||||||||||||||||||||
//
//OBX|1|NM|40001|Na+|135,3|mmol/L|134,0-149,0|N|||||-99999999,0|20120412||||
//
//OBX|2|NM|40002|K+|3,62|mmol/L|3,60-5,50|N|||||-99999999,00|20120412||||
//
//OBX|3|NM|5|Colesterol|1,79||0,00-2,00|N|||||1,79|20120412||||
//
//OBX|4|NM|6|Trigliceridos|1,11||0,00-1,50|N|||||1,11|20120412||||
//
//OBX|5|NM|1|Glucosa|0,96||0,50-1,10|N|||||0,96|20120412||||
//
//OBX|6|NM|2|Urea|0,31||0,10-0,50|N|||||0,31|20120412||||
//
//OBX|7|NM|3|Creatinina|0,93||0,70-1,30|N|||||0,93|20120412||||
//";

//  mensagito = @"H|\^&|||XS^00-16^65750^^^^05342311||||||||E1394-97
//P|1
//C|1
//O|1||     2^5^         B517^M|^^^^WBC\^^^^RBC\^^^^HGB\^^^^HCT\^^^^MCV\^^^^MCH\^^^^MCHC\^^^^PLT\^^^^NEUT%\^^^^LYMPH%\^^^^MONO%\^^^^EO%\^^^^BASO%\^^^^NEUT#\^^^^LYMPH#\^^^^MONO#\^^^^EO#\^^^^BASO#\^^^^RDW-SD\^^^^RDW-CV\^^^^MPV|||||||N||||||||||||||F
//C|1
//R|1|^^^^WBC^1|4.91|10*3/uL||N||||||20120423105139
//R|2|^^^^RBC^1|4.96|10*6/uL||N||||||20120423105139
//R|3|^^^^HGB^1|12.4|g/dL||N||||||20120423105139
//R|4|^^^^HCT^1|37.7|%||N||||||20120423105139
//R|5|^^^^MCV^1|76.0|fL||L||||||20120423105139
//R|6|^^^^MCH^1|25.0|pg||L||||||20120423105139
//R|7|^^^^MCHC^1|32.9|g/dL||N||||||20120423105139
//R|8|^^^^PLT^1|295|10*3/uL||N||||||20120423105139
//R|9|^^^^NEUT%^1|37.7|%||W||||||20120423105139
//R|10|^^^^LYMPH%^1|49.1|%||W||||||20120423105139
//R|11|^^^^MONO%^1|10.8|%||W||||||20120423105139
//R|12|^^^^EO%^1|1.8|%||N||||||20120423105139
//R|13|^^^^BASO%^1|0.6|%||N||||||20120423105139
//R|14|^^^^NEUT#^1|1.85|10*3/uL||W||||||20120423105139
//R|15|^^^^LYMPH#^1|2.41|10*3/uL||W||||||20120423105139
//R|16|^^^^MONO#^1|0.53|10*3/uL||W||||||20120423105139
//R|17|^^^^EO#^1|0.09|10*3/uL||N||||||20120423105139
//R|18|^^^^BASO#^1|0.03|10*3/uL||N||||||20120423105139
//R|19|^^^^RDW-SD^1|36.6|fL||L||||||20120423105139
//R|20|^^^^RDW-CV^1|13.7|%||N||||||20120423105139
//R|21|^^^^MPV^1|10.2|fL||N||||||20120423105139
//R|22|^^^^Blasts?|150|||A||||||20120423105139
//R|23|^^^^Immature_Gran?|20|||||||||20120423105139
//R|24|^^^^Left_Shift?|10|||||||||20120423105139
//R|25|^^^^NRBC?|10|||||||||20120423105139
//R|26|^^^^RBC_Agglutination?|60|||||||||20120423105139
//R|27|^^^^Turbidity/HGB_Interference?|90|||||||||20120423105139
//R|28|^^^^Iron_Deficiency?|90|||||||||20120423105139
//R|29|^^^^HGB_Defect?|90|||||||||20120423105139
//R|30|^^^^Fragments?|0|||||||||20120423105139
//R|31|^^^^PLT_Clumps?|20|||||||||20120423105139
//R|32|^^^^PLT_Clumps(S)?|50|||||||||20120423105139
//R|33|^^^^Positive_Morph||||A||||||20120423105139
//R|34|^^^^SCAT_DIFF|PNG&R&20120420&R&2012_04_23_10_51_a15165_DIFF.PNG|||N||||||20120423105139
//R|35|^^^^DIST_RBC|PNG&R&20120420&R&2012_04_23_10_51_a15165_RBC.PNG|||N||||||20120423105139
//R|36|^^^^DIST_PLT|PNG&R&20120420&R&2012_04_23_10_51_a15165_PLT.PNG|||N||||||20120423105139
//L|1|N";



