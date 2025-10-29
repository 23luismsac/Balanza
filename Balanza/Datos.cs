using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balanza
{
	
	class Datos
	{
		private string cadenaConexion = "Data Source=100.100.100.202;user id=systemusr;password=$y$T3mU$3r; database=TRC";
        //private string cadenaConexion = "Data Source=100.100.100.22;user id=sa;password=123456; database=TRC";
        private string Advertencia = "";
        public static Datos instancia = new Datos();
        //public Datos()
        //{
        //}

        public static Datos Instancia()
        {
            if (instancia == null)
            {
                instancia = new Datos();
            }
            return instancia;
        }
        public DataTable ParametrosBalanza(int NroValanza)
        {
            DataTable dt = new DataTable();
            SqlCommand cmd;
            SqlConnection con = new SqlConnection(cadenaConexion);

            try
            {
                con.Open();
                cmd = new SqlCommand("spBLZ_ObtenerParametrosBalanza", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NroBalanza", NroValanza);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                Advertencia = "Fuente: " + ex.Source + "Mensaje: " + ex.ToString();
                return null;
            }
            finally
            {
                con.Close();
                cmd = null;
            }
            return dt;
        }
        public DataTable ListaBalanza()
        {
            DataTable dt = new DataTable();
            SqlCommand cmd;
            SqlConnection con = new SqlConnection(cadenaConexion);

            try
            {
                con.Open();
                cmd = new SqlCommand("spBLZ_ObtenerListaBalanza", con);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                Advertencia = "Fuente: " + ex.Source + "Mensaje: " + ex.ToString();
                return null;
            }
            finally
            {
                con.Close();
                cmd = null;
            }
            return dt;
        }
        public Int32 fnObtenerBalanza(string DireccionIp, string HostName)
        {
            //DataTable dt = new DataTable();
            SqlCommand cmd;
            SqlConnection con = new SqlConnection(cadenaConexion);
            Int32 NroBalanza = 0;
            try
            {
                con.Open();
                cmd = new SqlCommand("spBLZ_ObtenerBalanza", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DireccionIp", DireccionIp);
                cmd.Parameters.AddWithValue("@HostName", HostName);
                //SqlDataAdapter da = new SqlDataAdapter(cmd);
                SqlDataReader Dr = cmd.ExecuteReader();
                if (Dr.Read())
                {
                    NroBalanza = Convert.ToInt32(Dr["NroBalanza"].ToString());
                }
                else
                {
                    NroBalanza = 0;
                }
            }
            catch (Exception ex)
            {
                Advertencia = "Fuente: " + ex.Source + "Mensaje: " + ex.ToString();
                return 0;
            }
            finally
            {
                con.Close();
                cmd = null;
            }
            return NroBalanza;
        }
    }
}
