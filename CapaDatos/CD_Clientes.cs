using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace capaDatos
{
    public class CD_Clientes
    {
        private readonly CD_conexionBD conexion = new CD_conexionBD();

        // Método para obtener datos del cliente por su ID
        public Dictionary<string, object> ObtenerClientePorId(int clienteId)
        {
            var cliente = new Dictionary<string, object>();
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();

                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM Clientes WHERE Id = @ClienteId";
                    cmd.Parameters.AddWithValue("@ClienteId", clienteId);
                    cmd.CommandType = CommandType.Text;

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            cliente["Id"] = dr["Id"];
                            cliente["Nombre"] = dr["Nombre"];
                            cliente["Email"] = dr["Email"];
                            cliente["Telefono"] = dr["Telefono"];
                            // Añade cualquier otro campo que necesites
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener cliente: " + ex.Message);
                throw;
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return cliente;
        }
    }
}
