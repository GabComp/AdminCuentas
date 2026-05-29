let TxtUserUpdate = $("#TxtUserUpdate");
let TxtMailUpdate = $("#TxtMailUpdate");
let TxtMailConfirmUpdate = $("#TxtMailConfirmUpdate");
let TxtPassUpdate = $("#TxtPassUpdate");
let TxtPassConfrimUpdate = $("#TxtPassConfrimUpdate");
let TxtNewTelUpdate = $("#TxtNewTelUpdate");
let btnUpdate = $("#btnUpdate");
let btnLogOut = $("#btnLogOut");

$(document).ready(function () {
    CargarDatosUsuario();
});

//eventos
btnUpdate.on('click', () => {
    let formularioValido = validarDatosUpdate(TxtUserUpdate.val(), TxtMailUpdate.val(), TxtMailConfirmUpdate.val(), TxtPassUpdate.val(), TxtPassConfrimUpdate.val(), TxtNewTelUpdate.val());
    if (formularioValido === true) {
        objUser.ACCION = 'ACT';
        objUser.USUARIO = TxtUserUpdate.val();
        objUser.CORREO_ELECTRONICO = TxtMailUpdate.val();
        objUser.CONTRASENIA = TxtPassUpdate.val();
        objUser.TELEFONO_CEL = TxtNewTelUpdate.val();
        ActualizarUsuario();
    } else {
        Swal.fire({
            title: 'Error!',
            icon: 'error',
            text: `${formularioValido}`
        });
    }
});

btnLogOut.on('click', () => {
    CerrarSesion();
});

TxtNewTelUpdate.on('input', function () {
    this.value = this.value.replace(/[^0-9]/g, '');
});

//funciones
function validarDatosUpdate(Usuario, Mail, MailConfirm, PassW, PassWConfirm, Tel) {
    const regexMail = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    const regexTel = /^\d{10}$/;
    const regexName = /^[A-Za-zÁÉÍÓÚáéíóúÑñ ]+$/;

    //primero revisamos que se cumpla cada uno por separado para que encaso de que no detener la funcion y retornar el error de acuerdo al dato erroneo
    if (!Usuario.trim()) return 'El Usuario es requerido';
    if (!Mail.trim()) return 'El Correo es requerido';
    if (!MailConfirm.trim()) return 'Confirmar el Correo es requerido';
    if (!PassW.trim()) return 'La contraseña es requerido';
    if (!PassWConfirm.trim()) return 'Confirmar la contraseña es requerido';
    if (!Tel.trim()) return 'El teléfono es requerido';

    if (!regexMail.test(Mail)) return 'ingrese un correo valido';
    if (!regexMail.test(MailConfirm)) return 'ingrese un correo valido';
    if (!regexName.test(Usuario)) return 'El Nombre debe contener solo letras'
    if (Mail !== MailConfirm) return 'el correo no esta confirmado, los Correos no coinciden';
    if (PassW !== PassWConfirm) return 'la contraseña no esta confirmada, no coinciden';
    if (!regexTel.test(Tel)) return 'ingrese un numero de telefono valido';
    //finalmente nos aseguramos que todo este bien para retonar un exito 
    return true
}

function LimpiarFormularioUpdate() {
    TxtUserUpdate.val();
    TxtMailUpdate.val();
    TxtMailConfirmUpdate.val();
    TxtPassUpdate.val();
    TxtPassConfrimUpdate.val();
    TxtNewTelUpdate.val();
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
                    LimpiarFormularioUpdate();
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

function CargarDatosUsuario() {
    $.get('/Login/CargarDatosUsuario', function (resp) {
        if (resp.usuario.mensaje > 0) {
            TxtUserUpdate.val(resp.usuario.NOMBRE);
            TxtMailUpdate.val(resp.usuario.CORREO);
            TxtMailConfirmUpdate.val(resp.usuario.CORREO);
            TxtNewTelUpdate.val(resp.usuario.mensaje2);
        } else {
            Swal.fire({ title: "Aviso!!", text: `no se encontraron usuarios`, icon: "warning" });
        }
    });
};

function CerrarSesion() {
    $.get('/Login/CerrarSesion', function (resp) {
        if (resp.ok) {
            window.location.href = '/Home/Index';
        }
    });
}