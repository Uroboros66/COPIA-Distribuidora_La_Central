using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DetalleCompraController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DetalleCompraController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetAllDetallesCompra")]
        public string GetDetallesCompra()
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM DetalleCompra;", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<DetalleCompra> detalleList = new List<DetalleCompra>();
            Response response = new Response();

            foreach (DataRow row in dt.Rows)
            {
                DetalleCompra detalle = new DetalleCompra
                {
                    IdDetalleCompra = Convert.ToInt32(row["IdDetalleCompra"]),
                    IdCompra = Convert.ToInt32(row["IdCompra"]),
                    CodigoProducto = Convert.ToInt32(row["CodigoProducto"]),
                    Cantidad = Convert.ToInt32(row["Cantidad"]),
                    PrecioUnitario = Convert.ToDecimal(row["PrecioUnitario"]),
                };
                detalleList.Add(detalle);
            }

            if (detalleList.Count > 0)
                return JsonConvert.SerializeObject(detalleList);
            else
            {
                response.StatusCode = 100;
                response.ErrorMessage = "No se encontraron detalles de compra.";
                return JsonConvert.SerializeObject(response);
            }
        }

        [HttpPost]
        [Route("AgregarDetalleCompra")]
        public IActionResult AgregarDetalleCompra([FromBody] DetalleCompra detalle)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"INSERT INTO DetalleCompra (IdCompra, CodigoProducto, Cantidad, PrecioUnitario)
                             VALUES (@IdCompra, @CodigoProducto, @Cantidad, @PrecioUnitario)";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@IdCompra", detalle.IdCompra);
            cmd.Parameters.AddWithValue("@CodigoProducto", detalle.CodigoProducto);
            cmd.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
            cmd.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);

            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            return Ok(rowsAffected > 0);
        }

        [HttpDelete]
        [Route("EliminarDetalleCompra/{id}")]
        public IActionResult EliminarDetalleCompra(int id)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = "DELETE FROM DetalleCompra WHERE IdDetalleCompra = @id";
            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", id);

            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            return Ok(rowsAffected > 0);
        }

        [HttpPut]
        [Route("ActualizarDetalleCompra/{id}")]
        public IActionResult ActualizarDetalleCompra(int id, [FromBody] DetalleCompra detalle)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"UPDATE DetalleCompra SET 
                                IdCompra = @IdCompra,
                                CodigoProducto = @CodigoProducto,
                                Cantidad = @Cantidad,
                                PrecioUnitario = @PrecioUnitario
                             WHERE IdDetalleCompra = @IdDetalleCompra";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@IdDetalleCompra", id);
            cmd.Parameters.AddWithValue("@IdCompra", detalle.IdCompra);
            cmd.Parameters.AddWithValue("@CodigoProducto", detalle.CodigoProducto);
            cmd.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
            cmd.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);

            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
                return Ok(new { message = "Detalle de compra actualizado correctamente." });
            else
                return NotFound(new { message = "Detalle de compra no encontrado." });
        }
    }
}