using System;
using System.Configuration;
using System.Data.SqlClient;

namespace capaDatos
{
    public class CD_conexionBD
    {
        private readonly string connectionString;

        public CD_conexionBD()
        {
            connectionString = ConfigurationManager.ConnectionStrings["ConexionBD"]?.ConnectionString
                               ?? throw new InvalidOperationException("Cadena de conexión no encontrada.");
        }

        public SqlConnection ObtenerConexion()
        {
            var conexion = new SqlConnection(connectionString);
            try
            {
                conexion.Open();
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al abrir la conexión con la base de datos.", ex);
            }
            return conexion;
        }

        public void CerrarConexion(SqlConnection conexion)
        {
            if (conexion != null && conexion.State == System.Data.ConnectionState.Open)
            {
                conexion.Close();
            }
        }
    }
}
