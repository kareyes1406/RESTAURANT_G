using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;

namespace capaDatos
{
    public class CD_Mesas
    {
        private readonly CD_conexionBD conexion = new CD_conexionBD();

        // Método para obtener todas las mesas con estado actualizado según reservas
        // En CD_Mesas.cs, método ObtenerMesasConEstadoActualizado (asumido)
        // Método para obtener todas las mesas con estado actualizado según reservas actuales
        public List<Dictionary<string, object>> ObtenerMesasConEstadoActualizado()
        {
            List<Dictionary<string, object>> mesas = ObtenerTodasLasMesas();
            DateTime ahora = DateTime.Now;

            foreach (var mesa in mesas)
            {
                // Verificar si hay reservas activas AHORA para esta mesa
                int idMesa = Convert.ToInt32(mesa["Id"]);
                var reservaActual = ObtenerReservaActivaActualParaMesa(idMesa);

                if (reservaActual != null)
                {
                    mesa["ReservaActual"] = reservaActual;
                    mesa["Estado"] = "reservada"; // Mesa actualmente en uso
                }
                else
                {
                    // Si no hay reserva actual, buscar próxima reserva
                    var proximaReserva = ObtenerProximaReservaActivaParaMesa(idMesa);
                    if (proximaReserva != null)
                    {
                        mesa["ProximaReserva"] = proximaReserva;
                        // El estado sigue siendo "disponible" hasta que llegue la hora de la reserva
                    }
                }
            }

            return mesas;
        }

        // Método auxiliar para obtener la reserva activa en este momento para una mesa
        private Dictionary<string, object> ObtenerReservaActivaActualParaMesa(int idMesa)
        {
            SqlConnection conn = null;
            Dictionary<string, object> reserva = null;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
            SELECT TOP 1 r.Id as IdReserva, r.ClienteId, r.FechaReserva, r.HoraReserva, 
                   r.CantidadPersonas, r.Estado, c.Nombre as NombreCliente
            FROM Reservas r
            INNER JOIN Clientes c ON r.ClienteId = c.Id
            WHERE r.MesaId = @IdMesa 
            AND CONVERT(date, r.FechaReserva) = CONVERT(date, GETDATE())
            AND r.Estado IN ('Pendiente', 'Atendida')
            AND CONVERT(time, GETDATE()) >= r.HoraReserva
            AND CONVERT(time, GETDATE()) <= DATEADD(HOUR, 2, r.HoraReserva)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdMesa", idMesa);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            reserva = new Dictionary<string, object>
                            {
                                ["IdReserva"] = reader["IdReserva"],
                                ["ClienteId"] = reader["ClienteId"],
                                ["FechaReserva"] = reader["FechaReserva"],
                                ["HoraReserva"] = reader["HoraReserva"],
                                ["CantidadPersonas"] = reader["CantidadPersonas"],
                                ["Estado"] = reader["Estado"],
                                ["NombreCliente"] = reader["NombreCliente"]
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerReservaActivaActualParaMesa: " + ex.Message);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return reserva;
        }

        // Método auxiliar para obtener la próxima reserva (que no ha comenzado todavía)
        private Dictionary<string, object> ObtenerProximaReservaActivaParaMesa(int idMesa)
        {
            SqlConnection conn = null;
            Dictionary<string, object> reserva = null;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
            SELECT TOP 1 r.Id as IdReserva, r.ClienteId, r.FechaReserva, r.HoraReserva, 
                   r.CantidadPersonas, r.Estado, c.Nombre as NombreCliente
            FROM Reservas r
            INNER JOIN Clientes c ON r.ClienteId = c.Id
            WHERE r.MesaId = @IdMesa 
            AND CONVERT(date, r.FechaReserva) = CONVERT(date, GETDATE())
            AND r.Estado = 'Pendiente'
            AND r.HoraReserva > CONVERT(time, GETDATE())
            ORDER BY r.HoraReserva ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdMesa", idMesa);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            reserva = new Dictionary<string, object>
                            {
                                ["IdReserva"] = reader["IdReserva"],
                                ["ClienteId"] = reader["ClienteId"],
                                ["FechaReserva"] = reader["FechaReserva"],
                                ["HoraReserva"] = reader["HoraReserva"],
                                ["CantidadPersonas"] = reader["CantidadPersonas"],
                                ["Estado"] = reader["Estado"],
                                ["NombreCliente"] = reader["NombreCliente"]
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerProximaReservaActivaParaMesa: " + ex.Message);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return reserva;
        }

        // Verificar si hay alguna reserva para hoy no atendida
        private bool TieneReservaParaHoy(List<Dictionary<string, object>> reservas)
        {
            DateTime fechaActual = DateTime.Now.Date;

            foreach (var reserva in reservas)
            {
                DateTime fechaReserva = Convert.ToDateTime(reserva["FechaReserva"]).Date;
                bool atendido = Convert.ToBoolean(reserva["Atendido"]);

                if (fechaReserva == fechaActual && !atendido)
                {
                    return true;
                }
            }

            return false;
        }

        // Método para obtener todas las mesas de la base de datos
        private List<Dictionary<string, object>> ObtenerTodasLasMesas()
        {
            List<Dictionary<string, object>> listaMesas = new List<Dictionary<string, object>>();
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();
                if (conn == null)
                {
                    Debug.WriteLine("Error: Conexión nula");
                    return listaMesas;
                }

                string query = "SELECT Id, NumeroMesa, Capacidad, Ubicacion, Estado, DisponibleDesde FROM Mesas";
                Debug.WriteLine("Ejecutando query: " + query);

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Dictionary<string, object> mesa = new Dictionary<string, object>
                        {
                            ["Id"] = reader["Id"],
                            ["NumeroMesa"] = reader["NumeroMesa"],
                            ["Capacidad"] = reader["Capacidad"],
                            ["Ubicacion"] = reader["Ubicacion"],
                            ["Estado"] = reader["Estado"],
                            ["DisponibleDesde"] = reader["DisponibleDesde"]
                        };

                        listaMesas.Add(mesa);
                    }
                }

                Debug.WriteLine($"Se encontraron {listaMesas.Count} mesas en total");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerTodasLasMesas: " + ex.Message);
                throw new Exception("Error al obtener las mesas: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return listaMesas;
        }

        // Método para obtener todas las reservas activas agrupadas por mesa
        private Dictionary<int, List<Dictionary<string, object>>> ObtenerReservasActivasPorMesa()
        {
            Dictionary<int, List<Dictionary<string, object>>> reservasPorMesa = new Dictionary<int, List<Dictionary<string, object>>>();
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
                    SELECT r.Id, r.ClienteId, r.MesaId, r.FechaReserva, r.HoraReserva, 
                           r.CantidadPersonas, r.Estado, c.Nombre as NombreCliente
                    FROM Reservas r
                    INNER JOIN Clientes c ON r.ClienteId = c.Id
                    WHERE r.FechaReserva >= @FechaActual
                    ORDER BY r.MesaId, r.FechaReserva, r.HoraReserva";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FechaActual", DateTime.Now.Date);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int idMesa = Convert.ToInt32(reader["MesaId"]);

                            Dictionary<string, object> reserva = new Dictionary<string, object>
                            {
                                ["Id"] = reader["Id"],
                                ["ClienteId"] = reader["ClienteId"],
                                ["MesaId"] = idMesa,
                                ["FechaReserva"] = reader["FechaReserva"],
                                ["HoraReserva"] = reader["HoraReserva"],
                                ["CantidadPersonas"] = reader["CantidadPersonas"],
                                ["Estado"] = reader["Estado"],
                                ["NombreCliente"] = reader["NombreCliente"],
                                ["Atendido"] = reader["Estado"].ToString().Equals("Atendida", StringComparison.OrdinalIgnoreCase)
                            };

                            if (!reservasPorMesa.ContainsKey(idMesa))
                            {
                                reservasPorMesa[idMesa] = new List<Dictionary<string, object>>();
                            }

                            reservasPorMesa[idMesa].Add(reserva);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerReservasActivasPorMesa: " + ex.Message);
                throw new Exception("Error al obtener las reservas: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return reservasPorMesa;
        }

        // Método para cambiar el estado de una mesa
        public bool CambiarEstadoMesa(int idMesa, string nuevoEstado)
        {
            SqlConnection conn = null;
            bool resultado = false;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = "UPDATE Mesas SET Estado = @NuevoEstado WHERE Id = @IdMesa";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NuevoEstado", nuevoEstado);
                    cmd.Parameters.AddWithValue("@IdMesa", idMesa);

                    int filasAfectadas = cmd.ExecuteNonQuery();
                    resultado = filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en CambiarEstadoMesa: " + ex.Message);
                throw new Exception("Error al cambiar el estado de la mesa: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return resultado;
        }

        // Método para obtener una mesa por su ID
        public Dictionary<string, object> ObtenerMesaPorId(int idMesa)
        {
            Dictionary<string, object> mesa = null;
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = "SELECT Id, NumeroMesa, Capacidad, Ubicacion, Estado, DisponibleDesde FROM Mesas WHERE Id = @IdMesa";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdMesa", idMesa);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            mesa = new Dictionary<string, object>
                            {
                                ["Id"] = reader["Id"],
                                ["NumeroMesa"] = reader["NumeroMesa"],
                                ["Capacidad"] = reader["Capacidad"],
                                ["Ubicacion"] = reader["Ubicacion"],
                                ["Estado"] = reader["Estado"],
                                ["DisponibleDesde"] = reader["DisponibleDesde"]
                            };
                        }
                    }
                }

                // Si encontramos la mesa, buscar sus reservas para hoy
                if (mesa != null)
                {
                    CD_Reservas reservasData = new CD_Reservas();
                    var reservasHoy = reservasData.ObtenerReservasPorFecha(DateTime.Now.Date);

                    if (reservasHoy.ContainsKey(idMesa) && reservasHoy[idMesa].Count > 0)
                    {
                        // Buscar la primera reserva no atendida
                        foreach (var reserva in reservasHoy[idMesa])
                        {
                            bool atendido = Convert.ToBoolean(reserva["Atendido"]);
                            if (!atendido)
                            {
                                mesa["ProximaReserva"] = new Dictionary<string, object>
                                {
                                    ["IdReserva"] = reserva["Id"],
                                    ["ClienteId"] = reserva["ClienteId"],
                                    ["NombreCliente"] = reserva["NombreCliente"],
                                    ["FechaReserva"] = reserva["FechaReserva"],
                                    ["HoraReserva"] = reserva["HoraReserva"],
                                    ["CantidadPersonas"] = reserva["CantidadPersonas"],
                                    ["Estado"] = reserva["Estado"]
                                };
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerMesaPorId: " + ex.Message);
                throw new Exception("Error al obtener la mesa: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return mesa;
        }

        // Método para guardar una nueva mesa
        public bool GuardarMesa(int numero, int capacidad, string ubicacion, string estado, DateTime disponibleDesde)
        {
            SqlConnection conn = null;
            bool resultado = false;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
                    INSERT INTO Mesas (NumeroMesa, Capacidad, Ubicacion, Estado, DisponibleDesde) 
                    VALUES (@NumeroMesa, @Capacidad, @Ubicacion, @Estado, @DisponibleDesde)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NumeroMesa", numero);
                    cmd.Parameters.AddWithValue("@Capacidad", capacidad);
                    cmd.Parameters.AddWithValue("@Ubicacion", ubicacion);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.Parameters.AddWithValue("@DisponibleDesde", disponibleDesde);

                    int filasAfectadas = cmd.ExecuteNonQuery();
                    resultado = filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en GuardarMesa: " + ex.Message);
                throw new Exception("Error al guardar la mesa: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return resultado;
        }

        // Método para actualizar una mesa existente
        public bool ActualizarMesa(int id, int numero, int capacidad, string ubicacion, string estado, DateTime disponibleDesde)
        {
            SqlConnection conn = null;
            bool resultado = false;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
                    UPDATE Mesas 
                    SET NumeroMesa = @NumeroMesa, 
                        Capacidad = @Capacidad, 
                        Ubicacion = @Ubicacion, 
                        Estado = @Estado, 
                        DisponibleDesde = @DisponibleDesde 
                    WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@NumeroMesa", numero);
                    cmd.Parameters.AddWithValue("@Capacidad", capacidad);
                    cmd.Parameters.AddWithValue("@Ubicacion", ubicacion);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.Parameters.AddWithValue("@DisponibleDesde", disponibleDesde);

                    int filasAfectadas = cmd.ExecuteNonQuery();
                    resultado = filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ActualizarMesa: " + ex.Message);
                throw new Exception("Error al actualizar la mesa: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return resultado;
        }

        // Método para verificar si ya existe una mesa con el mismo número
        public bool ExisteMesaConNumero(int numeroMesa)
        {
            SqlConnection conn = null;
            bool existe = false;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = "SELECT COUNT(1) FROM Mesas WHERE NumeroMesa = @NumeroMesa";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NumeroMesa", numeroMesa);

                    int cantidad = Convert.ToInt32(cmd.ExecuteScalar());
                    existe = cantidad > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ExisteMesaConNumero: " + ex.Message);
                throw new Exception("Error al verificar la existencia de la mesa: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return existe;
        }

        // Método para eliminar una mesa
        public bool EliminarMesa(int idMesa)
        {
            SqlConnection conn = null;
            bool resultado = false;

            try
            {
                conn = conexion.ObtenerConexion();

                // Primero verificar si la mesa tiene reservas asociadas
                string queryVerificar = @"
                    SELECT COUNT(1) FROM Reservas WHERE MesaId = @IdMesa AND Estado != 'Cancelada'";

                using (SqlCommand cmdVerificar = new SqlCommand(queryVerificar, conn))
                {
                    cmdVerificar.Parameters.AddWithValue("@IdMesa", idMesa);
                    int reservasActivas = Convert.ToInt32(cmdVerificar.ExecuteScalar());

                    if (reservasActivas > 0)
                    {
                        Debug.WriteLine("No se puede eliminar la mesa porque tiene reservas activas asociadas");
                        return false;
                    }
                }

                // Si no hay reservas activas, eliminar la mesa
                string queryEliminar = "DELETE FROM Mesas WHERE Id = @IdMesa";

                using (SqlCommand cmdEliminar = new SqlCommand(queryEliminar, conn))
                {
                    cmdEliminar.Parameters.AddWithValue("@IdMesa", idMesa);

                    int filasAfectadas = cmdEliminar.ExecuteNonQuery();
                    resultado = filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en EliminarMesa: " + ex.Message);
                throw new Exception("Error al eliminar la mesa: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return resultado;
        }

        // Método para obtener mesas disponibles por capacidad y fecha
        public List<Dictionary<string, object>> ObtenerMesasDisponiblesPorCapacidadYFecha(int capacidadMinima, DateTime fecha)
        {
            List<Dictionary<string, object>> mesasDisponibles = new List<Dictionary<string, object>>();
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
                    SELECT Id, NumeroMesa, Capacidad, Ubicacion, Estado, DisponibleDesde 
                    FROM Mesas 
                    WHERE Capacidad >= @CapacidadMinima 
                    AND DisponibleDesde <= @Fecha 
                    AND Estado = 'disponible'
                    ORDER BY Capacidad";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CapacidadMinima", capacidadMinima);
                    cmd.Parameters.AddWithValue("@Fecha", fecha);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> mesa = new Dictionary<string, object>
                            {
                                ["Id"] = reader["Id"],
                                ["NumeroMesa"] = reader["NumeroMesa"],
                                ["Capacidad"] = reader["Capacidad"],
                                ["Ubicacion"] = reader["Ubicacion"],
                                ["Estado"] = reader["Estado"],
                                ["DisponibleDesde"] = reader["DisponibleDesde"]
                            };

                            mesasDisponibles.Add(mesa);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerMesasDisponiblesPorCapacidadYFecha: " + ex.Message);
                throw new Exception("Error al obtener mesas disponibles: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return mesasDisponibles;
        }





        public List<Dictionary<string, object>> ObtenerMesasPorCapacidad(int capacidadRequerida)
        {
            List<Dictionary<string, object>> listaMesas = new List<Dictionary<string, object>>();
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();
                if (conn == null)
                {
                    Debug.WriteLine("Error: Conexión nula");
                    return listaMesas;
                }

                // Consulta para obtener mesas con capacidad suficiente
                string query = @"
                    SELECT Id, NumeroMesa, Capacidad, Ubicacion, Estado 
                    FROM Mesas 
                    WHERE Capacidad >= @CapacidadRequerida 
                    AND Estado = 'disponible'
                    ORDER BY Capacidad ASC"; // Ordenamos para obtener primero las de menor capacidad

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CapacidadRequerida", capacidadRequerida);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> mesa = new Dictionary<string, object>
                            {
                                ["Id"] = reader["Id"],
                                ["NumeroMesa"] = reader["NumeroMesa"],
                                ["Capacidad"] = reader["Capacidad"],
                                ["Ubicacion"] = reader["Ubicacion"],
                                ["Estado"] = reader["Estado"]
                            };

                            listaMesas.Add(mesa);
                        }
                    }
                }

                Debug.WriteLine($"Se encontraron {listaMesas.Count} mesas con capacidad de {capacidadRequerida} o más personas");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerMesasPorCapacidad: " + ex.Message);
                throw new Exception("Error al obtener mesas por capacidad: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return listaMesas;
        }
    }
}
