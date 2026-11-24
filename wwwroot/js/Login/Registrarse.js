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
let TxtCodigoRegistro = $("#TxtCodigoRegistro");
let grupoCodigoRegistro = $("#grupoCodigoRegistro");
let btnReenviarCodigo = $("#btnReenviarCodigo");

//objetos
let objRegistro = objUser;

// Estado del flujo: 'datos' (captura) -> 'codigo' (verificación).
let etapaRegistro = 'datos';

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
        if (etapaRegistro === 'datos') {
            btnRegistrar.prop('disabled', !this.checked);
        }
    });

    btnRegistrar.on('click', function (e) {
        if (etapaRegistro === 'datos' && (!pdfOpen || !$('#chkTerminos').is(':checked'))) {
            e.preventDefault();
            $('#msgTerminos').removeClass('d-none');
        }
    });

    setTimeout(() => {
        TxtNewUser.val('');
        TxtPassW.val('');
    }, 300);
});

//eventos
btnRegistrar.on('click', () => {
    if (etapaRegistro === 'datos') {
        const formularioValido = validarDatos(
            TxtNewUser.val(),
            TxtPassW.val(),
            TxtPassWConfrim.val(),
            TxtMail.val(),
            TxtMailConfirm.val(),
            TxtNewTel.val());

        if (formularioValido === true) {
            objRegistro.ACCION = 'ALTA';
            objRegistro.USUARIO = TxtNewUser.val().trim();
            objRegistro.CONTRASENIA = TxtPassWConfrim.val();
            objRegistro.CORREO_ELECTRONICO = TxtMailConfirm.val().trim();
            objRegistro.TELEFONO_CEL = TxtNewTel.val().trim();
            EnviarCodigo();
        } else {
            Swal.fire({ title: 'Error!', icon: 'error', text: `${formularioValido}` });
        }
        return;
    }

    // Etapa código: verificar OTP y completar el registro.
    const codigo = TxtCodigoRegistro.val().trim();
    if (!/^\d{6}$/.test(codigo)) {
        Swal.fire({ title: 'Error!', icon: 'error', text: 'Ingresa el código de 6 dígitos.' });
        return;
    }

    objRegistro.CODIGO = codigo;
    RegistrarUsuario();
});

btnReenviarCodigo.on('click', (e) => {
    e.preventDefault();
    EnviarCodigo();
});

btnBackIndex2.on('click', () => {
    window.location.href = '/Home/Index';
});

TxtNewTel.on('input', function () {
    this.value = this.value.replace(/[^0-9]/g, '').substring(0, 10);
});

TxtCodigoRegistro.on('input', function () {
    this.value = this.value.replace(/[^0-9]/g, '').substring(0, 6);
});

TxtNewUser.on('input', function () {
    this.value = this.value.replace(' ', '');
});

//funciones
function validarDatos(Usuario, PassW, PassWConfirm, Mail, MailConfirm, Tel) {
    const regexPass = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d!@#$%^&*()_+\-={}[\]:;"'<>,.?\/\\|]{8,}$/;
    const regexMail = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    const regexTel = /^\d{10}$/;
    const regexName = /^[A-Za-zÁÉÍÓÚáéíóúÑñ ]+$/;

    if (!Usuario.trim()) return 'El Usuario es requerido';
    if (/\s/.test(Usuario)) return 'No se permiten espacios en el nombre de usuario.';
    if (!PassW.trim()) return 'La contraseña es requerida';
    if (!PassWConfirm.trim()) return 'La confirmación de la contraseña es requerida';
    if (!Mail.trim()) return 'El Correo es requerido';
    if (!MailConfirm.trim()) return 'Confirmar el Correo es requerido';
    if (!Tel.trim()) return 'El teléfono es requerido';

    if (!regexPass.test(PassW)) return 'Ingrese una contraseña con mínimo 8 caracteres, 1 letra y 1 número';
    if (!regexPass.test(PassWConfirm)) return 'Ingrese una contraseña con mínimo 8 caracteres, 1 letra y 1 número';
    if (!regexMail.test(Mail)) return 'Ingrese un correo válido';
    if (!regexMail.test(MailConfirm)) return 'Ingrese un correo válido';
    if (!regexName.test(Usuario)) return 'El nombre debe contener solo letras';
    if (Mail !== MailConfirm) return 'Los correos no coinciden';
    if (PassW !== PassWConfirm) return 'Las contraseñas no coinciden';
    if (!regexTel.test(Tel)) return 'Ingrese un número de teléfono válido (10 dígitos)';
    return true;
}

function EnviarCodigo() {
    btnRegistrar.prop('disabled', true);

    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Login/EnviarCodigoRegistro',
        type: "POST",
        data: JSON.stringify({ UserDatos: { CORREO_ELECTRONICO: objRegistro.CORREO_ELECTRONICO } }),
        cache: false,
        success: function (data) {
            if (data && data.exito) {
                etapaRegistro = 'codigo';
                grupoCodigoRegistro.removeClass('d-none');
                TxtCodigoRegistro.focus();
                btnRegistrar.html('<i class="fas fa-check-circle me-2"></i>Verificar código');

                [TxtNewUser, TxtPassW, TxtPassWConfrim, TxtMail, TxtMailConfirm, TxtNewTel].forEach(function (el) {
                    el.prop('disabled', true);
                });
                chkTerminos.prop('disabled', true);

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

                btnRegistrar.prop('disabled', false);
            } else {
                btnRegistrar.prop('disabled', false);
                Swal.fire({
                    title: "ERROR!!",
                    text: (data && data.mensaje) || 'No fue posible enviar el código.',
                    icon: "error"
                });
            }
        },
        error: function () {
            btnRegistrar.prop('disabled', false);
            Swal.fire({ title: "ERROR!!", text: 'OCURRIÓ UN ERROR AL ENVIAR EL CÓDIGO.', icon: "error" });
        }
    });
}

function LimpiarFormularioRegistro() {
    TxtNewUser.val('');
    TxtPassW.val('');
    TxtPassWConfrim.val('');
    TxtMail.val('');
    TxtMailConfirm.val('');
    TxtNewTel.val('');
    TxtCodigoRegistro.val('');
    chkTerminos.prop('checked', false);
}

//ajax
function RegistrarUsuario() {
    btnRegistrar.prop('disabled', true);

    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Login/UsuarioWeb',
        type: "POST",
        data: JSON.stringify(objRegistro),
        cache: false,
        success: function (data) {
            const mensaje = data && data.mensaje
                ? data.mensaje
                : (data && data.datos && data.datos.length > 0 ? data.datos[0].mensaje : null);

            if (data && data.exito && data.datos && data.datos.length > 0 &&
                data.datos[0].mensaje === 'OK') {
                Swal.fire({
                    title: "AVISO!!",
                    text: "El usuario se registró correctamente.",
                    icon: "success"
                }).then(() => {
                    LimpiarFormularioRegistro();
                    LimpiarObjetoUsuario();
                    window.location.href = '/Home/Index';
                });
            } else {
                btnRegistrar.prop('disabled', false);
                Swal.fire({
                    title: "ERROR!!",
                    text: mensaje || 'No fue posible registrar el usuario.',
                    icon: "error"
                });
            }
        },
        error: function () {
            btnRegistrar.prop('disabled', false);
            Swal.fire({ title: "ERROR!!", text: 'OCURRIÓ UN ERROR AL REGISTRAR EL USUARIO.', icon: "error" });
        }
    });
};
