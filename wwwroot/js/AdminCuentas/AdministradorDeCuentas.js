let txtNumeroServicio = $("#txtNumeroServicio");
let txtNombreServicio = $("#txtNombreServicio");
let txtTotalPagar = $("#txtTotalPagar");
let txtNombreCorto = $("#txtNombreCorto");
let btnRegistrarServicio = $("#btnRegistrarServicio");
var tabla;

const img = $("#imgZoom");

let objCuentas = {
    username: "alexis",
    password: "1234"
};
let objDatosValidacion = {
    alias: "VALIDA_CUENTA",
    filtros: [
        {
            id_cuenta: 18,
            nombre: "Abrego Consuelo",
            total: 1000
        }
    ]
};
let objConsultarCuentas = {
    ID_USUARIO_WEB: 0,
};
let objActionsCuentaUsuario = {
    ACCION: '',
    ID_USUARIO_WEB: 0,
    CUENTA_COMAPA: 18,
    MOTIVO_BAJA: '',
    ID_IMAGEN_AUTORIZA_PAPERLESS: 0,
    ALIAS_CUENTA: '',
    Nombre_Propietario: '', 
    PAGO_TOTAL: 0
};

$(document).ready(function () {
    LoginCuentas();
    ConsultarCuentasUsuario();
    $("#txtNombreServicio").on("input", function () {
        $(this).val($(this).val().toUpperCase());
    });
});
//eventos
btnRegistrarServicio.on('click', function () {
    if (ValidarFormularioCuenta() === 'valido') {
        //console.log('Formulario válido');
        objActionsCuentaUsuario.ACCION = 'ALTA';
        objActionsCuentaUsuario.CUENTA_COMAPA = txtNumeroServicio.val();
        objActionsCuentaUsuario.Nombre_Propietario = txtNombreServicio.val();
        objActionsCuentaUsuario.PAGO_TOTAL = txtTotalPagar.val();
        objActionsCuentaUsuario.ALIAS_CUENTA = txtNombreCorto.val();
        objDatosValidacion.filtros[0].id_cuenta = txtNumeroServicio.val();
        objDatosValidacion.filtros[0].nombre = txtNombreServicio.val();
        objDatosValidacion.filtros[0].total = txtTotalPagar.val();
        ValidarCuentasUsuarios();
    } else {
        Swal.fire({ title: "ERROR!!", text: `${ValidarFormularioCuenta()}`, icon: "error" });
    }
});

$("#btnMostrarRegistro").click(function () {

    $("#contenedorTabla").hide();
    $("#contenedorRegistro").fadeIn(200);

    $("#btnMostrarRegistro")
        .removeClass("btn-outline-secondary")
        .addClass("btn-principal");

    $("#btnMostrarTabla")
        .removeClass("btn-principal")
        .addClass("btn-outline-secondary");

});

$("#btnMostrarTabla").click(function () {

    $("#contenedorRegistro").hide();
    $("#contenedorTabla").fadeIn(200);

    $("#btnMostrarTabla")
        .removeClass("btn-outline-secondary")
        .addClass("btn-principal");

    $("#btnMostrarRegistro")
        .removeClass("btn-principal")
        .addClass("btn-outline-secondary");

});

img.on('mousemove', function (e) {
    const rect = this.getBoundingClientRect();
    const X = ((e.clientX - rect.left) / rect.width) * 100;
    const Y = ((e.clientY - rect.top) / rect.height) * 100;

    $(this).css({
        "transform-origin": `${X}% ${Y}%`,
        "transform": "scale(1.5)"
    });
});

img.on("mouseleave", function () {

    $(this).css({
        "transform-origin": "center center",
        "transform": "scale(1)"
    });

});

$('#tablaCuentas').DataTable({
    responsive: true,
    language: {
        url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-MX.json'
    }
});

//$(document).on('click', '.btn-editarCuenta', function () {

//    const idUsuario = $(this).data('id');
//    const tabla = $(this).data('tabla');

//    console.log({ idUsuario, tabla });
//});

$(document).on('click', '.btn-eliminarCuenta', function () {

    const idUsuario = $(this).data('id');
    //const tabla = $(this).data('tabla');

    Swal.fire({
        title: "Atencion, estas seguro ?",
        text: `Deseas Eliminar la cuenta: ${idUsuario} ?`,
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#bfd9f2",
        confirmButtonText: "Si, seguro!",
        cancelButtonText: "No"
    }).then((result) => {
        if (result.isConfirmed) {
            if (idUsuario) {
                objActionsCuentaUsuario.CUENTA_COMAPA = idUsuario;
                objActionsCuentaUsuario.ACCION = 'BAJA';
                objActionsCuentaUsuario.MOTIVO_BAJA = 'Portal Comapa';
                ActionsCuentaUsuario();
            }
        } else {
            $('.select-usuario').val(0)
        }
    });
});

//funciones
function ValidarFormularioCuenta() {
    if (txtNumeroServicio.val() < 0 || txtNumeroServicio.val() === '') return 'Ingresa un numero valido por favor';
    if (txtNombreServicio.val() === '') return 'Ingresa un nombre valido por favor';
    if (txtTotalPagar.val() < 0 || txtTotalPagar.val() === '') return 'Ingresa un valor valido por favor';
    if (txtNombreCorto.val() === '') return 'ingresa un nombre corto por favor';
    return 'valido';
};

function LimpiarFormularioCuenta() {
    txtNumeroServicio.val('');
    txtNombreServicio.val('');
    txtTotalPagar.val('');
    txtNombreCorto.val('');
}

//tablas
function LLenarTabla(data, Tab) { 

    if ($.fn.DataTable.isDataTable(`#${Tab}`)) {
        //$(`#${Tab}`).DataTable().clear().destroy();
        $(`#${Tab}`).DataTable().clear().destroy();
    }

    tabla = $(`#${Tab}`).DataTable({
        "bLengthChange": false,
        scrollX: false,
        data: data,
        paging: false,
        select: false,
        "autoWidth": false,
        order: [],
        pageLength: 7,
        searching: false,
        info: false,
        columns: [
            { "data": "cuentA_COMAPA", "visible": true },
            { "data": "aliaS_CUENTA", "visible": true },
            //{
            //    title: 'Editar',
            //    data: null,
            //    orderable: false,
            //    render: function (data, type, row) {
            //        return `
            //            <center><button type="button"
            //                    class="btn-editarCuenta btn btn-primary"
            //                    data-id='${JSON.stringify(row)}'
            //                    data-tabla="${Tab}">
            //                <i class="fas fa-edit"></i>
            //            </button></center>`;
            //    }
            //},
            {
                title: 'Eliminar',
                data: null,
                orderable: false,
                render: function (data, type, row) {
                    return `
                        <center><button type="button"
                                class="btn-eliminarCuenta btn btn-danger"
                                data-id="${row.cuentA_COMAPA}"
                                data-tabla="${Tab}">
                            <i class="fas fa-trash"></i>
                        </button></center>`;
                }
            }
        ], 
        columnDefs: [

        ],
        language: {
            "decimal": "",
            "emptyTable": "No hay información",
            "info": "Mostrando _START_ a _END_ de _TOTAL_ Entradas",
            "infoEmpty": "Mostrando 0 to 0 of 0 Entradas",
            "infoFiltered": "(Filtrado de _MAX_ total entradas)",
            "infoPostFix": "",
            "thousands": ",",
            "lengthMenu": "Mostrar _MENU_ Entradas",
            "loadingRecords": "Cargando...",
            "processing": "Procesando...",
            "search": "Buscar:",
            "zeroRecords": "Sin resultados encontrados",
            "paginate": {
                "first": "Primero",
                "last": "Ultimo",
                "next": "<i class='mdi mdi-chevron-right'>",
                "previous": "<i class='mdi mdi-chevron-left'>"
            }
        },
    });
};

//AJAX
function LoginCuentas() {// obtiene el token 
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/AdministradorDeCuentas/LoginCuentas',
        type: "POST",
        data: JSON.stringify(objCuentas),
        cache: false,
        success: function (data) {
            //if (data.ok) { ValidarLoginCuentas();}
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};

function ValidarCuentasUsuarios() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/AdministradorDeCuentas/ValidarCuentasUsuarios',
        type: "POST",
        data: JSON.stringify(objDatosValidacion),
        cache: false,
        success: function (data) {
            if (data.body[0].usrValido) {
                ActionsCuentaUsuario();
            } else {
                Swal.fire({ title: "ERROR!!", text: `NO SE ENCONTRO NINGUNA CUENTA CON LOS DATOS PROPORCIONADOS!`, icon: "error" });
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INTENTAR VERIFICAR LA CUENTA!`, icon: "error" });
        }
    });
};

function ConsultarCuentasUsuario() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/AdministradorDeCuentas/ConsultarCuentasUsuario',
        type: "POST",
        data: JSON.stringify(objConsultarCuentas),
        cache: false,
        success: function (data) {
            if (data.exito) {
                //console.log(data);
                LLenarTabla(data.datos, 'tablaCuentas');
            } else {
                //Swal.fire({ title: "ERROR!!", text: `${data.mensaje}`, icon: "error" });
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};

function ActionsCuentaUsuario() {
    let windowChange = objActionsCuentaUsuario.ACCION == 'ALTA' ? true : false;
    //console.log(windowChange);
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/AdministradorDeCuentas/ActionsCuentaUsuario',
        type: "POST",
        data: JSON.stringify(objActionsCuentaUsuario),
        cache: false,
        success: function (data) {
            if (data.exito) {
                ConsultarCuentasUsuario()
                Swal.fire({ title: "Aviso", text: `Accion: realizada con exito !!`, icon: "success" });
                LimpiarFormularioCuenta();
                if (windowChange) { $("#btnMostrarTabla").trigger("click"); }
            } else {
                Swal.fire({ title: "ERROR!!", text: `${data.mensaje}`, icon: "error" });
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};