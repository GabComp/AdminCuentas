//Login
let TxtUser = $("#TxtUser");
//let TxtCorreolog = $("#TxtCorreolog");
let TxtPass = $("#TxtPass");
let btnIniciarSession = $("#btnIniciarSession");
let btnRegistrarse = $("#btnRegistrarse");
//let btnOmitir = $("#btnOmitir");
let perfiLink = $("#perfiLink");

let objConsultarCuentas = {
    ID_USUARIO_WEB: 0,
};

$(document).ready(function () {
    //localStorage.clear();
    CerrarSesion();
    $("#perfiLink").hide();

    setTimeout(() => {
        TxtPass.val('');
    }, 300);
});

//eventos
btnIniciarSession.on('click', () => {
    let formularioValido = validarDatosLogin(TxtUser.val(), TxtPass.val());
    if (formularioValido === true) {
        objUser.ACCION = 'ACCE';
        TxtUser.val().includes('@') ? objUser.CORREO_ELECTRONICO = TxtUser.val() : objUser.USUARIO = TxtUser.val();
        objUser.CONTRASENIA = TxtPass.val();
        Login();
    } else {
        Swal.fire({
            title: 'Error!',
            icon: 'error',
            text: `${formularioValido}`
        });
    }
});

//btnRegistrarse.on('click', () => {
//    window.location.href = '/Home/Registro';
//});

//btnOmitir.on('click', () => {
//    window.location.href = '/Home/Inicio';
//});

TxtPass.on('keydown', function (e) {
    if (e.key === 'Enter') {
        e.preventDefault();
        btnIniciarSession.click();
    }
});

TxtUser.on('input', function () {
    this.value = this.value.replace(' ', '');
});

//funciones
function validarDatosLogin(user, pass) {
    //primero revisamos que se cumpla cada uno por separado para que encaso de que no detener la funcion y retornar el error de acuerdo al dato erroneo
    const regexMail = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    const regexName = /^[A-Za-zÁÉÍÓÚáéíóúÑñ ]+$/;
    if (!user.trim()) return 'El Usuario es requerido';
    if (/\s/.test(user)) {
        return 'No se permiten espacios en el nombre de usuario o correo.';
    }
    if (!pass.trim()) return 'La contraseñia es requerido';
    
    if (user.includes("@")) {
        if (!regexMail.test(user)) return 'El formato del correo es incorrecto';
    } else {
        if (!regexName.test(user)) return 'El Nombre debe contener solo letras';
    }
    
    //finalmente nos aseguramos que todo este bien para retonar un exito 
    return true
}

function LimpiarFormLogin() {
    TxtUser.val('');
    //TxtCorreolog.val(''); 
    TxtPass.val('');
}

//ajax
function Login() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Login/InicioSesion',
        type: "POST",
        data: JSON.stringify(objUser),
        cache: false,
        success: function (data) {
            //console.log(data);
            let result = data.datos[0].mensaje
            if (/^\d+$/.test(result) > 0) {
                LimpiarFormLogin();
                window.location.href = '/Home/Menu';
                ConsultarCuentasUsuario();
            } else {
                Swal.fire({ title: "ERROR!!", text: `Revise su usuario y contraseña`, icon: "error" });
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR EN EL SERVICIO AL INICIAE SESION!`, icon: "error" });
        }
    });
};

function ConsultarCuentasUsuario() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/AdministradorDeCuentas/ConsultarCuentasUsuario',
        type: "POST",
        data: JSON.stringify(objConsultarCuentas),
        cache: false,
        success: function (data) {
            if (data.exito) {
                //console.log(data);
            } else {
                //Swal.fire({ title: "ERROR!!", text: `gzfbhtfgb ${data.mensaje}`, icon: "error" });
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};

function CerrarSesion() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Login/CerrarSesion',
        type: "POST",
        data: JSON.stringify({}),
        cache: false,
        success: function (data) {
            //if (data.exito) {
            //    //console.log(data);
            //} else {
            //    Swal.fire({ title: "ERROR!!", text: `${data.mensaje}`, icon: "error" });
            //}
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};