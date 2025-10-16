using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnidadMedidaController : ControllerBase
    {
        private readonly IConfiguration _config;

        public UnidadMedidaController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Get()
        {
            List<UnidadMedida> lista = new();
            string query = "SELECT * FROM UnidadMedida";

            using SqlConnection con = new(_config.GetConnectionString("DefaultConnection"));
            SqlCommand cmd = new(query, con);
            con.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new UnidadMedida
                {
                    IdUnidad = Convert.ToInt32(dr["IdUnidad"]),
                    Nombre = dr["Nombre"].ToString()
                });
            }

            return Ok(lista);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            string query = "SELECT * FROM UnidadMedida WHERE IdUnidad = @Id";

            using SqlConnection con = new(_config.GetConnectionString("DefaultConnection"));
            SqlCommand cmd = new(query, con);
            cmd.Parameters.AddWithValue("@Id", id);

            con.Open();
            SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                var unidad = new UnidadMedida
                {
                    IdUnidad = Convert.ToInt32(dr["IdUnidad"]),
                    Nombre = dr["Nombre"].ToString()
                };
                return Ok(unidad);
            }

            return NotFound();
        }

        [HttpPost]
        public IActionResult Post(UnidadMedida unidad)
        {
            string query = "INSERT INTO UnidadMedida (Nombre) VALUES (@Nombre); SELECT SCOPE_IDENTITY();";

            using SqlConnection con = new(_config.GetConnectionString("DefaultConnection"));
            SqlCommand cmd = new(query, con);
            cmd.Parameters.AddWithValue("@Nombre", unidad.Nombre);

            con.Open();
            var newId = cmd.ExecuteScalar();

            return CreatedAtAction(nameof(GetById), new { id = newId }, unidad);
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, UnidadMedida unidad)
        {
            string query = "UPDATE UnidadMedida SET Nombre = @Nombre WHERE IdUnidad = @Id";

            using SqlConnection con = new(_config.GetConnectionString("DefaultConnection"));
            SqlCommand cmd = new(query, con);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Nombre", unidad.Nombre);

            con.Open();
            int affectedRows = cmd.ExecuteNonQuery();

            if (affectedRows > 0)
            {
                return NoContent();
            }

            return NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            string query = "DELETE FROM UnidadMedida WHERE IdUnidad = @Id";

            using SqlConnection con = new(_config.GetConnectionString("DefaultConnection"));
            SqlCommand cmd = new(query, con);
            cmd.Parameters.AddWithValue("@Id", id);

            con.Open();
            int affectedRows = cmd.ExecuteNonQuery();

            if (affectedRows > 0)
            {
                return NoContent();
            }

            return NotFound();
        }
    }
}