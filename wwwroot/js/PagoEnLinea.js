
$('#btnConsulta').on('click', function () {
    const divSaldo = document.getElementById('divSaldoPendiente');
    divSaldo.classList.remove('d-none');
    divSaldo.classList.remove('fade-out');
    divSaldo.classList.add('fade-in');

    if ($(window).width() <= 768) {

        $('html, body').animate({
            scrollTop: $('#divSaldoPendiente').offset().top - 120
        }, 800);

    }
});

$('#btnCancelar').on('click', function () {
    const divSaldo = document.getElementById('divSaldoPendiente');
    divSaldo.classList.remove('fade-in');
    divSaldo.classList.add('fade-out');

    setTimeout(() => {
        divSaldo.classList.add('d-none');
    }, 800);

    if ($(window).width() <= 768) {

        $('html, body').animate({
            scrollTop: $('#divCuenta').offset().top - 120
        }, 800);

    }
});