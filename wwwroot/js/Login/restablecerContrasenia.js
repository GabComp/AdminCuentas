let TxtMailContra = $("#TxtMailContra");
let TxtMailContraConfirm = $("#TxtMailContraConfirm");
let chkNoRobot = $("#chkNoRobot");
let robotError = $("#robotError");
let btnBackIndex = $("#btnBackIndex");
let btnContinuarContra = $("#btnContinuarContra");

//objetos
let objCaptcha = {
    Bot: false
}

let objPrepararCorreo = {
    ACCION: '',
    USUARIO: '',
    CONTRASENIA: '',
    CORREO_ELECTRONICO: '',
    TELEFONO_CEL: '',
    MOTIVO_BAJA: '',
    ID_USUARIO: '',
};

objDatosRecuperacion = {
    UserDatos: objPrepararCorreo,
    BotTest: objCaptcha
};

//eventos
btnBackIndex.on('click', () => {
    window.location.href = '/Home/Index';
});

chkNoRobot.on('click', () => {
    objDatosRecuperacion.BotTest.Bot = ValidarCaptcha();
});

btnContinuarContra.on('click', () => {
    let restablecimientoValido = ValidarCorreos(TxtMailContra.val(), TxtMailContraConfirm.val());
    if (restablecimientoValido === true) {
        objDatosRecuperacion.UserDatos.ACCION = 'CORR';
        objDatosRecuperacion.UserDatos.CORREO_ELECTRONICO = TxtMailContra.val().trim();

        PrepararRestablecimiento();
    } else {
        Swal.fire({
            title: 'Error!',
            icon: 'error',
            text: `${restablecimientoValido}`
        });
    }
});

//funciones
function ValidarCorreos( Mail, MailConfirm) {
    const regexMail = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

    if (!Mail.trim()) return 'El Correo es requerido';
    if (!MailConfirm.trim()) return 'Confirmar el correo es requerido';

    if (!regexMail.test(Mail)) return 'ingrese un correo valido';
    if (!regexMail.test(MailConfirm)) return 'ingrese un correo valido';
    if (Mail !== MailConfirm) return 'el correo no esta confirmado, los Correos no coinciden';
    return true;
};

function ValidarCaptcha() {
    if (!chkNoRobot.is(':checked')) {
        robotError.removeClass('d-none');
        return false;
    }
    robotError.addClass('d-none');
    return true;
};

function LimpiarFormularioContras() {
    TxtMailContra.val('');
    TxtMailContraConfirm.val('');
};

//ajax
function PrepararRestablecimiento() {

    if (!objDatosRecuperacion.BotTest.Bot) {
        Swal.fire({ title: "ERROR!!", text: `no ha pasado la prueba de que no es un robot`, icon: "error" });
        return;
    }

    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Login/Restablecimiento',
        type: "POST",
        data: JSON.stringify(objDatosRecuperacion),
        cache: false,
        success: function (data) {
            if (data.exito) {
                Swal.fire({
                    title: "AVISO!!",
                    text: `${data.mensaje}`,
                    icon: "success"
                }).then(() => {
                    LimpiarFormularioContras();
                    window.location.href = '/Home/Index';
                });
            } else {
                Swal.fire({ title: "ERROR!!", text: `${data.mensaje}`, icon: "error" });
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
}