using Distribuidora_La_Central.Web.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrasladoController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TrasladoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Obtiene los traslados filtrados por código de bodega origen.
        /// </summary>
        /// <param name="codigoBodegaOrigen">Código de la bodega origen</param>
        /// <returns>Lista de traslados</returns>
        [HttpGet]
        [Route("GetTodosLosTraslados")]
        [Produces("application/json")]
        public ActionResult<List<Traslado>> GetTodosLosTraslados()
        {
            List<Traslado> trasladoList = new List<Traslado>();

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Traslado", con))
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Traslado traslado = new Traslado
                                {
                                    idTraslado = Convert.ToInt32(reader["idTraslado"]),
                                    codigoProducto = Convert.ToInt32(reader["codigoProducto"]),
                                    idBodegaOrigen = Convert.ToInt32(reader["idBodegaOrigen"]),
                                    idBodegaDestino = Convert.ToInt32(reader["idBodegaDestino"]),
                                    cantidad = Convert.ToInt32(reader["cantidad"]),
                                    fechaTraslado = reader["fechaTraslado"] == DBNull.Value
                                        ? null
                                        : Convert.ToDateTime(reader["fechaTraslado"]),
                                    realizadoPor = Convert.ToInt32(reader["realizadoPor"]),
                                    estado = reader["estado"].ToString()
                                };

                                trasladoList.Add(traslado);
                            }
                        }
                    }
                }

                return Ok(trasladoList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }


        /// <summary>
        /// Registra un nuevo traslado (sin detalle).
        /// </summary>
        [HttpPost]
        [Route("PostTraslado")]
        public string PostTraslado([FromBody] Traslado traslado)
        {
            Response response = new Response();

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand(@"INSERT INTO Traslado 
                (codigoProducto, idBodegaOrigen, idBodegaDestino, cantidad, fechaTraslado, realizadoPor, estado) 
                VALUES 
                (@codigoProducto, @idBodegaOrigen, @idBodegaDestino, @cantidad, @fechaTraslado, @realizadoPor, @estado)", con))
                    {
                        cmd.Parameters.AddWithValue("@codigoProducto", traslado.codigoProducto);
                        cmd.Parameters.AddWithValue("@idBodegaOrigen", traslado.idBodegaOrigen);
                        cmd.Parameters.AddWithValue("@idBodegaDestino", traslado.idBodegaDestino);
                        cmd.Parameters.AddWithValue("@cantidad", traslado.cantidad);
                        cmd.Parameters.AddWithValue("@fechaTraslado", traslado.fechaTraslado ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@realizadoPor", traslado.realizadoPor);
                        cmd.Parameters.AddWithValue("@estado", traslado.estado ?? "");

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        con.Close();

                        if (rowsAffected > 0)
                        {
                            response.StatusCode = 200;
                            response.ErrorMessage = "Traslado guardado correctamente";
                        }
                        else
                        {
                            response.StatusCode = 100;
                            response.ErrorMessage = "Error al guardar el traslado";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = "Error interno: " + ex.Message;
            }

            return JsonConvert.SerializeObject(response);
        }


        [HttpGet]
        [Route("DescargarReporteTraslados")]
        public IActionResult DescargarReporteTraslados()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Consulta con los campos requeridos
                string query = @"SELECT t.idTraslado, t.codigoProducto, 
                        bo.nombre AS bodegaOrigen, bd.nombre AS bodegaDestino,
                        t.cantidad, t.fechaTraslado, t.realizadoPor, t.estado
                FROM Traslado t
                JOIN Bodega bo ON t.idBodegaOrigen = bo.idBodega
                JOIN Bodega bd ON t.idBodegaDestino = bd.idBodega";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                using (var stream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4.Rotate()); // Horizontal
                    PdfWriter.GetInstance(document, stream).CloseStream = false;
                    document.Open();

                    // Fuentes (consistentes con el otro reporte)
                    var fontTitle = FontFactory.GetFont("Arial", 18, Font.BOLD);
                    var fontHeader = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
                    var fontCell = FontFactory.GetFont("Arial", 9);

                    // Título
                    document.Add(new Paragraph("Reporte de Traslados", fontTitle));
                    document.Add(Chunk.NEWLINE);

                    // Tabla con 8 columnas
                    PdfPTable table = new PdfPTable(8);
                    table.WidthPercentage = 100;

                    // Anchos de columnas optimizados
                    float[] columnWidths = new float[] { 1f, 1.5f, 2f, 2f, 1f, 1.5f, 2f, 1.5f };
                    table.SetWidths(columnWidths);

                    // Encabezados (mismo estilo que facturas)
                    AddHeaderCell(table, "ID Traslado", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Cód. Producto", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Bodega Origen", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Bodega Destino", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Cantidad", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Fecha Traslado", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Realizado Por", fontHeader, BaseColor.DARK_GRAY);
                    AddHeaderCell(table, "Estado", fontHeader, BaseColor.DARK_GRAY);

                    // Datos con manejo de nulos
                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(new Phrase(row["idTraslado"].ToString(), fontCell));
                        table.AddCell(new Phrase(row["codigoProducto"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["bodegaOrigen"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["bodegaDestino"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["cantidad"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["fechaTraslado"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["realizadoPor"]?.ToString() ?? "-", fontCell));
                        table.AddCell(new Phrase(row["estado"]?.ToString() ?? "-", fontCell));
                    }

                    document.Add(table);
                    document.Close();

                    stream.Position = 0;
                    return File(stream.ToArray(), "application/pdf", "Reporte_Traslados.pdf");
                }
            }
        }

        // Método auxiliar idéntico al original
        private void AddHeaderCell(PdfPTable table, string text, Font font, BaseColor bgColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bgColor;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            table.AddCell(cell);
        }



    }
}