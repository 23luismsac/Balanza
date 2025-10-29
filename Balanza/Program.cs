using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Balanza
{
	class Program
	{
		private static DataTable dtParametros = null;
		private static SerialPort Balanza;
		private static Int32 BITES;
		//Private Balanza As New System.IO.Ports.SerialPort
		private static string CaracterLectura;
		private static bool ConexionBalanza = false;
		private static int NroBalanza = 0;
		static HttpClient client = new HttpClient();


		static void Main(string[] args)
		{

            //int prueba = 0;
            //if (prueba == 0)
            //{
            //    EnviarDatoOdoo(6, "BLZ TRC TRUJILLO", Convert.ToDecimal(222), DateTime.Today);


            //}


            Console.Write("INICIANDO EL SISTEMA \r\n");
			Console.Write("Obteniendo el número de balanza \r\n");
			IPHostEntry host;
			host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily.ToString() == "InterNetwork")
				{
					NroBalanza = Datos.instancia.fnObtenerBalanza(ip.ToString(), Dns.GetHostName());
					if (NroBalanza > 0)
					{
						break;
					}
				}
			}

			if (NroBalanza <= 0)
			{
				Console.WriteLine("Este IP no esta configurado para la balanza.");
				Console.ReadLine();
				return;
			}

			Console.WriteLine("Nro de Balanza Obtenida: " + NroBalanza.ToString());
			CargarParametros();



			Balanza = new SerialPort(); // New System.IO.Ports.SerialPort;
			Balanza.Dispose();
			Balanza.Close();

			do
			{

				if (ConexionBalanza == false)
				{
					AbrirBalanza();
				}

				if (ConexionBalanza == true)
				{
					//Console.WriteLine("Ingreso a pesar: \r\n");
					if (Balanza.IsOpen)
					{
						ConectarBalaza();
					}
					else
					{
						ConexionBalanza = false;
					}
				}

				Thread.Sleep(1000);
			} while (true);
			Balanza.Dispose();
			Balanza.Close();

		}
		private static void CargarParametros() {
			try
			{
				dtParametros = ParametrosBalanza(NroBalanza);
				do
				{
					if (dtParametros == null)
					{
						Console.WriteLine("No existe parametros para esta balanza seleccionada.");
						Console.ReadLine();
						return;
					}
					else
					{
						Console.WriteLine("Ingreso la balanza " + dtParametros.Rows[0]["Descripcion"].ToString() + " Fecha Hora de inicio: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " Puerto: COM" + dtParametros.Rows[0]["Puerto"].ToString() + "\r\n");
						break;
					}
				} while (true);
			}
			catch (Exception ex)
			{
				dtParametros = null;
				Console.WriteLine("Error al obtener parametros de la balanza.");
			}
		}
		private static bool AbrirBalanza() {
			try
			{
				if (Balanza.IsOpen == false)
				{
					try
					{


						Balanza.PortName = "COM" + dtParametros.Rows[0]["Puerto"].ToString();
						Balanza.BaudRate = Convert.ToInt32(dtParametros.Rows[0]["Velocidad"].ToString());

						Int32 Paridad = Convert.ToInt32(dtParametros.Rows[0]["Paridad"].ToString());

						switch (Paridad)
						{
							case 0:
								Balanza.Parity = System.IO.Ports.Parity.None;
								break;
							case 1:
								Balanza.Parity = System.IO.Ports.Parity.Even;
								break;
							case 2:
								Balanza.Parity = System.IO.Ports.Parity.Odd;
								break;
							case 3:
								Balanza.Parity = System.IO.Ports.Parity.Mark;
								break;
							case 4:
								Balanza.Parity = System.IO.Ports.Parity.Space;
								break;
							default:
								break;
						}

						Balanza.DataBits = Convert.ToInt32(dtParametros.Rows[0]["LongDatos"].ToString());
						BITES = Convert.ToInt32(dtParametros.Rows[0]["LongDatos"].ToString());
						CaracterLectura = dtParametros.Rows[0]["Caracter"].ToString();
						Balanza.StopBits = StopBits.One; // Convert.ToInt32(dtParametros.Rows[0]["BitParada"].ToString());
						Balanza.RtsEnable = true;
						Balanza.Encoding = System.Text.Encoding.Default;
						Balanza.DiscardNull = true;

						//Console.WriteLine("Lista cargada con parametros de la base de datos");
						//Console.WriteLine("Abriendo Puerto de balanza");



						Balanza.Open();
						Console.WriteLine("Conexión con la balanza establecida \r\n");
						ConexionBalanza = true;
						return true;

					}
					catch (Exception ex)
					{
						Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - Error en conexion con balanza Mensaje: " + ex.Message + "\r\nVolviendo a cargar los parametros.");
						Console.WriteLine("\r\n");

						Balanza.Dispose();
						Balanza.Close();
						ConexionBalanza = false;
						string[] puertosDisponibles = SerialPort.GetPortNames();
						bool BanderaPuertoExiste = false;

						foreach (var item in puertosDisponibles)
						{
							if (item == "COM" + dtParametros.Rows[0]["Puerto"].ToString()) {
								BanderaPuertoExiste = true;
							}
						}
						if (BanderaPuertoExiste == false) {
							//Console.WriteLine("Estar");
							CargarParametros();
						}
						return false;
					}
				}
				else
				{
					Console.WriteLine("------------------------------------------------- ");
					Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - La Balanza ya esta Conectada en otro proceso");
					Balanza.Dispose();
					Balanza.Close();
					ConexionBalanza = false;
					return false;
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine("------------------------------------------------- ");
				Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - Error al conectar con la balanza.");
				Console.WriteLine("Tipo Error. " + ex.Message);
				Balanza.Dispose();
				Balanza.Close();
				ConexionBalanza = false;
				return false;
			}
		}

		public static DataTable ParametrosBalanza(int NroBalanza) {
			DataTable dt = Datos.instancia.ParametrosBalanza(NroBalanza);
			return dt;
		}

		public static void ConectarBalaza() {
			try
			{
				string PesoEntrada_Valido = "";
				decimal datos = 0;

				//Console.WriteLine("Puerto abierto de balanza");
				//Console.WriteLine("PesoEntrada_Valido:" + PesoEntrada_Valido.Trim().Length);

				//while (PesoEntrada_Valido.Trim().Length == 0) { 
				//PesoEntrada_Valido = "0i0       90     00";
				if (Balanza.IsOpen)
				{
					//Console.WriteLine("Enviando petision a la balanza");
					Balanza.Write("W");
					Balanza.WriteLine("W");
					Balanza.Write("\r\n");
					//Console.WriteLine("Peticion enviada");

					//Console.WriteLine("leer Balanza");
					PesoEntrada_Valido = Balanza.ReadExisting();
					//Console.WriteLine("Datos de lectura de balanza Balanza.ReadExisting(): " + PesoEntrada_Valido.ToString());

				}
				else {
					ConexionBalanza = false;
					Console.WriteLine("\r\n No hay conexion con la balanza.  \r\n");
					Balanza.Dispose();
					Balanza.Close();
					goto fin;
				}
				StreamWriter miEscritura = File.AppendText("ejemplo.txt");
				miEscritura.WriteLine("Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - Peso:" + PesoEntrada_Valido);
				miEscritura.Close();

				if (PesoEntrada_Valido.Trim().Length == 0) {

					Console.WriteLine("\r\n Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - Error No hay datos para leer peso de ingreso vacio.  \r\n");
					string[] puertosDisponibles = SerialPort.GetPortNames();
					bool BanderaPuertoExiste = false;
					foreach (var item in puertosDisponibles)
					{
						if (item == "COM" + dtParametros.Rows[0]["Puerto"].ToString())
						{
							BanderaPuertoExiste = true;
						}
					}
					if (BanderaPuertoExiste == false)
					{
						ConexionBalanza = false;
					}
					goto fin;
				}

				int ReadBufferSize;

				//Console.WriteLine("Cargar el Buffer");

				ReadBufferSize = Balanza.ReadBufferSize;
				//Console.WriteLine("Buffer Cargado: " +ReadBufferSize ); 
				PesoEntrada_Valido = PesoEntrada_Valido.Substring(0, 13);

				//Console.WriteLine("PesoEntrada Valido: " + PesoEntrada_Valido);

				if (Convert.ToInt32(PesoEntrada_Valido.LastIndexOf(CaracterLectura) + 1) == 0) {
					Console.WriteLine("\r\n Error en peso de entrdada  \r\n");
					goto fin;
				}

				string cadena = PesoEntrada_Valido;
				char aBuscar = Convert.ToChar(CaracterLectura);
				//Console.WriteLine("Candena de entrada: " + cadena);
				//Console.WriteLine("a Buscar: " + aBuscar);

				//Console.WriteLine("Ingreso al for para buscar: " + aBuscar);
				int n = 0;
				foreach (var item in cadena)
				{
					if (item == aBuscar) {
						n += 1;
					}
				}
				//Console.WriteLine("Encontrados - abuscar: " + n.ToString());
				if (n > 1) {
					Console.WriteLine("\r\n Error. Caracter de lectura tiene mas de 1:  " + n + "\r\n");
					goto fin;
				}

				int F;
				long Contador = 0;

				for (F = 0; F < PesoEntrada_Valido.Length; F++)
				{
					if (PesoEntrada_Valido.Substring(F, 1) == CaracterLectura) {
						Contador += 1;
					}
				}
				//Console.WriteLine("resultado segundo for: " + Contador.ToString());

				if (Contador > 1) {
					Console.WriteLine("\r\n Error. Caracter de lectura tiene mas de 1:  " + Contador + "\r\n");
					goto fin;
				}
				string Cadena1Filtro;
				//int CantidadCeros = 2;
				Cadena1Filtro = PesoEntrada_Valido.Substring(PesoEntrada_Valido.LastIndexOf(CaracterLectura, PesoEntrada_Valido.Length - 1) + 1);

				//Console.WriteLine("Obtener cadena filtro: " + Cadena1Filtro);

				int longitud = Cadena1Filtro.Length;
				int ddd = Cadena1Filtro.LastIndexOf(" ");
				PesoEntrada_Valido = Cadena1Filtro.Substring(1, Cadena1Filtro.Length - 1);
				//Console.WriteLine("resultado de longitud y peso: " + PesoEntrada_Valido.ToString());
				PesoEntrada_Valido = PesoEntrada_Valido.Replace(" ", "");

				//Console.WriteLine("limpiando peso: " + PesoEntrada_Valido.ToString());


				//Console.WriteLine("Convertir a decimales la lectura");
				datos = Math.Round(Convert.ToDecimal(PesoEntrada_Valido) / 1000, 4);
				//Console.WriteLine("Datos de lectura: " + datos.ToString());

				Console.WriteLine("--------------------------------------------------------------");
				Console.WriteLine("Balanza: " + dtParametros.Rows[0]["Descripcion"].ToString() + " - Fecha Hora: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - Peso: " + datos.ToString());
				Console.WriteLine("--------------------------------------------------------------");
				Console.WriteLine("Enivando a data a ODO");
				string resp= EnviarDatoOdoo(Convert.ToInt32(dtParametros.Rows[0]["NroBalanza"].ToString()), dtParametros.Rows[0]["Descripcion"].ToString(), datos, DateTime.Now);
				
				if (resp == string.Empty)
				{
					Console.WriteLine("No devolvio ningun resultado ODO.");
				}
				else {
					Console.WriteLine("Respuesta de ODOO: " + resp.ToString() );
				}
				fin:;
				//Thread.Sleep(1000);
				//}

			}
			catch (Exception ex)
			{
				Console.WriteLine("Error en lectura de balanza: " + ex.Message);

			}
		}

		//public static string Base64Decode()
		//{
		//	string ruta = Directory.GetCurrentDirectory() + @"\BLZ_Config.sys";
		//	StreamReader sr = new StreamReader(ruta);
		//	string line = sr.ReadLine();
		//	sr.Close();
		//	var base64EncodedBytes = System.Convert.FromBase64String(line);
		//	return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
		//}
		
		private static string EnviarDatoOdoo(Int32 NroBalanza, String DescripcionBalanza, decimal Peso,DateTime FechaRegistro) {
			//var url = $"https://23luismsac-grupotrc-atcsac2-10264358.dev.odoo.com/jsonrpc/";
			// var url = $"https://23luismsac-grupotrc-atcsac4-14467625.dev.odoo.com/jsonrpc/";
			var url = $"https://grupotrc-25-10-25-test-24923397.dev.odoo.com/jsonrpc/";
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			var request = (HttpWebRequest)WebRequest.Create(url);

			string json = "{\"jsonrpc\":\"2.0\"," +
				"\"method\":\"call\"," +
				"\"params\":{\"service\":\"object\"," +
							"\"method\":\"execute\"," +
							//"\"args\":[\"23luismsac-grupotrc-atcsac2-10264358\",6,\"54f718cdff3d52665ff1da12b27b8e730f883649\",\"trc.atsa.weight\",\"create\"," +
							"\"args\":[\"grupotrc-25-10-25-test-24923397\",2,\"ab38371e563b554d6f2858bf55243e9bb07f51c3\",\"trc.atsa.weight\",\"create\"," +
											"{\"scale_number\":\"" + NroBalanza +"\"," +
											"\"scale_description\":\""+ DescripcionBalanza +"\"," +
											"\"weight\":\""+ Peso.ToString() +"\"," +
											"\"weight_date\":\""+ FechaRegistro.ToString("yyyy-MM-dd HH:mm:ss") +"\"}]}," +
				"\"id\":1}";
			request.Method = "POST";
			request.ContentType = "application/json";
			request.Accept = "application/json";
			
			using (var streamWriter = new StreamWriter(request.GetRequestStream()))
			{
				streamWriter.Write(json);
				streamWriter.Flush();
				streamWriter.Close();
			}

			try
			{
				using (WebResponse response = request.GetResponse())
				{
					using (Stream strReader = response.GetResponseStream())
					{
						if (strReader == null) return "";
						using (StreamReader objReader = new StreamReader(strReader))
						{
							string responseBody = objReader.ReadToEnd();
							// Do something with responseBody
							//Console.WriteLine(responseBody);
							Console.WriteLine("Fin envio: " + DateTime.Now);
							return responseBody;

						}
					}
				}
			}
			catch (WebException ex)
			{
				// Handle error
				Console.WriteLine("Error al enviar datos a ODO. Tipo de Error: " + ex.Message + "  enlace utilizando al enlace: " + url);
				return "";
			}
			
		}
		

	}

}

