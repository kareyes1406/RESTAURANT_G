using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace capaDatos
{
    public class CD_Reservas
    {
        private readonly CD_conexionBD conexion = new CD_conexionBD();

        // Método para obtener todas las reservas
        public List<Dictionary<string, object>> ObtenerTodasLasReservas()
        {
            List<Dictionary<string, object>> listaReservas = new List<Dictionary<string, object>>();
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();
                if (conn == null)
                {
                    Debug.WriteLine("Error: Conexión nula");
                    return listaReservas;
                }

                string query = @"
                    SELECT r.Id, r.ClienteId, r.MesaId, r.FechaReserva, r.HoraReserva, 
                           r.CantidadPersonas, r.Estado, c.Nombre as NombreCliente, m.NumeroMesa
                    FROM Reservas r
                    INNER JOIN Clientes c ON r.ClienteId = c.Id
                    INNER JOIN Mesas m ON r.MesaId = m.Id
                    ORDER BY r.FechaReserva DESC, r.HoraReserva";

                Debug.WriteLine("Ejecutando query: " + query);

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Dictionary<string, object> reserva = new Dictionary<string, object>
                        {
                            ["Id"] = reader["Id"],
                            ["ClienteId"] = reader["ClienteId"],
                            ["MesaId"] = reader["MesaId"],
                            ["FechaReserva"] = reader["FechaReserva"],
                            ["HoraReserva"] = reader["HoraReserva"],
                            ["CantidadPersonas"] = reader["CantidadPersonas"],
                            ["Estado"] = reader["Estado"],
                            ["NombreCliente"] = reader["NombreCliente"],
                            ["NumeroMesa"] = reader["NumeroMesa"]
                        };

                        listaReservas.Add(reserva);
                    }
                }

                Debug.WriteLine($"Se encontraron {listaReservas.Count} reservas en total");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerTodasLasReservas: " + ex.Message);
                throw new Exception("Error al obtener las reservas: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return listaReservas;
        }

        // Método para obtener reservas por fecha agrupadas por mesa
        public Dictionary<int, List<Dictionary<string, object>>> ObtenerReservasPorFecha(DateTime fecha)
        {
            Dictionary<int, List<Dictionary<string, object>>> reservasPorMesa = new Dictionary<int, List<Dictionary<string, object>>>();
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
                    SELECT r.Id, r.ClienteId, r.MesaId, r.FechaReserva, r.HoraReserva, 
                           r.CantidadPersonas, r.Estado, c.Nombre as NombreCliente, m.NumeroMesa
                    FROM Reservas r
                    INNER JOIN Clientes c ON r.ClienteId = c.Id
                    INNER JOIN Mesas m ON r.MesaId = m.Id
                    WHERE CONVERT(date, r.FechaReserva) = @Fecha
                    ORDER BY r.MesaId, r.HoraReserva";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Fecha", fecha.Date);

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
                                ["NumeroMesa"] = reader["NumeroMesa"],
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
                Debug.WriteLine("Error en ObtenerReservasPorFecha: " + ex.Message);
                throw new Exception("Error al obtener las reservas por fecha: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return reservasPorMesa;
        }

        // Método para marcar una reserva como atendida
        public bool MarcarReservaComoAtendida(int idReserva)
        {
            SqlConnection conn = null;
            bool resultado = false;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = "UPDATE Reservas SET Estado = 'Atendida' WHERE Id = @IdReserva";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdReserva", idReserva);

                    int filasAfectadas = cmd.ExecuteNonQuery();
                    resultado = filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en MarcarReservaComoAtendida: " + ex.Message);
                throw new Exception("Error al marcar la reserva como atendida: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return resultado;
        }

        public bool VerificarDisponibilidadMesa(int idMesa, DateTime fechaReserva, TimeSpan horaReserva)
        {
            SqlConnection conn = null;
            bool estaDisponible = true;

            try
            {
                conn = conexion.ObtenerConexion();

                // Verificar si la mesa existe y está disponible en general
                string queryMesa = @"
            SELECT COUNT(1) 
            FROM Mesas 
            WHERE Id = @IdMesa AND DisponibleDesde <= @Fecha";

                using (SqlCommand cmd = new SqlCommand(queryMesa, conn))
                {
                    cmd.Parameters.AddWithValue("@IdMesa", idMesa);
                    cmd.Parameters.AddWithValue("@Fecha", fechaReserva.Date);

                    int mesaExiste = Convert.ToInt32(cmd.ExecuteScalar());
                    if (mesaExiste == 0)
                    {
                        Debug.WriteLine($"Mesa {idMesa} no existe o no está disponible desde {fechaReserva.Date}");
                        return false;
                    }
                }

                // Calcular la hora de finalización de la reserva (2 horas después)
                TimeSpan horaFinReserva = horaReserva.Add(new TimeSpan(2, 0, 0));

                // Verificar si hay solapamiento con otras reservas activas
                string queryReserva = @"
            SELECT COUNT(1) 
            FROM Reservas 
            WHERE MesaId = @IdMesa 
            AND CONVERT(date, FechaReserva) = @Fecha
            AND Estado NOT IN ('Cancelada', 'Rechazada', 'Finalizada')
            AND (
                -- Caso 1: La nueva reserva comienza durante otra reserva existente
                (@HoraInicio >= HoraReserva AND @HoraInicio < DATEADD(HOUR, 2, HoraReserva))
                OR 
                -- Caso 2: La nueva reserva termina durante otra reserva existente
                (@HoraFin > HoraReserva AND @HoraFin <= DATEADD(HOUR, 2, HoraReserva))
                OR
                -- Caso 3: La nueva reserva contiene completamente a otra reserva existente
                (@HoraInicio <= HoraReserva AND @HoraFin >= DATEADD(HOUR, 2, HoraReserva))
                OR
                -- Caso 4: Una reserva existente contiene completamente la nueva reserva
                (HoraReserva <= @HoraInicio AND DATEADD(HOUR, 2, HoraReserva) >= @HoraFin)
            )";

                using (SqlCommand cmd = new SqlCommand(queryReserva, conn))
                {
                    cmd.Parameters.AddWithValue("@IdMesa", idMesa);
                    cmd.Parameters.AddWithValue("@Fecha", fechaReserva.Date);
                    cmd.Parameters.AddWithValue("@HoraInicio", horaReserva);
                    cmd.Parameters.AddWithValue("@HoraFin", horaFinReserva);

                    int reservasSuperpuestas = Convert.ToInt32(cmd.ExecuteScalar());
                    estaDisponible = reservasSuperpuestas == 0;

                    Debug.WriteLine($"Mesa {idMesa}, Fecha {fechaReserva.Date}, Hora {horaReserva}-{horaFinReserva} - Superposiciones: {reservasSuperpuestas}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en VerificarDisponibilidadMesa: " + ex.Message);
                throw new Exception("Error al verificar disponibilidad de mesa: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return estaDisponible;
        }

        // Método para crear una nueva reserva
        public bool CrearReserva(int clienteId, int idMesa, DateTime fechaReserva, TimeSpan horaReserva, int cantidadPersonas)
        {
            SqlConnection conn = null;
            bool resultado = false;
            try
            {
                // Liberar mesas con reservas expiradas antes de verificar disponibilidad
                LiberarMesasConReservasExpiradas();

                // Verificar disponibilidad en el horario específico antes de crear la reserva
                if (!VerificarDisponibilidadMesa(idMesa, fechaReserva, horaReserva))
                {
                    Debug.WriteLine($"La mesa {idMesa} no está disponible en la fecha {fechaReserva.Date} a las {horaReserva}");
                    return false;
                }

                conn = conexion.ObtenerConexion();

                // Insertar la nueva reserva con duración exacta de 2 horas
                string query = @"
            INSERT INTO Reservas (ClienteId, MesaId, FechaReserva, HoraReserva, CantidadPersonas, Estado) 
            VALUES (@ClienteId, @IdMesa, @FechaReserva, @HoraReserva, @CantidadPersonas, 'Pendiente');
            SELECT SCOPE_IDENTITY();";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Asegurarse de que los parámetros tienen el tipo correcto
                    cmd.Parameters.Add("@ClienteId", SqlDbType.Int).Value = clienteId;
                    cmd.Parameters.Add("@IdMesa", SqlDbType.Int).Value = idMesa;
                    cmd.Parameters.Add("@FechaReserva", SqlDbType.Date).Value = fechaReserva.Date;
                    cmd.Parameters.Add("@HoraReserva", SqlDbType.Time).Value = horaReserva;
                    cmd.Parameters.Add("@CantidadPersonas", SqlDbType.Int).Value = cantidadPersonas;

                    // Obtener el ID de la nueva reserva
                    object result = cmd.ExecuteScalar();
                    resultado = result != null && result != DBNull.Value;

                    if (resultado)
                    {
                        Debug.WriteLine("Reserva creada con ID: " + result.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en CrearReserva: " + ex.Message);
                throw new Exception("Error al crear la reserva: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }
            return resultado;
        }


        // Método para cancelar una reserva
        public bool CancelarReserva(int idReserva)
        {
            SqlConnection conn = null;
            bool resultado = false;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = "UPDATE Reservas SET Estado = 'Cancelada' WHERE Id = @IdReserva";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdReserva", idReserva);

                    int filasAfectadas = cmd.ExecuteNonQuery();
                    resultado = filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en CancelarReserva: " + ex.Message);
                throw new Exception("Error al cancelar la reserva: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return resultado;
        }

        // Método para obtener una reserva por su ID
        public Dictionary<string, object> ObtenerReservaPorId(int idReserva)
        {
            Dictionary<string, object> reserva = null;
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
                    SELECT r.Id, r.ClienteId, r.MesaId, r.FechaReserva, r.HoraReserva, 
                           r.CantidadPersonas, r.Estado, c.Nombre as NombreCliente, m.NumeroMesa
                    FROM Reservas r
                    INNER JOIN Clientes c ON r.ClienteId = c.Id
                    INNER JOIN Mesas m ON r.MesaId = m.Id
                    WHERE r.Id = @IdReserva";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdReserva", idReserva);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            reserva = new Dictionary<string, object>
                            {
                                ["Id"] = reader["Id"],
                                ["ClienteId"] = reader["ClienteId"],
                                ["MesaId"] = reader["MesaId"],
                                ["FechaReserva"] = reader["FechaReserva"],
                                ["HoraReserva"] = reader["HoraReserva"],
                                ["CantidadPersonas"] = reader["CantidadPersonas"],
                                ["Estado"] = reader["Estado"],
                                ["NombreCliente"] = reader["NombreCliente"],
                                ["NumeroMesa"] = reader["NumeroMesa"]
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerReservaPorId: " + ex.Message);
                throw new Exception("Error al obtener la reserva: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return reserva;
        }

        // Método para obtener las reservas de un cliente
        public List<Dictionary<string, object>> ObtenerReservasPorCliente(int clienteId)
        {
            List<Dictionary<string, object>> listaReservas = new List<Dictionary<string, object>>();
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
                    SELECT r.Id, r.ClienteId, r.MesaId, r.FechaReserva, r.HoraReserva, 
                           r.CantidadPersonas, r.Estado, c.Nombre as NombreCliente, m.NumeroMesa, m.Ubicacion
                    FROM Reservas r
                    INNER JOIN Clientes c ON r.ClienteId = c.Id
                    INNER JOIN Mesas m ON r.MesaId = m.Id
                    WHERE r.ClienteId = @ClienteId
                    ORDER BY r.FechaReserva DESC, r.HoraReserva";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ClienteId", clienteId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> reserva = new Dictionary<string, object>
                            {
                                ["Id"] = reader["Id"],
                                ["ClienteId"] = reader["ClienteId"],
                                ["MesaId"] = reader["MesaId"],
                                ["FechaReserva"] = reader["FechaReserva"],
                                ["HoraReserva"] = reader["HoraReserva"],
                                ["CantidadPersonas"] = reader["CantidadPersonas"],
                                ["Estado"] = reader["Estado"],
                                ["NombreCliente"] = reader["NombreCliente"],
                                ["NumeroMesa"] = reader["NumeroMesa"],
                                ["Ubicacion"] = reader["Ubicacion"]
                            };

                            listaReservas.Add(reserva);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerReservasPorCliente: " + ex.Message);
                throw new Exception("Error al obtener las reservas del cliente: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return listaReservas;
        }

        // Método para actualizar el estado de una reserva
        public bool ActualizarEstadoReserva(int idReserva, string nuevoEstado)
        {
            SqlConnection conn = null;
            bool resultado = false;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = "UPDATE Reservas SET Estado = @NuevoEstado WHERE Id = @IdReserva";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdReserva", idReserva);
                    cmd.Parameters.AddWithValue("@NuevoEstado", nuevoEstado);

                    int filasAfectadas = cmd.ExecuteNonQuery();
                    resultado = filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ActualizarEstadoReserva: " + ex.Message);
                throw new Exception("Error al actualizar el estado de la reserva: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return resultado;
        }

        // Método mejorado para liberar mesas con reservas expiradas
        public void LiberarMesasConReservasExpiradas()
        {
            SqlConnection conn = null;

            try
            {
                conn = conexion.ObtenerConexion();

                // Actualizar reservas que han superado las 2 horas desde su hora de inicio
                string query = @"
            UPDATE Reservas 
            SET Estado = 'Finalizada' 
            WHERE Estado IN ('Pendiente', 'Atendida') 
            AND (
                -- Caso 1: Reservas de hoy que ya expiraron su tiempo de 2 horas
                (CONVERT(DATE, FechaReserva) = CONVERT(DATE, GETDATE()) 
                 AND DATEADD(HOUR, 2, HoraReserva) <= CONVERT(TIME, GETDATE()))
                OR
                -- Caso 2: Reservas de días anteriores
                (CONVERT(DATE, FechaReserva) < CONVERT(DATE, GETDATE()))
            )";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    Debug.WriteLine($"Se liberaron {filasAfectadas} reservas expiradas");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en LiberarMesasConReservasExpiradas: " + ex.Message);
                throw new Exception("Error al liberar mesas con reservas expiradas: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }
        }

        // Nuevo método: Verificar si una reserva ha expirado (duró más de 2 horas)
        public bool VerificarReservaExpirada(int idReserva)
        {
            SqlConnection conn = null;
            bool haExpirado = false;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
                    SELECT 
                        CASE 
                            WHEN (CONVERT(DATE, FechaReserva) = CONVERT(DATE, GETDATE()) 
                                 AND DATEADD(HOUR, 2, HoraReserva) <= CONVERT(TIME, GETDATE()))
                                OR (CONVERT(DATE, FechaReserva) < CONVERT(DATE, GETDATE()))
                            THEN 1
                            ELSE 0
                        END as Expirada
                    FROM Reservas 
                    WHERE Id = @IdReserva";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdReserva", idReserva);
                    var resultado = cmd.ExecuteScalar();

                    if (resultado != null && resultado != DBNull.Value)
                    {
                        haExpirado = Convert.ToInt32(resultado) == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en VerificarReservaExpirada: " + ex.Message);
                throw new Exception("Error al verificar si la reserva ha expirado: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return haExpirado;
        }

        // Nuevo método: Obtener tiempo restante de una reserva en minutos
        public int ObtenerMinutosRestantesReserva(int idReserva)
        {
            SqlConnection conn = null;
            int minutosRestantes = 0;

            try
            {
                conn = conexion.ObtenerConexion();
                string query = @"
                    SELECT 
                        CASE 
                            WHEN CONVERT(DATE, FechaReserva) = CONVERT(DATE, GETDATE()) THEN
                                DATEDIFF(MINUTE, GETDATE(), 
                                    DATEADD(HOUR, 2, 
                                        CONVERT(DATETIME, 
                                            CONVERT(DATE, FechaReserva) + CONVERT(VARCHAR, HoraReserva, 108)
                                        )
                                    )
                                )
                            ELSE 0
                        END as MinutosRestantes
                    FROM Reservas 
                    WHERE Id = @IdReserva";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdReserva", idReserva);
                    var resultado = cmd.ExecuteScalar();

                    if (resultado != null && resultado != DBNull.Value)
                    {
                        int minutos = Convert.ToInt32(resultado);
                        minutosRestantes = minutos > 0 ? minutos : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error en ObtenerMinutosRestantesReserva: " + ex.Message);
                throw new Exception("Error al obtener los minutos restantes de la reserva: " + ex.Message, ex);
            }
            finally
            {
                conexion.CerrarConexion(conn);
            }

            return minutosRestantes;
        }
    }
}