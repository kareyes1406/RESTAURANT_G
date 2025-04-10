using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;

using System.Web.Script.Serialization;
using REFOOD.Filters;

namespace Administrador.Controllers
{
    [AdminAuthentication]
    public class EstadisticasController : Controller
    {
        private readonly string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ConexionBD"].ConnectionString;

        // Vista principal del dashboard
        public ActionResult Dashboard()
        {
            // Comprobar si el usuario está autenticado como administrador
            if (Session["AdminEmail"] == null)
            {
                return RedirectToAction("Index", "Acceso");
            }

            return View();
        }

        // Método para obtener los datos de reservas diarias (última semana)
        [HttpGet]
        public JsonResult ObtenerReservasDiarias()
        {
            try
            {
                // Obtener datos para los últimos 7 días
                DateTime fechaFin = DateTime.Today;
                DateTime fechaInicio = fechaFin.AddDays(-6); // 7 días incluyendo hoy

                var datos = ObtenerDatosReservasPorPeriodo(fechaInicio, fechaFin, "dia");

                return Json(datos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Método para obtener los datos de reservas semanales (últimas 4 semanas)
        [HttpGet]
        public JsonResult ObtenerReservasSemanales()
        {
            try
            {
                // Obtener datos para las últimas 4 semanas
                DateTime fechaFin = DateTime.Today;
                DateTime fechaInicio = fechaFin.AddDays(-28); // Aproximadamente 4 semanas

                var datos = ObtenerDatosReservasPorPeriodo(fechaInicio, fechaFin, "semana");

                return Json(datos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Método para obtener los datos de reservas mensuales (últimos 6 meses)
        [HttpGet]
        public JsonResult ObtenerReservasMensuales()
        {
            try
            {
                // Obtener datos para los últimos 6 meses
                DateTime fechaFin = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1); // Último día del mes actual
                DateTime fechaInicio = fechaFin.AddMonths(-5).AddDays(-(fechaFin.Day - 1)); // Primer día 6 meses atrás

                var datos = ObtenerDatosReservasPorPeriodo(fechaInicio, fechaFin, "mes");

                return Json(datos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Método auxiliar para obtener datos de reservas por período
        private object ObtenerDatosReservasPorPeriodo(DateTime fechaInicio, DateTime fechaFin, string tipoPeriodo)
        {
            List<string> etiquetas = new List<string>();
            List<int> cantidades = new List<int>();
            List<int> atendidas = new List<int>();
            List<int> canceladas = new List<int>();

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                conexion.Open();
                string query = "";

                switch (tipoPeriodo)
                {
                    case "dia":
                        query = @"
                            DECLARE @FechaInicio DATE = @paramFechaInicio;
                            DECLARE @FechaFin DATE = @paramFechaFin;

                            WITH Fechas AS (
                                SELECT @FechaInicio AS Fecha
                                UNION ALL
                                SELECT DATEADD(DAY, 1, Fecha)
                                FROM Fechas
                                WHERE DATEADD(DAY, 1, Fecha) <= @FechaFin
                            )
                            SELECT 
                                F.Fecha,
                                COALESCE(COUNT(R.Id), 0) AS TotalReservas,
                                COALESCE(SUM(CASE WHEN R.Estado = 'Atendida' THEN 1 ELSE 0 END), 0) AS ReservasAtendidas,
                                COALESCE(SUM(CASE WHEN R.Estado = 'Cancelada' THEN 1 ELSE 0 END), 0) AS ReservasCanceladas
                            FROM Fechas F
                            LEFT JOIN Reservas R ON CONVERT(DATE, R.FechaReserva) = F.Fecha
                            GROUP BY F.Fecha
                            ORDER BY F.Fecha;
                        ";
                        break;

                    case "semana":
                        query = @"
                            DECLARE @FechaInicio DATE = @paramFechaInicio;
                            DECLARE @FechaFin DATE = @paramFechaFin;

                            WITH Fechas AS (
                                SELECT @FechaInicio AS FechaInicio, 
                                       DATEADD(DAY, 6, @FechaInicio) AS FechaFin
                                UNION ALL
                                SELECT DATEADD(DAY, 7, FechaInicio),
                                       DATEADD(DAY, 7, FechaFin)
                                FROM Fechas
                                WHERE DATEADD(DAY, 7, FechaInicio) <= @FechaFin
                            )
                            SELECT 
                                CONCAT('Semana ', ROW_NUMBER() OVER(ORDER BY F.FechaInicio)) AS Semana,
                                F.FechaInicio,
                                F.FechaFin,
                                COALESCE(COUNT(R.Id), 0) AS TotalReservas,
                                COALESCE(SUM(CASE WHEN R.Estado = 'Atendida' THEN 1 ELSE 0 END), 0) AS ReservasAtendidas,
                                COALESCE(SUM(CASE WHEN R.Estado = 'Cancelada' THEN 1 ELSE 0 END), 0) AS ReservasCanceladas
                            FROM Fechas F
                            LEFT JOIN Reservas R ON CONVERT(DATE, R.FechaReserva) BETWEEN F.FechaInicio AND F.FechaFin
                            GROUP BY F.FechaInicio, F.FechaFin
                            ORDER BY F.FechaInicio;
                        ";
                        break;

                    case "mes":
                        query = @"
                            DECLARE @FechaInicio DATE = @paramFechaInicio;
                            DECLARE @FechaFin DATE = @paramFechaFin;

                            WITH Meses AS (
                                SELECT 
                                    DATEFROMPARTS(YEAR(@FechaInicio), MONTH(@FechaInicio), 1) AS FechaInicio,
                                    EOMONTH(DATEFROMPARTS(YEAR(@FechaInicio), MONTH(@FechaInicio), 1)) AS FechaFin
                                UNION ALL
                                SELECT 
                                    DATEADD(MONTH, 1, FechaInicio),
                                    EOMONTH(DATEADD(MONTH, 1, FechaInicio))
                                FROM Meses
                                WHERE DATEADD(MONTH, 1, FechaInicio) <= DATEFROMPARTS(YEAR(@FechaFin), MONTH(@FechaFin), 1)
                            )
                            SELECT 
                                FORMAT(M.FechaInicio, 'MMM yyyy') AS Mes,
                                M.FechaInicio,
                                M.FechaFin,
                                COALESCE(COUNT(R.Id), 0) AS TotalReservas,
                                COALESCE(SUM(CASE WHEN R.Estado = 'Atendida' THEN 1 ELSE 0 END), 0) AS ReservasAtendidas,
                                COALESCE(SUM(CASE WHEN R.Estado = 'Cancelada' THEN 1 ELSE 0 END), 0) AS ReservasCanceladas
                            FROM Meses M
                            LEFT JOIN Reservas R ON CONVERT(DATE, R.FechaReserva) BETWEEN M.FechaInicio AND M.FechaFin
                            GROUP BY M.FechaInicio, M.FechaFin
                            ORDER BY M.FechaInicio;
                        ";
                        break;
                }

                using (SqlCommand cmd = new SqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@paramFechaInicio", fechaInicio);
                    cmd.Parameters.AddWithValue("@paramFechaFin", fechaFin);
                    cmd.CommandTimeout = 120; // 2 minutos

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            switch (tipoPeriodo)
                            {
                                case "dia":
                                    etiquetas.Add(Convert.ToDateTime(reader["Fecha"]).ToString("dd/MM"));
                                    break;
                                case "semana":
                                    string semana = reader["Semana"].ToString();
                                    DateTime inicioSemana = Convert.ToDateTime(reader["FechaInicio"]);
                                    DateTime finSemana = Convert.ToDateTime(reader["FechaFin"]);
                                    etiquetas.Add($"{semana} ({inicioSemana:dd/MM} - {finSemana:dd/MM})");
                                    break;
                                case "mes":
                                    etiquetas.Add(reader["Mes"].ToString());
                                    break;
                            }

                            cantidades.Add(Convert.ToInt32(reader["TotalReservas"]));
                            atendidas.Add(Convert.ToInt32(reader["ReservasAtendidas"]));
                            canceladas.Add(Convert.ToInt32(reader["ReservasCanceladas"]));
                        }
                    }
                }
            }

            return new
            {
                etiquetas = etiquetas,
                series = new[]
                {
                    new { name = "Total Reservas", data = cantidades },
                    new { name = "Atendidas", data = atendidas },
                    new { name = "Canceladas", data = canceladas }
                }
            };
        }

        // Método para obtener estadísticas generales
        [HttpGet]
        public JsonResult ObtenerEstadisticasGenerales()
        {
            try
            {
                int totalReservas = 0;
                int reservasHoy = 0;
                int reservasPendientes = 0;
                int ocupacionPromedio = 0;

                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open();

                    // Total de reservas
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Reservas", conexion))
                    {
                        totalReservas = (int)cmd.ExecuteScalar();
                    }

                    // Reservas de hoy
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Reservas WHERE CONVERT(DATE, FechaReserva) = CONVERT(DATE, GETDATE())", conexion))
                    {
                        reservasHoy = (int)cmd.ExecuteScalar();
                    }

                    // Reservas pendientes
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Reservas WHERE Estado = 'Pendiente'", conexion))
                    {
                        reservasPendientes = (int)cmd.ExecuteScalar();
                    }

                    // Ocupación promedio (personas por reserva)
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(AVG(CantidadPersonas), 0) FROM Reservas", conexion))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != DBNull.Value)
                        {
                            ocupacionPromedio = (int)Math.Round(Convert.ToDouble(result));
                        }
                    }
                }

                return Json(new
                {
                    totalReservas = totalReservas,
                    reservasHoy = reservasHoy,
                    reservasPendientes = reservasPendientes,
                    ocupacionPromedio = ocupacionPromedio
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}