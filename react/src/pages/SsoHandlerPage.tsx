import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Gift, AlertCircle, Loader2 } from 'lucide-react';

export default function SsoHandlerPage() {
  const [searchParams] = useSearchParams();
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = searchParams.get('token');
    if (!token) {
      setError('No se recibio token SSO.');
      setLoading(false);
      return;
    }

    fetch('/api-asistencia/auth/sso-login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token }),
    })
      .then((res) => {
        if (!res.ok) throw new Error('Error al validar token SSO');
        return res.json();
      })
      .then((data) => {
        const d = data.data || data;
        localStorage.setItem('token', d.access_token);
        localStorage.setItem('user', JSON.stringify(d.user));
        window.location.href = '/asistencia/';
      })
      .catch((err) => {
        setError(err.message || 'Error de autenticacion SSO.');
        setLoading(false);
      });
  }, [searchParams]);

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center" style={{ background: '#F8FAFC' }}>
        <div className="rounded-[32px] p-12 text-center" style={{ background: 'white', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.05)' }}>
          <div className="w-20 h-20 rounded-[20px] flex items-center justify-center mx-auto mb-6"
            style={{ background: 'rgba(218, 41, 28, 0.1)' }}
          >
            <Gift className="w-10 h-10" style={{ color: '#DA291C' }} />
          </div>
          <Loader2 className="w-6 h-6 mx-auto mb-4 animate-spin" style={{ color: '#DA291C' }} />
          <p className="text-sm font-semibold" style={{ color: '#64748B' }}>Validando acceso...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center" style={{ background: '#F8FAFC' }}>
      <div className="rounded-[32px] p-12 text-center max-w-sm" style={{ background: 'white', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.05)' }}>
        <AlertCircle className="w-10 h-10 mx-auto mb-4" style={{ color: '#EF4444' }} />
        <p className="text-sm font-semibold mb-1" style={{ color: '#0F172A' }}>{error}</p>
        <p className="text-xs mb-6" style={{ color: '#94A3B8' }}>Inicia sesion manualmente:</p>
        <a
          href="/asistencia/"
          className="inline-flex items-center gap-2 px-5 py-2.5 rounded-[10px] text-sm font-semibold transition-all"
          style={{ background: '#DA291C', color: 'white' }}
        >
          Ir al inicio
        </a>
      </div>
    </div>
  );
}
