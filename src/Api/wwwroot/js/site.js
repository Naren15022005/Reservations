// Confirmar cancelación
function confirmCancel(event) {
    if (!confirm('¿Estás seguro de que deseas cancelar esta reserva? Esta acción no se puede deshacer.')) {
        event.preventDefault();
    }
}

// Calcular costo automáticamente al cambiar fechas/personas
document.addEventListener('DOMContentLoaded', function () {
    const checkIn  = document.getElementById('CheckIn');
    const checkOut = document.getElementById('CheckOut');
    const persons  = document.getElementById('TotalPersons');

    if (checkIn && checkOut) {
        checkOut.addEventListener('change', updateNightsDisplay);
        checkIn.addEventListener('change', function () {
            if (checkOut.value && checkIn.value >= checkOut.value) {
                checkOut.value = '';
            }
            updateNightsDisplay();
        });
    }

    function updateNightsDisplay() {
        const nights = document.getElementById('nights-display');
        if (!nights || !checkIn.value || !checkOut.value) return;
        const diff = Math.round((new Date(checkOut.value) - new Date(checkIn.value)) / 86400000);
        nights.textContent = diff > 0 ? diff + ' noche(s)' : '-';
    }
});
