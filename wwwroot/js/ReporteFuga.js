let btnEnviarFuga = $("#btnEnviarFuga");

$("#fugaTelefono, #fugaCuenta").on("input", function () {
    this.value = this.value.replace(/[^0-9]/g, "").substring(0, 10);
});

btnEnviarFuga.on("click", function () {
    const nombre = $("#fugaNombre").val().trim();
    const telefono = $("#fugaTelefono").val().trim();
    const correo = $("#fugaCorreo").val().trim();
    const tipo = $("#fugaTipo").val();
    const direccion = $("#fugaDireccion").val().trim();
    const colonia = $("#fugaColonia").val().trim();
    const referencia = $("#fugaReferencia").val().trim();
    const descripcion = $("#fugaDescripcion").val().trim();
    const cuenta = $("#fugaCuenta").val().trim();

    if (!nombre) {
        Swal.fire({ title: "Error", icon: "error", text: "Indica tu nombre." });
        return;
    }
    if (!tipo) {
        Swal.fire({ title: "Error", icon: "error", text: "Selecciona el tipo de reporte." });
        return;
    }
    if (!descripcion) {
        Swal.fire({ title: "Error", icon: "error", text: "Describe el problema." });
        return;
    }
    if (telefono && !/^\d{10}$/.test(telefono)) {
        Swal.fire({ title: "Error", icon: "error", text: "El teléfono debe contener 10 dígitos." });
        return;
    }
    if (correo && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(correo)) {
        Swal.fire({ title: "Error", icon: "error", text: "El correo no tiene un formato válido." });
        return;
    }

    const payload = {
        Nombre: nombre,
        Telefono: telefono,
        Correo: correo,
        TipoReporte: tipo,
        Direccion: direccion,
        Colonia: colonia,
        Referencia: referencia,
        Descripcion: descripcion,
        IdCuenta: cuenta ? parseInt(cuenta) : null
    };

    btnEnviarFuga.prop("disabled", true);

    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: "/Home/ReporteFuga",
        type: "POST",
        data: JSON.stringify(payload),
        cache: false,
        success: function (data) {
            if (data && data.exito) {
                Swal.fire({
                    title: "Reporte enviado",
                    text: data.mensaje || "Gracias por avisar.",
                    icon: "success"
                }).then(() => {
                    $("#formFuga")[0].reset();
                });
            } else {
                Swal.fire({
                    title: "Error",
                    icon: "error",
                    text: (data && data.mensaje) || "No fue posible enviar el reporte."
                });
            }
            btnEnviarFuga.prop("disabled", false);
        },
        error: function () {
            btnEnviarFuga.prop("disabled", false);
            Swal.fire({ title: "Error", icon: "error", text: "Ocurrió un error al enviar el reporte." });
        }
    });
});
