let TxtNewUser = $("#TxtNewUser");
let TxtPassW = $("#TxtPassW");
let TxtPassWConfrim = $("#TxtPassWConfrim");
let TxtMail = $("#TxtMail");
let TxtMailConfirm = $("#TxtMailConfirm");
let TxtNewTel = $("#TxtNewTel");
let chkTerminos = $("#chkTerminos");
let lnkTerminos = $("#lnkTerminos");
let msgTerminos = $("#msgTerminos");
let btnRegistrar = $("#btnRegistrar");
let btnBackIndex2 = $("#btnBackIndex2");

//objetos
let objRegistro = objUser;

//al tener lista la pantalla 
$(document).ready(function () {
    btnRegistrar.prop('disabled', true);
    let pdfOpen = false;

    lnkTerminos.on('click', () => {
        pdfOpen = true;

        chkTerminos.prop('disabled', false);
        msgTerminos.addClass('d-none');
    });

    chkTerminos.on('change', function () {
        btnRegistrar.prop('disabled', !this.checked);
    });

    btnRegistrar.on('click', function (e) {

        if (!pdfOpen || !$('#chkTerminos').is(':checked')) {
            e.preventDefault();
            $('#msgTerminos').removeClass('d-none');
            return false;
        }
    });

    setTimeout(() => {
        TxtNewUser.val('');
        TxtPassW.val('');
    }, 300);
});

//eventos
btnRegistrar.on('click', () => {
    let formularioValido = validarDatos(TxtNewUser.val(), TxtPassW.val(), TxtPassWConfrim.val(), TxtMail.val(), TxtMailConfirm.val(), TxtNewTel.val());
    if (formularioValido === true) {
        objRegistro.ACCION = 'ALTA';
        objRegistro.USUARIO = TxtNewUser.val();
        objRegistro.CONTRASENIA = TxtPassWConfrim.val();
        objRegistro.CORREO_ELECTRONICO = TxtMailConfirm.val();
        objRegistro.TELEFONO_CEL = TxtNewTel.val();
        RegistrarUsuario();
    } else {
        Swal.fire({
            title: 'Error!',
            icon: 'error',
            text: `${formularioValido}`
        });
    }
});

btnBackIndex2.on('click', () => {
    window.location.href = '/Home/Index';
});

TxtNewTel.on('input', function () {
    this.value = this.value.replace(/[^0-9]/g, '');
});

//funciones
function validarDatos(Usuario, PassW, PassWConfirm, Mail, MailConfirm, Tel) {
    const regexPass = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$/;
    const regexMail = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    const regexTel = /^\d{10}$/;
    const regexName = /^[A-Za-zÁÉÍÓÚáéíóúÑñ ]+$/;

    //primero revisamos que se cumpla cada uno por separado para que encaso de que no detener la funcion y retornar el error de acuerdo al dato erroneo
    if (!Usuario.trim()) return 'El Usuario es requerido';
    if (!PassW.trim()) return 'La contraseña es requerido';
    if (!PassWConfirm.trim()) return 'Confirmar la contraseña es requerido';
    if (!Mail.trim()) return 'El Correo es requerido';
    if (!MailConfirm.trim()) return 'Confirmar el Correo es requerido';
    if (!Tel.trim()) return 'El teléfono es requerido';

    if (!regexPass.test(PassW)) return 'ingrese una contraseña con minimo 8 caracteres, 1 letra y 1 numero';
    if (!regexPass.test(PassWConfirm)) return 'ingrese una contraseña con minimo 8 caracteres, 1 letra y 1 numero';
    if (!regexMail.test(Mail)) return 'ingrese un correo valido';
    if (!regexMail.test(MailConfirm)) return 'ingrese un correo valido';
    if (!regexName.test(Usuario)) return 'El Nombre debe contener solo letras'
    if (Mail !== MailConfirm) return 'el correo no esta confirmado, los Correos no coinciden';
    if (PassW !== PassWConfirm) return 'la contraseña no esta confirmada, no coinciden';
    if (!regexTel.test(Tel)) return 'ingrese un numero de telefono valido';
    //finalmente nos aseguramos que todo este bien para retonar un exito 
    return true
}

function LimpiarFormularioRegistro() {
    TxtNewUser.val();
    TxtPassW.val();
    TxtPassWConfrim.val();
    TxtMail.val();
    TxtMailConfirm.val();
    TxtNewTel.val();
    chkTerminos.val();
}

//ajax
function RegistrarUsuario() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Login/UsuarioWeb',
        type: "POST",
        data: JSON.stringify(objRegistro),
        cache: false,
        success: function (data) {
            if (data.datos[0].mensaje === 'OK' && data.exito) {
                Swal.fire({
                    title: "AVISO!!",
                    text: "el usuario se registro correctamente",
                    icon: "success"
                }).then(() => {
                    LimpiarFormularioRegistro();
                    LimpiarObjetoUsuario();
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