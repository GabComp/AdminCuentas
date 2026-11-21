let SelectCuentaHojaAdeudo = $("#SelectCuentaHojaAdeudo");
let divButtonDownload = $("#divButtonDownload");

$(document).ready(function () {
    $("#divButtonDownload").removeClass("d-flex").addClass("d-none");
    AsegurarCuentasUsuario();
});

function AsegurarCuentasUsuario() {
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
    let valor = this.value;
    if (valor != null && valor != undefined && valor != "" && valor != '0') {
        $("#divButtonDownload").removeClass("d-none").addClass("d-flex");
    } else {
        $("#divButtonDownload").removeClass("d-flex").addClass("d-none");
    }
})

function CargarCuentasSelect(idSelect, data) {
    const select = $(`${idSelect}`);
    select.empty();

    // Opción predeterminada
    select.append(
        $('<option>', {
            value: 0,
            text: 'Selecciona un número de cuenta',
            selected: true
        })
    );

    data.forEach(cuenta => {
        select.append(
            $('<option>', {
                value: cuenta.CUENTA_COMAPA,
                text: `${cuenta.CUENTA_COMAPA} - ${cuenta.ALIAS_CUENTA}`
            })
        );
    })
};

$("#BtnDownload").on("click", function () {
    const rutaPDF = '/docs/HojaAdeudo/Terminos_Condiciones.pdf';

    const enlace = document.createElement('a');
    enlace.href = rutaPDF;
    enlace.download = 'Terminos_Condiciones';

    document.body.appendChild(enlace);
    enlace.click();
    document.body.removeChild(enlace);
});

function ObtenerCuentasHojaAdeudo() {
    $.ajax({
        url: '/Recibos/ObtenerCuentasRecibosUsuario',
        type: "GET",
        cache: false,
        success: function (data) {
            if (data && data.length > 0) {
                CargarCuentasSelect('#SelectCuentaHojaAdeudo', data);
            } else {
                $('#SelectCuentaHojaAdeudo').prop('disabled', true);
                Swal.fire({
                    title: "Sin cuentas",
                    text: "No tienes cuentas vinculadas. Agrega una en Administrar Cuenta.",
                    icon: "info"
                });
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL OBTENER LAS CUENTAS!`, icon: "error" });
        }
    });
}