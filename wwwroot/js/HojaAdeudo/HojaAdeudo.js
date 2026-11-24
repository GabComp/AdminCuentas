let SelectCuentaHojaAdeudo = $("#SelectCuentaHojaAdeudo");
let divButtonDownload = $("#divButtonDownload");

$(document).ready(function () {
    if (SelectCuentaHojaAdeudo.length > 0) {
        AsegurarCuentasUsuario();
    }
});

function AsegurarCuentasUsuario() {
    // Asegura que la lista de cuentas de la sesión esté cargada aunque el
    // usuario llegue directo a esta página.
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/AdministradorDeCuentas/ConsultarCuentasUsuario',
        type: "POST",
        data: JSON.stringify({ ID_USUARIO_WEB: 0 }),
        cache: false,
        complete: function () {
            ObtenerCuentasHojaAdeudo();
        }
    });
}

SelectCuentaHojaAdeudo.on('change', function () {
    const cuenta = parseInt(this.value || '0', 10);
    ocultarFormulario();
    $('#constanciaValidacion').addClass('d-none').removeClass('alert-success alert-danger alert-warning').text('');

    if (cuenta > 0) {
        validarConstancia(cuenta);
    }
});

$('#selTipoConstancia').on('change', function () {
    actualizarCamposNotaria();
});

$('#btnSolicitarConstancia').on('click', function () {
    solicitarConstancia();
});

$('#BtnDownload').on('click', function () {
    const rutaPDF = '/docs/HojaAdeudo/Terminos_Condiciones.pdf';
    const enlace = document.createElement('a');
    enlace.href = rutaPDF;
    enlace.download = 'Terminos_Condiciones';
    document.body.appendChild(enlace);
    enlace.click();
    document.body.removeChild(enlace);
});

function actualizarCamposNotaria() {
    const esNotaria = $('#selTipoConstancia').val() === '1';
    $('#grupoNotaria, #grupoDatoNotaria').toggleClass('d-none', !esNotaria);
    $('#txtNotaria, #txtDatoNotaria').prop('required', esNotaria);
}

function ocultarFormulario() {
    $('#formConstancia').addClass('d-none');
    $('#constanciaExito').addClass('d-none');
    $('#btnSolicitarConstancia').prop('disabled', false);
}

function validarConstancia(cuenta) {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/HojaAdeudo/ValidarConstancia',
        type: "POST",
        data: JSON.stringify({ idCuenta: cuenta }),
        cache: false,
        success: function (data) {
            if (!data || !data.exito) {
                mostrarValidacion(data?.mensaje || 'No fue posible validar la cuenta.', 'danger');
                return;
            }

            prefillFormulario(data);

            if (data.puedeRegistrar) {
                mostrarValidacion(data.mensaje || 'La cuenta cumple con los requisitos.', 'success');
                $('#formConstancia').removeClass('d-none');
            } else {
                mostrarValidacion(data.mensaje || 'La cuenta no cumple con los requisitos.', 'warning');
            }
        },
        error: function (xhr) {
            const mensaje = xhr.responseJSON && (xhr.responseJSON.mensaje || xhr.responseJSON.message)
                ? (xhr.responseJSON.mensaje || xhr.responseJSON.message)
                : 'No fue posible validar la cuenta. Intenta de nuevo.';

            if (xhr.status === 401) {
                mostrarValidacion('La sesión expiró. Inicia sesión nuevamente.', 'danger');
                return;
            }

            mostrarValidacion(mensaje, 'danger');
        }
    });
}

function mostrarValidacion(mensaje, tipo) {
    const $alerta = $('#constanciaValidacion');
    $alerta.removeClass('d-none alert-success alert-danger alert-warning')
        .addClass('alert-' + tipo)
        .text(mensaje);
}

function prefillFormulario(data) {
    const nombre = (data.nombre || '').trim();
    const telefono = (data.telefono || '').replace(/\D/g, '');

    if (nombre) {
        $('#txtSolicitante').val(nombre);
    }

    if (telefono.length === 10) {
        $('#txtTelefono').val(telefono);
    }

    actualizarCamposNotaria();
}

function solicitarConstancia() {
    const cuenta = parseInt(SelectCuentaHojaAdeudo.val() || '0', 10);
    const tipo = parseInt($('#selTipoConstancia').val() || '2', 10);
    const solicitante = ($('#txtSolicitante').val() || '').trim();
    const telefono = ($('#txtTelefono').val() || '').replace(/\D/g, '');
    const observacion = ($('#txtObservacion').val() || '').trim();
    const notaria = ($('#txtNotaria').val() || '').trim();
    const datoNotaria = ($('#txtDatoNotaria').val() || '').trim();

    if (cuenta <= 0) {
        return Swal.fire({ title: 'Atención', text: 'Selecciona una cuenta.', icon: 'warning' });
    }

    if (!solicitante) {
        return Swal.fire({ title: 'Atención', text: 'Captura el nombre del solicitante.', icon: 'warning' });
    }

    if (telefono.length !== 10) {
        return Swal.fire({ title: 'Atención', text: 'Captura un teléfono de 10 dígitos.', icon: 'warning' });
    }

    if (!observacion) {
        return Swal.fire({ title: 'Atención', text: 'Captura el motivo de la constancia.', icon: 'warning' });
    }

    if (tipo === 1 && (!notaria || !datoNotaria)) {
        return Swal.fire({
            title: 'Atención',
            text: 'Captura la notaría y el nombre del representante.',
            icon: 'warning'
        });
    }

    const $boton = $('#btnSolicitarConstancia').prop('disabled', true);

    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/HojaAdeudo/SolicitarConstancia',
        type: "POST",
        data: JSON.stringify({
            idCuenta: cuenta,
            idTipoConstancia: tipo,
            solicitante: solicitante,
            telefono: telefono,
            notaria: tipo === 1 ? notaria : '',
            datoNotaria: tipo === 1 ? datoNotaria : '',
            observacion: observacion
        }),
        cache: false,
        success: function (data) {
            if (!data || !data.exito) {
                $boton.prop('disabled', false);
                return Swal.fire({
                    title: 'No se pudo registrar',
                    text: (data && data.mensaje) || 'Intenta de nuevo.',
                    icon: 'error'
                });
            }

            $('#formConstancia').addClass('d-none');
            $('#constanciaValidacion').addClass('d-none');
            $('#lblFolioConstancia').text(
                data.folio
                    ? 'Folio de la solicitud: ' + data.folio
                    : 'Tu solicitud quedó registrada.');

            const total = Number(data.total || 0);
            $('#lblImporteConstancia').text(
                total > 0
                    ? 'Importe a pagar: ' + total.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })
                    : '');

            const cuentaActual = parseInt(SelectCuentaHojaAdeudo.val() || '0', 10);
            if (data.idConstancia && cuentaActual > 0) {
                $('#btnPagarConstanciaPortal')
                    .attr('href', `/pago-en-linea/constancia?idConstancia=${data.idConstancia}&idCuenta=${cuentaActual}`)
                    .removeClass('d-none');
            }

            $('#constanciaExito').removeClass('d-none');
        },
        error: function (xhr) {
            $boton.prop('disabled', false);

            const mensaje = xhr.responseJSON && (xhr.responseJSON.mensaje || xhr.responseJSON.message)
                ? (xhr.responseJSON.mensaje || xhr.responseJSON.message)
                : 'No fue posible registrar la solicitud. Intenta de nuevo.';

            Swal.fire({ title: 'Error', text: mensaje, icon: 'error' });
        }
    });
}

function CargarCuentasSelect(idSelect, data) {
    const select = $(idSelect);
    select.empty();
    select.append($('<option>', {
        value: 0,
        text: 'Selecciona un número de cuenta',
        selected: true
    }));

    data.forEach(cuenta => {
        select.append($('<option>', {
            value: cuenta.CUENTA_COMAPA,
            text: `${cuenta.CUENTA_COMAPA} - ${cuenta.ALIAS_CUENTA}`
        }));
    });
}

function ObtenerCuentasHojaAdeudo() {
    $.ajax({
        url: '/Recibos/ObtenerCuentasRecibosUsuario',
        type: "GET",
        cache: false,
        success: function (data) {
            if (data && data.length > 0) {
                CargarCuentasSelect('#SelectCuentaHojaAdeudo', data);
                $('#avisoCuentasHoja').addClass('d-none');
            } else {
                $('#SelectCuentaHojaAdeudo').prop('disabled', true);
                $('#avisoCuentasHoja').removeClass('d-none');
            }
        },
        error: function (xhr) {
            if (xhr.status === 401) {
                mostrarValidacion('La sesión expiró. Inicia sesión nuevamente.', 'danger');
                return;
            }

            Swal.fire({
                title: 'Error',
                text: 'No fue posible obtener tus cuentas. Intenta de nuevo.',
                icon: 'error'
            });
        }
    });
}
