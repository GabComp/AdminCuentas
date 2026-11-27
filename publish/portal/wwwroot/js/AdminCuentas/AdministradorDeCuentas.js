let txtNumeroServicio = $("#txtNumeroServicio");
let txtNombreServicio = $("#txtNombreServicio");
let txtTotalPagar = $("#txtTotalPagar");
let txtNombreCorto = $("#txtNombreCorto");
let btnRegistrarServicio = $("#btnRegistrarServicio");
let txtCodigoVinculacion = $("#txtCodigoVinculacion");
let grupoCodigoVinculacion = $("#grupoCodigoVinculacion");
let btnReenviarCodigoVinculacion = $("#btnReenviarCodigoVinculacion");

let etapaVinculacion = 'datos';

const img = $("#imgZoom");

function escaparHtml(valor) {
    return String(valor ?? '').replace(/[&<>"']/g, function (caracter) {
        return {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }[caracter];
    });
}

let objDatosValidacion = {
    alias: "VALIDA_CUENTA",
    filtros: [
        {
            id_cuenta: 0,
            nombre: "",
            total: 0
        }
    ]
};
let objConsultarCuentas = {
    ID_USUARIO_WEB: 0,
};
let objActionsCuentaUsuario = {
    ACCION: '',
    ID_USUARIO_WEB: 0,
    CUENTA_COMAPA: 0,
    MOTIVO_BAJA: '',
    ID_IMAGEN_AUTORIZA_PAPERLESS: 0,
    ALIAS_CUENTA: '',
    Nombre_Propietario: '', 
    PAGO_TOTAL: 0
};

$(document).ready(function () {
    ConsultarCuentasUsuario();
    $("#txtNombreServicio").on("input", function () {
        $(this).val($(this).val().toUpperCase());
    });
});
//eventos
btnRegistrarServicio.on('click', function () {
    if (etapaVinculacion === 'datos') {
        if (ValidarFormularioCuenta() !== 'valido') {
            Swal.fire({ title: "ERROR!!", text: `${ValidarFormularioCuenta()}`, icon: "error" });
            return;
        }

        objActionsCuentaUsuario.ACCION = 'ALTA';
        objActionsCuentaUsuario.CUENTA_COMAPA = txtNumeroServicio.val();
        objActionsCuentaUsuario.Nombre_Propietario = txtNombreServicio.val();
        objActionsCuentaUsuario.PAGO_TOTAL = txtTotalPagar.val();
        objActionsCuentaUsuario.ALIAS_CUENTA = txtNombreCorto.val();
        objDatosValidacion.filtros[0].id_cuenta = txtNumeroServicio.val();
        objDatosValidacion.filtros[0].nombre = txtNombreServicio.val();
        objDatosValidacion.filtros[0].total = txtTotalPagar.val();

        EnviarOtpVinculacion();
        return;
    }

    // Etapa código: verificar OTP y completar el vínculo.
    const codigo = txtCodigoVinculacion.val().trim();
    if (!/^\d{6}$/.test(codigo)) {
        Swal.fire({ title: "ERROR!!", icon: "error", text: "Ingresa el código de 6 dígitos." });
        return;
    }

    objActionsCuentaUsuario.CODIGO = codigo;
    ValidarCuentasUsuarios();
});

btnReenviarCodigoVinculacion.on('click', function (e) {
    e.preventDefault();
    EnviarOtpVinculacion();
});

txtCodigoVinculacion.on('input', function () {
    this.value = this.value.replace(/[^0-9]/g, '').substring(0, 6);
});

function resetVinculacion() {
    etapaVinculacion = 'datos';
    grupoCodigoVinculacion.addClass('d-none');
    txtCodigoVinculacion.val('');
    btnRegistrarServicio.text('Registrar');
    objActionsCuentaUsuario.CODIGO = '';

    [txtNumeroServicio, txtNombreServicio, txtTotalPagar, txtNombreCorto].forEach(function (el) {
        el.prop('disabled', false);
    });
}

function EnviarOtpVinculacion() {
    btnRegistrarServicio.prop('disabled', true);

    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/AdministradorDeCuentas/EnviarOtpVinculacion',
        type: "POST",
        data: JSON.stringify({}),
        cache: false,
        success: function (data) {
            if (data && data.exito) {
                etapaVinculacion = 'codigo';
                grupoCodigoVinculacion.removeClass('d-none');
                txtCodigoVinculacion.focus();
                btnRegistrarServicio.text('Verificar y vincular');

                [txtNumeroServicio, txtNombreServicio, txtTotalPagar, txtNombreCorto].forEach(function (el) {
                    el.prop('disabled', true);
                });

                if (data.codigoDepuracion) {
                    Swal.fire({
                        title: 'Código (solo desarrollo)',
                        text: `Tu código es: ${data.codigoDepuracion}`,
                        icon: 'info',
                        confirmButtonText: 'OK'
                    });
                } else {
                    Swal.fire({
                        title: 'Código enviado',
                        text: data.mensaje || 'Revisa tu correo.',
                        icon: 'success',
                        timer: 2500,
                        showConfirmButton: false
                    });
                }

                btnRegistrarServicio.prop('disabled', false);
            } else {
                btnRegistrarServicio.prop('disabled', false);
                Swal.fire({
                    title: "ERROR!!",
                    text: (data && data.mensaje) || 'No fue posible enviar el código.',
                    icon: "error"
                });
            }
        },
        error: function () {
            btnRegistrarServicio.prop('disabled', false);
            Swal.fire({ title: "ERROR!!", text: 'OCURRIÓ UN ERROR AL ENVIAR EL CÓDIGO.', icon: "error" });
        }
    });
}

$("#btnMostrarRegistro").click(function () {

    $("#contenedorTabla").hide();
    $("#contenedorRegistro").fadeIn(200);

    $("#btnMostrarRegistro")
        .removeClass("btn-outline-secondary")
        .addClass("btn-principal")
        .attr("aria-pressed", "true");

    $("#btnMostrarTabla")
        .removeClass("btn-principal")
        .addClass("btn-outline-secondary")
        .attr("aria-pressed", "false");

});

$("#btnMostrarTabla").click(function () {

    $("#contenedorRegistro").hide();
    $("#contenedorTabla").fadeIn(200);

    $("#btnMostrarTabla")
        .removeClass("btn-outline-secondary")
        .addClass("btn-principal")
        .attr("aria-pressed", "true");

    $("#btnMostrarRegistro")
        .removeClass("btn-principal")
        .addClass("btn-outline-secondary")
        .attr("aria-pressed", "false");

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

// La tabla se renderiza en LLenarTabla() sin dependencias externas.

$(document).on('click', '.btn-editarCuenta', function () {
    const cuenta = $(this).data('id');
    const aliasActual = $(this).data('alias') || '';

    Swal.fire({
        title: 'Editar alias',
        input: 'text',
        inputValue: aliasActual,
        inputPlaceholder: 'Ej. Casa',
        inputAttributes: { maxlength: 40, 'aria-label': 'Alias de la cuenta' },
        showCancelButton: true,
        confirmButtonText: 'Guardar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#691C32',
        inputValidator: (valor) => {
            const texto = (valor || '').trim();
            if (!texto) return 'Ingresa un alias.';
            if (texto.length > 40) return 'El alias debe tener máximo 40 caracteres.';
            return null;
        }
    }).then((result) => {
        if (!result.isConfirmed) {
            return;
        }

        objActionsCuentaUsuario.ACCION = 'ACT';
        objActionsCuentaUsuario.CUENTA_COMAPA = cuenta;
        objActionsCuentaUsuario.ALIAS_CUENTA = (result.value || '').trim();

        ActionsCuentaUsuario();
    });
});

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

// Tabla de cuentas (render simple, sin dependencias externas)
function LLenarTabla(data) {
    const tbody = document.querySelector('#tablaCuentas tbody');
    if (!tbody) return;

    tbody.innerHTML = '';

    if (!data || data.length === 0) {
        const fila = document.createElement('tr');
        fila.innerHTML = `
            <td colspan="4" class="text-center text-muted py-4">
                No tienes cuentas vinculadas. Usa "Registrar Cuenta" para agregar una.
            </td>`;
        tbody.appendChild(fila);
        return;
    }

    data.forEach(cuenta => {
        const fila = document.createElement('tr');
        fila.innerHTML = `
            <td>${escaparHtml(cuenta.CUENTA_COMAPA)}</td>
            <td>${escaparHtml(cuenta.ALIAS_CUENTA)}</td>
            <td class="text-center">
                <button type="button"
                        class="btn-editarCuenta btn btn-outline-secondary btn-sm"
                        data-id="${escaparHtml(cuenta.CUENTA_COMAPA)}"
                        data-alias="${escaparHtml(cuenta.ALIAS_CUENTA)}"
                        title="Editar alias de la cuenta ${escaparHtml(cuenta.CUENTA_COMAPA)}"
                        aria-label="Editar alias de la cuenta ${escaparHtml(cuenta.CUENTA_COMAPA)}">
                    <i class="fas fa-pen"></i>
                </button>
            </td>
            <td class="text-center">
                <button type="button"
                        class="btn-eliminarCuenta btn btn-danger btn-sm"
                        data-id="${escaparHtml(cuenta.CUENTA_COMAPA)}"
                        title="Eliminar cuenta ${escaparHtml(cuenta.CUENTA_COMAPA)}"
                        aria-label="Eliminar cuenta ${escaparHtml(cuenta.CUENTA_COMAPA)}">
                    <i class="fas fa-trash"></i>
                </button>
            </td>`;
        tbody.appendChild(fila);
    });
};

//AJAX
function ValidarCuentasUsuarios() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/AdministradorDeCuentas/ValidarCuentasUsuarios',
        type: "POST",
        data: JSON.stringify(objDatosValidacion),
        cache: false,
        success: function (data) {
            if (data.ok && data.body && data.body.length > 0 && data.body[0].usrValido) {
                ActionsCuentaUsuario();
            } else {
                resetVinculacion();
                btnRegistrarServicio.prop('disabled', false);
                Swal.fire({ title: "ERROR!!", text: `NO SE ENCONTRO NINGUNA CUENTA CON LOS DATOS PROPORCIONADOS!`, icon: "error" });
            }
        },
        error: function (xhr) {
            const mensaje = xhr.responseJSON && xhr.responseJSON.message
                ? xhr.responseJSON.message
                : 'OCURRIO UN ERROR AL INTENTAR VERIFICAR LA CUENTA!';
            resetVinculacion();
            btnRegistrarServicio.prop('disabled', false);
            Swal.fire({ title: "ERROR!!", text: mensaje, icon: "error" });
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
                LLenarTabla(data.datos);
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
                ConsultarCuentasUsuario();
                Swal.fire({ title: "Aviso", text: `Acción realizada con éxito.`, icon: "success" });
                LimpiarFormularioCuenta();
                resetVinculacion();
                if (windowChange) { $("#btnMostrarTabla").trigger("click"); }
            } else {
                resetVinculacion();
                btnRegistrarServicio.prop('disabled', false);
                Swal.fire({ title: "ERROR!!", text: `${data.mensaje}`, icon: "error" });
            }
        },
        error: function () {
            resetVinculacion();
            btnRegistrarServicio.prop('disabled', false);
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};