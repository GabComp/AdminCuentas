let SelectCuentaHojaAdeudo = $("#SelectCuentaHojaAdeudo");
let divButtonDownload = $("#divButtonDownload");

$(document).ready(function () {
    $("#divButtonDownload").removeClass("d-flex").addClass("d-none");
    ObtenerCuentasHojaAdeudo();
});

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
            text: 'Selecciona un numero de cuenta',
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
        contentType: "application/json; charset=utf-8",
        url: '/Recibos/ObtenerCuentasRecibosUsuario',
        type: "POST",
        data: JSON.stringify(),
        cache: false,
        success: function (data) {
            if (data.length > 0) {
                CargarCuentasSelect('#SelectCuentaHojaAdeudo', data);
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
}