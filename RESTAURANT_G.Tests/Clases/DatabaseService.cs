using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTAURANT_G.Tests.Clases
{
   public class DatabaseService : IDatabaseService
    {
        private readonly string connectionString;

        public DatabaseService()
        {
            connectionString = ConfigurationManager.ConnectionStrings["TuConexionBD"].ConnectionString;
        }

        public int CambiarContraseña(string correo, string nuevaContraseña)
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_CambiarContraseña", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Correo", correo);
                        cmd.Parameters.AddWithValue("@NuevaContraseña", nuevaContraseña);

                        SqlParameter returnParameter = new SqlParameter("@ReturnVal", SqlDbType.Int);
                        returnParameter.Direction = ParameterDirection.ReturnValue;
                        cmd.Parameters.Add(returnParameter);

                        cmd.ExecuteNonQuery();
                        return (int)returnParameter.Value; // Retorna 1 si se actualizó correctamente, 0 si el correo no existe.
                    }
                }
            }
            catch
            {
                return -1; // Código de error en la BD
            }
        }

        public string ObtenerContraseñaHash(string correo)
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Contraseña FROM Clientes WHERE Correo = @Correo", conexion))
                    {
                        cmd.Parameters.AddWithValue("@Correo", correo);
                        var result = cmd.ExecuteScalar();

                        return result?.ToString(); // Retorna la contraseña hash o null si no existe el usuario
                    }
                }
            }
            catch
            {
                return null; // Manejo básico de error
            }
        }

    }
}