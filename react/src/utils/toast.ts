import Swal from 'sweetalert2';

export function toast(msg: string, type: 'success' | 'error' = 'success') {
  Swal.fire({
    text: msg,
    icon: type,
    toast: true,
    position: 'top-end',
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
    background: type === 'success' ? '#dcfce7' : '#fee2e2',
    color: type === 'success' ? '#166534' : '#991b1b',
    iconColor: type === 'success' ? '#10b981' : '#ef4444',
  });
}
