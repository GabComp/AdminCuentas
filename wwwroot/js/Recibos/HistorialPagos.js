let SelectCuentaPagos = $("#SelectCuentaPagos");

let DatosValidacionPagos = {
    alias: "HISTORIAL_PAGOS",
    filtros: [
        {
            id_cuenta: 0
        }
    ]
};

$(document).ready(function () {
    AsegurarCuentasUsuario();
});

SelectCuentaPagos.on("change", function () {
    const valor = this.value;
    if (valor && valor !== "0") {
        DatosValidacionPagos.filtros[0].id_cuenta = valor;
        ConsultaHistorialPagos();
    }
});

function AsegurarCuentasUsuario() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: "/AdministradorDeCuentas/ConsultarCuentasUsuario",
        type: "POST",
        data: JSON.stringify({ ID_USUARIO_WEB: 0 }),
        cache: false,
        complete: function () {
            ObtenerCuentas();
        }
    });
}

function ObtenerCuentas() {
    $.ajax({
        url: "/Recibos/ObtenerCuentasRecibosUsuario",
        type: "GET",
        cache: false,
        success: function (data) {
            if (data && data.length > 0) {
                CargarCuentasSelect(data);
            } else {
                mostrarVacio("No tienes cuentas vinculadas.", "fa-link-slash");
            }
        },
        error: function () {
            mostrarVacio("No fue posible obtener tus cuentas.", "fa-circle-exclamation");
        }
    });
}

function CargarCuentasSelect(data) {
    const select = SelectCuentaPagos;
    select.empty();

    data.forEach(function (cuenta) {
        select.append(
            $("<option>", {
                value: cuenta.CUENTA_COMAPA,
                text: cuenta.CUENTA_COMAPA + " - " + cuenta.ALIAS_CUENTA
            })
        );
    });

    const valor = select.val();
    if (valor) {
        DatosValidacionPagos.filtros[0].id_cuenta = valor;
        ConsultaHistorialPagos();
    }
}

function ConsultaHistorialPagos() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: "/Recibos/ConsultaHistorialPagos",
        type: "POST",
        data: JSON.stringify(DatosValidacionPagos),
        cache: false,
        success: function (data) {
            if (data && data.ok) {
                renderizar(data.body || []);
            } else {
                mostrarVacio((data && data.message) || "No fue posible consultar el historial.", "fa-circle-exclamation");
            }
        },
        error: function () {
            mostrarVacio("No fue posible consultar el historial de pagos.", "fa-circle-exclamation");
        }
    });
}

function renderizar(pagos) {
    const tbody = $("#tbodyPagos");
    tbody.empty();

    if (pagos.length === 0) {
        mostrarVacio("No tienes pagos en línea registrados para esta cuenta.", "fa-receipt");
        return;
    }

    $("#estadoPagos").addClass("d-none");
    $("#tablaPagos").removeClass("d-none");

    pagos.forEach(function (pago) {
        const fecha = pago.fechaPago
            ? new Date(pago.fechaPago).toLocaleString("es-MX", {
                year: "numeric",
                month: "2-digit",
                day: "2-digit",
                hour: "2-digit",
                minute: "2-digit"
            })
            : "-";

        const importe = (pago.importe != null)
            ? "$" + Number(pago.importe).toLocaleString("es-MX", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
            : "-";

        const fila = "<tr>" +
            "<td>" + fecha + "</td>" +
            "<td>" + pago.idPago + "</td>" +
            "<td>" + importe + "</td>" +
            "<td>" + etiquetaEstatus(pago.idEstatus) + "</td>" +
            "</tr>";

        tbody.append(fila);
    });
}

function etiquetaEstatus(idEstatus) {
    const id = Number(idEstatus);

    if (id === 30) {
        return '<span class="badge-pago aplicado">Aplicado</span>';
    }
    if (id === 28) {
        return '<span class="badge-pago proceso">En proceso</span>';
    }
    return '<span class="badge-pago otro">' + (id || "-") + "</span>";
}

function mostrarVacio(mensaje, icono) {
    $("#tablaPagos").addClass("d-none");

    const estado = $("#estadoPagos");
    estado.removeClass("d-none");
    estado.html('<i class="fas ' + icono + '"></i><p>' + mensaje + "</p>");
}
