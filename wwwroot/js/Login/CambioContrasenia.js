let TxtCodigo = $("#TxtCodigo");
let TxtPassChange = $("#TxtPassChange");
let TxtPassChangeConfrim = $("#TxtPassChangeConfrim");
let btnUpdateContra = $("#btnUpdateContra");

$(document).ready(function () {

});
//eventos
btnUpdateContra.on('click', () => {
    let formularioValido = validarDatosUpdateContra(TxtCodigo.val(), TxtPassChange.val(), TxtPassChangeConfrim.val());
    if (formularioValido === true) {
        objUser.ACCION = 'CONTRA';
        objUser.CONTRASENIA = TxtPassChange.val();
        objUser.CODIGO = TxtCodigo.val();
        ActualizarUsuario();
    } else {
        Swal.fire({
            title: 'Error!',
            icon: 'error',
            text: `${formularioValido}`
        });
    }
});

//funciones
function validarDatosUpdateContra(Codigo, PassW, PassWConfirm) {
    //primero revisamos que se cumpla cada uno por separado para que encaso de que no detener la funcion y retornar el error de acuerdo al dato erroneo
    if (!Codigo.trim()) return 'El Codgo de recuperacion es requerido';
    if (/\s/.test(Codigo)) {
        return 'Quite los espacios de el codigo.';
    }
    if (!PassW.trim()) return 'La contraseña es requerido';
    if (!PassWConfirm.trim()) return 'Confirmar la contraseña es requerido';
    if (/\s/.test(PassW)) {
        return 'Quite los espacios de el codigo.';
    }
    if (/\s/.test(PassWConfirm)) {
        return 'Quite los espacios de el codigo.';
    }

    if (PassW !== PassWConfirm) return 'la contraseña no esta confirmada, no coinciden';
    //finalmente nos aseguramos que todo este bien para retonar un exito 
    return true
};

function LimpiarFormularioContra() {
    TxtCodigo.val('');
    TxtPassChange.val('');
    TxtPassChangeConfrim.val('');
};

//ajax
function ActualizarUsuario() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Login/UsuarioWeb',
        type: "POST",
        data: JSON.stringify(objUser),
        cache: false,
        success: function (data) {
            if (data.datos[0].mensaje === 'OK' && data.exito) {
                Swal.fire({
                    title: "AVISO!!",
                    text: "el usuario se Actualizo correctamente",
                    icon: "success"
                }).then(() => {
                    LimpiarFormularioContra();
                    window.location.href = '/Home/Index';
                });
            } else {
                Swal.fire({ title: "ERROR!!", text: `${data.datos[0].mensaje}`, icon: "error" });
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};
