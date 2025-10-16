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
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("Login")]
        public IActionResult Login([FromBody] Usuario usuario)
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());

            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Usuario WHERE nombre = @nombre AND codigoAcceso = @codigoAcceso", con);
            da.SelectCommand.Parameters.AddWithValue("@nombre", usuario.nombre);
            da.SelectCommand.Parameters.AddWithValue("@codigoAcceso", usuario.codigoAcceso);

            DataTable dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                // Crear objeto Usuario desde los datos del DataTable
                Usuario usuarioEncontrado = new Usuario
                {
                    idUsuario = Convert.ToInt32(dt.Rows[0]["idUsuario"]),
                    nombre = dt.Rows[0]["nombre"].ToString(),
                    rol = dt.Rows[0]["rol"].ToString(),
                    codigoAcceso = dt.Rows[0]["codigoAcceso"].ToString()
                };

                return Ok(usuarioEncontrado); // ✅ Devuelve un JSON con el usuario
            }
            else
            {
                return Unauthorized("Nombre o código incorrecto");
            }
        }



        [HttpPost("Registrar")]
        public IActionResult Registrar([FromBody] Usuario usuario)
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());

            SqlDataAdapter checkUser = new SqlDataAdapter("SELECT * FROM Usuario WHERE nombre = @nombre", con);
            checkUser.SelectCommand.Parameters.AddWithValue("@nombre", usuario.nombre);

            DataTable dt = new DataTable();
            checkUser.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                return BadRequest("El usuario ya existe");
            }

            SqlCommand cmd = new SqlCommand("INSERT INTO Usuario (nombre, rol, codigoAcceso) VALUES (@nombre, @rol, @codigoAcceso)", con);
            cmd.Parameters.AddWithValue("@nombre", usuario.nombre);
            cmd.Parameters.AddWithValue("@rol", usuario.rol);
            cmd.Parameters.AddWithValue("@codigoAcceso", usuario.codigoAcceso);

            con.Open();
            int i = cmd.ExecuteNonQuery();
            con.Close();

            if (i > 0)
            {
                return Ok("Registro exitoso");
            }
            else
            {
                return StatusCode(500, "Error al registrar usuario");
            }
        }


        [HttpGet("Lista")]
        public IActionResult GetUsuarios()
        {
            List<Usuario> usuarios = new List<Usuario>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Usuario", con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    usuarios.Add(new Usuario
                    {
                        idUsuario = Convert.ToInt32(reader["idUsuario"]),
                        nombre = reader["nombre"].ToString(),
                        rol = reader["rol"].ToString(),
                        codigoAcceso = reader["codigoAcceso"].ToString()
                    });
                }
            }

            return Ok(usuarios);
        }





    }
}

