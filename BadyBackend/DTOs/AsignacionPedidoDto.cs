using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs.AsignacionPedido
{
    public class AsignacionPedidoCreateDto
    {
        [Required]
        [Range(
            1,
            int.MaxValue,
            ErrorMessage =
                "Debe seleccionar una asignación de vehículo."
        )]
        public int IdAsignacionVehiculo
        {
            get;
            set;
        }


        [Required]
        [Range(
            1,
            int.MaxValue,
            ErrorMessage =
                "Debe seleccionar un pedido."
        )]
        public int IdPedido
        {
            get;
            set;
        }


        // =====================================================
        // DTO PARA EDITAR ASIGNACIÓN
        // =====================================================

        public class AsignacionPedidoUpdateDto
        {
            [Required]
            [Range(
                1,
                int.MaxValue,
                ErrorMessage =
                    "Debe seleccionar una asignación de vehículo."
            )]
            public int IdAsignacionVehiculo
            {
                get;
                set;
            }


            [Required]
            [Range(
                1,
                int.MaxValue,
                ErrorMessage =
                    "Debe seleccionar un pedido."
            )]
            public int IdPedido
            {
                get;
                set;
            }
        }


        // =====================================================
        // PERSONAL HISTÓRICO DEL PEDIDO
        // =====================================================

        public class PersonalAsignacionPedidoDto
        {
            public int IdAsignacionVehiculo
            {
                get;
                set;
            }


            public int IdUsuario
            {
                get;
                set;
            }


            public string Usuario
            {
                get;
                set;
            } = string.Empty;


            public string? CorreoUsuario
            {
                get;
                set;
            }
        }


        // =====================================================
        // RESPUESTA DE ASIGNACIÓN
        // =====================================================

        public class AsignacionPedidoResponseDto
        {
            public int Id
            {
                get;
                set;
            }


            public int IdAsignacionVehiculo
            {
                get;
                set;
            }


            // =================================================
            // USUARIO PRINCIPAL
            // =================================================
            //
            // Se mantiene para no romper el frontend actual.
            //

            public int IdUsuario
            {
                get;
                set;
            }


            public string Usuario
            {
                get;
                set;
            } = string.Empty;


            public string? CorreoUsuario
            {
                get;
                set;
            }


            // =================================================
            // VEHÍCULO
            // =================================================

            public int IdVehiculo
            {
                get;
                set;
            }


            public string Vehiculo
            {
                get;
                set;
            } = string.Empty;


            public string? Placa
            {
                get;
                set;
            }


            // =================================================
            // PEDIDO
            // =================================================

            public int IdPedido
            {
                get;
                set;
            }


            public int IdCliente
            {
                get;
                set;
            }


            public decimal TotalPedido
            {
                get;
                set;
            }


            // =================================================
            // FECHAS
            // =================================================

            public DateTime FechaAsignacion
            {
                get;
                set;
            }


            public DateTime? FechaEntrega
            {
                get;
                set;
            }


            // =================================================
            // ESTADOS
            // =================================================

            public string EstadoAsignacion
            {
                get;
                set;
            } = string.Empty;


            public string EstadoPedido
            {
                get;
                set;
            } = string.Empty;


            // =================================================
            // PERSONAL HISTÓRICO
            // =================================================

            public List<PersonalAsignacionPedidoDto>
                PersonalAsignado
            {
                get;
                set;
            } = new();
        }
    }
}