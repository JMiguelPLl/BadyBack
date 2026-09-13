using BadyApi.Helpers;
using BadyBackend.Data;
using BadyBackend.DTOs.AsignacionPedido;
using BadyBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BadyBackend.DTOs.AsignacionPedido.AsignacionPedidoCreateDto;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AsignacionPedidoController
        : ControllerBase
    {
        private readonly AppDbContext _context;


        public AsignacionPedidoController(
            AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // LISTAR TODAS
        // =====================================================

        /*
         * GET:
         *
         * api/AsignacionPedido/Listar
         */
        [HttpGet("Listar")]
        public async Task<IActionResult> Listar()
        {
            var asignaciones =
                await ConstruirConsulta()
                    .OrderByDescending(
                        a => a.Fecha_Asignacion
                    )
                    .ToListAsync();


            var resultado =
                asignaciones
                    .Select(MapearAsignacion)
                    .ToList();


            return Ok(resultado);
        }


        // =====================================================
        // LISTAR POR ESTADO
        // =====================================================

        /*
         * GET:
         *
         * api/AsignacionPedido/Estado/Asignado
         *
         * api/AsignacionPedido/Estado/Entregado
         */
        [HttpGet("Estado/{estado}")]
        public async Task<IActionResult>
            ListarPorEstado(
                string estado)
        {
            var estadoNormalizado =
                EstadosAsignacionPedido
                    .Normalizar(estado);


            if (estadoNormalizado == null)
            {
                return BadRequest(new
                {
                    message =
                        "El estado solamente puede ser Asignado o Entregado."
                });
            }


            var asignaciones =
                await ConstruirConsulta()
                    .Where(a =>
                        a.Estado ==
                        estadoNormalizado
                    )
                    .OrderByDescending(
                        a => a.Fecha_Asignacion
                    )
                    .ToListAsync();


            var resultado =
                asignaciones
                    .Select(MapearAsignacion)
                    .ToList();


            return Ok(resultado);
        }


        // =====================================================
        // OBTENER POR ID
        // =====================================================

        /*
         * GET:
         *
         * api/AsignacionPedido/5
         */
        [HttpGet("{id:int}")]
        public async Task<IActionResult>
            ObtenerPorId(
                int id)
        {
            var asignacion =
                await ConstruirConsulta()
                    .FirstOrDefaultAsync(
                        a => a.Id == id
                    );


            if (asignacion == null)
            {
                return NotFound(new
                {
                    message =
                        "La asignación de pedido no existe."
                });
            }


            return Ok(
                MapearAsignacion(
                    asignacion
                )
            );
        }


        // =====================================================
        // CREAR ASIGNACIÓN
        // =====================================================

        /*
         * FLUJO:
         *
         * Pedido:
         * Pendiente
         *
         * ↓
         *
         * Se crea Asignacion_Pedido
         *
         * Asignacion_Pedido:
         * Asignado
         *
         * Pedido:
         * Asignado
         *
         *
         * POST:
         *
         * api/AsignacionPedido/Agregar
         */
        [HttpPost("Agregar")]
        public async Task<IActionResult>
            Agregar(
                [FromBody]
                AsignacionPedidoCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            // =============================================
            // BUSCAR PEDIDO
            // =============================================

            var pedido =
                await _context.Pedidos
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            dto.IdPedido
                    );


            if (pedido == null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido seleccionado no existe."
                });
            }


            // =============================================
            // SOLO PEDIDOS PENDIENTES
            // =============================================

            if (
                !string.Equals(
                    pedido.Estado,
                    EstadosPedido.Pendiente,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Solo se pueden asignar pedidos que estén en estado Pendiente."
                });
            }


            // =============================================
            // BUSCAR ASIGNACIÓN USUARIO - VEHÍCULO
            // =============================================

            var asignacionVehiculo =
                await _context
                    .Asignacion_Vehiculos

                    .Include(a =>
                        a.Usuario
                    )

                    .Include(a =>
                        a.Vehiculo
                    )

                    .FirstOrDefaultAsync(
                        a =>
                            a.Id ==
                            dto.IdAsignacionVehiculo
                    );


            if (asignacionVehiculo == null)
            {
                return NotFound(new
                {
                    message =
                        "La asignación de vehículo no existe."
                });
            }


            // =============================================
            // ASIGNACIÓN DE VEHÍCULO ACTIVA
            // =============================================

            if (
                !string.Equals(
                    asignacionVehiculo.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "La asignación entre usuario y vehículo está inactiva."
                });
            }


            // =============================================
            // VALIDAR USUARIO
            // =============================================

            if (
                asignacionVehiculo.Usuario ==
                null
            )
            {
                return BadRequest(new
                {
                    message =
                        "La asignación no tiene un usuario válido."
                });
            }


            if (
                !string.Equals(
                    asignacionVehiculo
                        .Usuario
                        .Estado,

                    "Activo",

                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "El usuario asignado se encuentra inactivo."
                });
            }


            // =============================================
            // VALIDAR VEHÍCULO
            // =============================================

            if (
                asignacionVehiculo.Vehiculo ==
                null
            )
            {
                return BadRequest(new
                {
                    message =
                        "La asignación no tiene un vehículo válido."
                });
            }


            if (
                !string.Equals(
                    asignacionVehiculo
                        .Vehiculo
                        .Estado,

                    "Activo",

                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "El vehículo se encuentra inactivo."
                });
            }


            // =============================================
            // EVITAR DOBLE ASIGNACIÓN DEL PEDIDO
            // =============================================

            var pedidoYaAsignado =
                await _context
                    .Asignacion_Pedidos
                    .AnyAsync(a =>
                        a.id_pedido ==
                            dto.IdPedido &&
                        a.Estado ==
                            EstadosAsignacionPedido
                                .Asignado
                    );


            if (pedidoYaAsignado)
            {
                return Conflict(new
                {
                    message =
                        "Este pedido ya está asignado a un distribuidor."
                });
            }



            // =============================================
            // PERSONAL ACTIVO DEL VEHÍCULO
            // =============================================
            //
            // Guardamos una fotografía histórica del personal
            // que estaba asignado al vehículo en este momento.
            // Esto evita que un pedido antiguo muestre el
            // personal actual del vehículo.
            // =============================================

            var personalActivoVehiculo =
                await _context
                    .Asignacion_Vehiculos

                    .AsNoTracking()

                    .Where(a =>
                        a.id_vehiculo ==
                            asignacionVehiculo.id_vehiculo
                        &&
                        a.Estado == "Activo"
                        &&
                        a.Usuario.Estado == "Activo"
                    )

                    .ToListAsync();


            if (personalActivoVehiculo.Count == 0)
            {
                return BadRequest(new
                {
                    message =
                        "El vehículo seleccionado no tiene personal activo asignado."
                });
            }


            // =============================================
            // CREAR ASIGNACIÓN
            // =============================================

            var asignacionPedido =
                new Asignacion_Pedido
                {
                    id_asignacion_vehiculo =
                        dto.IdAsignacionVehiculo,

                    id_pedido =
                        dto.IdPedido,

                    Fecha_Asignacion =
                        DateTime.UtcNow,

                    Fecha_Entrega =
                        null,

                    Estado =
                        EstadosAsignacionPedido
                            .Asignado
                };



            // =============================================
            // GUARDAR PERSONAL HISTÓRICO DEL PEDIDO
            // =============================================

            foreach (
                var personal
                in personalActivoVehiculo
            )
            {
                asignacionPedido
                    .Asignacion_Pedido_Usuarios
                    .Add(
                        new Asignacion_Pedido_Usuario
                        {
                            Id_asignacion_vehiculo =
                                personal.Id,

                            Fecha =
                                asignacionPedido
                                    .Fecha_Asignacion,

                            Estado =
                                "Activo"
                        }
                    );
            }


            // =============================================
            // CAMBIAR PEDIDO A ASIGNADO
            // =============================================

            pedido.Estado =
                EstadosPedido.Asignado;


            await _context
                .Asignacion_Pedidos
                .AddAsync(
                    asignacionPedido
                );


            await _context
                .SaveChangesAsync();


            return Ok(new
            {
                message =
                    "Pedido asignado al distribuidor correctamente.",

                asignacion = new
                {
                    id =
                        asignacionPedido.Id,

                    idPedido =
                        asignacionPedido
                            .id_pedido,

                    idAsignacionVehiculo =
                        asignacionPedido
                            .id_asignacion_vehiculo,

                    idUsuario =
                        asignacionVehiculo
                            .id_usuario,

                    usuario =
                        asignacionVehiculo
                            .Usuario
                            .Nombre,

                    idVehiculo =
                        asignacionVehiculo
                            .id_vehiculo,

                    vehiculo =
                        asignacionVehiculo
                            .Vehiculo
                            .Marca,

                    placa =
                        asignacionVehiculo
                            .Vehiculo
                            .Placa,

                    fechaAsignacion =
                        asignacionPedido
                            .Fecha_Asignacion,

                    estadoAsignacion =
                        asignacionPedido
                            .Estado,

                    estadoPedido =
                        pedido.Estado
                }
            });
        }


        // =====================================================
        // EDITAR ASIGNACIÓN
        // =====================================================

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Editar(
          int id,
          [FromBody] AsignacionPedidoUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var asignacionPedido =
                await _context.Asignacion_Pedidos

                    .Include(a =>
                        a.Pedido
                    )

                    .Include(a =>
                        a.Asignacion_Pedido_Usuarios
                    )

                    .FirstOrDefaultAsync(
                        a => a.Id == id
                    );

            if (asignacionPedido == null)
            {
                return NotFound(new
                {
                    message =
                        "La asignación de pedido no existe."
                });
            }

            /*
             * Solo puede cambiarse el vehículo
             * mientras siga en estado Asignado.
             */
            if (
                asignacionPedido.Estado !=
                EstadosAsignacionPedido.Asignado
            )
            {
                return BadRequest(new
                {
                    message =
                        "Solo se puede modificar una asignación que esté en estado Asignado."
                });
            }

            if (
                asignacionPedido.Pedido == null ||
                asignacionPedido.Pedido.Estado !=
                EstadosPedido.Asignado
            )
            {
                return BadRequest(new
                {
                    message =
                        "El pedido ya no se encuentra en estado Asignado."
                });
            }

            /*
             * En este caso NO permitimos cambiar
             * el pedido desde el frontend.
             *
             * Debe seguir siendo el mismo pedido.
             */
            if (
                dto.IdPedido !=
                asignacionPedido.id_pedido
            )
            {
                return BadRequest(new
                {
                    message =
                        "No se puede cambiar el pedido de esta asignación."
                });
            }

            /*
             * Buscar la nueva asignación
             * usuario-vehículo.
             */
            var nuevaAsignacionVehiculo =
                await _context.Asignacion_Vehiculos
                    .Include(a => a.Usuario)
                    .Include(a => a.Vehiculo)
                    .FirstOrDefaultAsync(
                        a =>
                            a.Id ==
                            dto.IdAsignacionVehiculo
                    );

            if (nuevaAsignacionVehiculo == null)
            {
                return NotFound(new
                {
                    message =
                        "La asignación de vehículo seleccionada no existe."
                });
            }

            /*
             * Debe continuar activa.
             */
            if (
                !string.Equals(
                    nuevaAsignacionVehiculo.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "La asignación de vehículo seleccionada está inactiva."
                });
            }

            if (
                nuevaAsignacionVehiculo.Vehiculo == null ||
                !string.Equals(
                    nuevaAsignacionVehiculo.Vehiculo.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "El vehículo seleccionado se encuentra inactivo."
                });
            }

            if (
                nuevaAsignacionVehiculo.Usuario == null ||
                !string.Equals(
                    nuevaAsignacionVehiculo.Usuario.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "La asignación seleccionada no tiene un usuario activo."
                });
            }

            // =============================================
            // PERSONAL ACTIVO DEL NUEVO VEHÍCULO
            // =============================================

            var nuevoPersonalVehiculo =
                await _context
                    .Asignacion_Vehiculos

                    .AsNoTracking()

                    .Where(a =>
                        a.id_vehiculo ==
                            nuevaAsignacionVehiculo.id_vehiculo
                        &&
                        a.Estado == "Activo"
                        &&
                        a.Usuario.Estado == "Activo"
                    )

                    .ToListAsync();


            if (nuevoPersonalVehiculo.Count == 0)
            {
                return BadRequest(new
                {
                    message =
                        "El vehículo seleccionado no tiene personal activo asignado."
                });
            }


            /*
             * Cambiar únicamente la asignación
             * usuario-vehículo.
             *
             * El pedido sigue siendo el mismo.
             */
            asignacionPedido.id_asignacion_vehiculo =
                dto.IdAsignacionVehiculo;


            // =============================================
            // REEMPLAZAR PERSONAL HISTÓRICO
            // =============================================
            //
            // Esto solamente puede ocurrir mientras el pedido
            // siga en estado Asignado. Una vez EnCamino,
            // este método ya no permite editar.
            // =============================================

            _context
                .Asignacion_Pedido_Usuarios
                .RemoveRange(
                    asignacionPedido
                        .Asignacion_Pedido_Usuarios
                );


            asignacionPedido
                .Asignacion_Pedido_Usuarios
                .Clear();


            foreach (
                var personal
                in nuevoPersonalVehiculo
            )
            {
                asignacionPedido
                    .Asignacion_Pedido_Usuarios
                    .Add(
                        new Asignacion_Pedido_Usuario
                        {
                            Id_asignacion_vehiculo =
                                personal.Id,

                            Fecha =
                                asignacionPedido
                                    .Fecha_Asignacion,

                            Estado =
                                "Activo"
                        }
                    );
            }


            /*
             * No modificamos:
             *
             * Fecha_Asignacion
             * Fecha_Entrega
             * Estado
             *
             * porque solo estamos cambiando
             * el vehículo encargado del pedido.
             */

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Vehículo asignado al pedido actualizado correctamente.",

                asignacion = new
                {
                    id =
                        asignacionPedido.Id,

                    idPedido =
                        asignacionPedido.id_pedido,

                    idAsignacionVehiculo =
                        asignacionPedido
                            .id_asignacion_vehiculo,

                    idVehiculo =
                        nuevaAsignacionVehiculo
                            .id_vehiculo,

                    vehiculo =
                        nuevaAsignacionVehiculo
                            .Vehiculo.Marca,

                    placa =
                        nuevaAsignacionVehiculo
                            .Vehiculo.Placa,

                    estado =
                        asignacionPedido.Estado
                }
            });
        }

        // =====================================================
        // DISTRIBUIDOR MARCA ENTREGADO
        // =====================================================

        /*
         * El distribuidor pulsa:
         *
         * "Pedido entregado"
         *
         *
         * Asignacion_Pedido:
         *
         * Asignado
         * ↓
         * Entregado
         *
         *
         * Pedido:
         *
         * Asignado
         * ↓
         * PorConfirmarEntrega
         *
         *
         * PATCH:
         *
         * api/AsignacionPedido/5/Entregar
         */
        [HttpPatch("{id:int}/Entregar")]
        public async Task<IActionResult>
            MarcarComoEntregado(
                int id)
        {
            var asignacion =
                await _context
                    .Asignacion_Pedidos

                    .Include(a =>
                        a.Pedido
                    )

                    .FirstOrDefaultAsync(
                        a =>
                            a.Id == id
                    );


            if (asignacion == null)
            {
                return NotFound(new
                {
                    message =
                        "La asignación de pedido no existe."
                });
            }


            if (
                asignacion.Estado ==
                EstadosAsignacionPedido
                    .Entregado
            )
            {
                return BadRequest(new
                {
                    message =
                        "Esta asignación ya fue marcada como entregada."
                });
            }


            if (
                asignacion.Estado !=
                EstadosAsignacionPedido
                    .Asignado
            )
            {
                return BadRequest(new
                {
                    message =
                        "Solamente una asignación en estado Asignado puede marcarse como entregada."
                });
            }


            if (asignacion.Pedido == null)
            {
                return BadRequest(new
                {
                    message =
                        "No se encontró el pedido asociado."
                });
            }


            // =============================================
            // ASIGNACIÓN = ENTREGADO
            // =============================================

            asignacion.Estado =
                EstadosAsignacionPedido
                    .Entregado;


            asignacion.Fecha_Entrega =
                DateTime.UtcNow;


            // =============================================
            // PEDIDO ESPERA AL CLIENTE
            // =============================================

            asignacion.Pedido.Estado =
                EstadosPedido
                    .PorConfirmarEntrega;


            await _context
                .SaveChangesAsync();


            return Ok(new
            {
                message =
                    "El distribuidor marcó el pedido como entregado. Falta la confirmación del cliente.",

                idAsignacion =
                    asignacion.Id,

                idPedido =
                    asignacion.id_pedido,

                estadoAsignacion =
                    asignacion.Estado,

                fechaEntrega =
                    asignacion.Fecha_Entrega,

                estadoPedido =
                    asignacion
                        .Pedido
                        .Estado
            });
        }

        // =====================================================
        // CONSULTA COMÚN
        // =====================================================

        private IQueryable<Asignacion_Pedido>
            ConstruirConsulta()
        {
            return _context
                .Asignacion_Pedidos

                .AsNoTracking()

                .AsSplitQuery()

                // =================================================
                // ASIGNACIÓN PRINCIPAL
                // =================================================

                .Include(a =>
                    a.Asignacion_vehiculo
                )

                .ThenInclude(av =>
                    av.Usuario
                )

                .Include(a =>
                    a.Asignacion_vehiculo
                )

                .ThenInclude(av =>
                    av.Vehiculo
                )

                // =================================================
                // PERSONAL HISTÓRICO DEL PEDIDO
                // =================================================
                //
                // IMPORTANTE:
                //
                // Ya no dependemos del personal activo actual
                // del vehículo para mostrar el historial.
                //
                // Aquí cargamos las asignaciones que quedaron
                // guardadas cuando se creó el pedido.
                // =================================================

                .Include(a =>
                    a.Asignacion_Pedido_Usuarios
                )

                .ThenInclude(h =>
                    h.AsignacionVehiculo
                )

                .ThenInclude(av =>
                    av.Usuario
                )

                // =================================================
                // PEDIDO
                // =================================================

                .Include(a =>
                    a.Pedido
                );
        }


        // =====================================================
        // MAPEAR DTO
        // =====================================================

        private static
            AsignacionPedidoResponseDto
            MapearAsignacion(
                Asignacion_Pedido a)
        {
            return new
                AsignacionPedidoResponseDto
            {
                Id =
                    a.Id,


                IdAsignacionVehiculo =
                    a.id_asignacion_vehiculo,


                // =================================================
                // USUARIO PRINCIPAL
                // =================================================
                //
                // Se conserva para no romper el frontend actual.
                // =================================================

                IdUsuario =
                    a.Asignacion_vehiculo
                        .id_usuario,


                Usuario =
                    a.Asignacion_vehiculo
                        .Usuario
                        .Nombre,


                CorreoUsuario =
                    a.Asignacion_vehiculo
                        .Usuario
                        .Email,


                // =================================================
                // VEHÍCULO
                // =================================================

                IdVehiculo =
                    a.Asignacion_vehiculo
                        .id_vehiculo,


                Vehiculo =
                    a.Asignacion_vehiculo
                        .Vehiculo
                        .Marca,


                Placa =
                    a.Asignacion_vehiculo
                        .Vehiculo
                        .Placa,


                // =================================================
                // PEDIDO
                // =================================================

                IdPedido =
                    a.id_pedido,


                IdCliente =
                    a.Pedido
                        .Id_cliente,


                TotalPedido =
                    a.Pedido
                        .Total,


                // =================================================
                // FECHAS
                // =================================================

                FechaAsignacion =
                    a.Fecha_Asignacion,


                FechaEntrega =
                    a.Fecha_Entrega,


                // =================================================
                // ESTADOS
                // =================================================

                EstadoAsignacion =
                    a.Estado,


                EstadoPedido =
                    a.Pedido
                        .Estado,


                // =================================================
                // PERSONAL HISTÓRICO
                // =================================================

                PersonalAsignado =
                    a.Asignacion_Pedido_Usuarios

                        .Where(h =>
                            h.Estado ==
                            "Activo"
                        )

                        .OrderBy(h =>
                            h.AsignacionVehiculo
                                .Usuario
                                .Nombre
                        )

                        .Select(h =>
                            new PersonalAsignacionPedidoDto
                            {
                                IdAsignacionVehiculo =
                                    h.Id_asignacion_vehiculo,


                                IdUsuario =
                                    h.AsignacionVehiculo
                                        .id_usuario,


                                Usuario =
                                    h.AsignacionVehiculo
                                        .Usuario
                                        .Nombre,


                                CorreoUsuario =
                                    h.AsignacionVehiculo
                                        .Usuario
                                        .Email
                            }
                        )

                        .ToList()
            };
        }
    }
}