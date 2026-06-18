import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Lock, User, AlertCircle } from 'lucide-react';

export default function LoginPage() {
  const [carnet, setCarnet] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(carnet, password);
      navigate('/');
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Credenciales invalidas o error de conexion.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center px-4" style={{ background: '#F8FAFC' }}>
      <div className="w-full max-w-md p-8 sm:p-10 rounded-xl"
        style={{
          background: 'rgba(30, 41, 59, 0.7)',
          backdropFilter: 'blur(24px)',
          border: '1px solid rgba(255, 255, 255, 0.1)',
          boxShadow: '0 20px 44px -18px rgba(0, 0, 0, 0.45)',
        }}
      >
        <div className="flex flex-col items-center mb-8">
          <div className="w-16 h-16 rounded-xl flex items-center justify-center"
            style={{
              background: 'linear-gradient(135deg, #DA291C 0%, #a51d14 100%)',
              boxShadow: '0 12px 24px rgba(218, 41, 28, 0.3)',
            }}
          >
            <span className="text-white font-display font-extrabold text-2xl tracking-tighter">Claro</span>
          </div>
          <h1 className="mt-4 text-2xl font-display font-extrabold text-white text-center">Despacho de Juguetes</h1>
          <p className="text-xs text-gray-400 mt-1">Control de Asistencia e Inventario</p>
        </div>

        {error && (
          <div className="mb-4 p-3 rounded-lg flex items-center gap-2 text-xs"
            style={{ background: 'rgba(239, 68, 68, 0.1)', color: '#EF4444', border: '1px solid rgba(239, 68, 68, 0.2)' }}
          >
            <AlertCircle className="w-4 h-4 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-gray-300">Número de Carnet</label>
            <div className="relative">
              <User className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
              <input
                type="text"
                required
                value={carnet}
                onChange={(e) => setCarnet(e.target.value)}
                placeholder="Ej. 100234"
                className="w-full pl-10 pr-4 py-2.5 rounded-lg text-sm text-white placeholder-gray-500"
                style={{
                  background: 'rgba(15, 23, 42, 0.5)',
                  border: '1px solid rgba(255, 255, 255, 0.1)',
                  backdropFilter: 'blur(8px)',
                }}
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-gray-300">Contraseña</label>
            <div className="relative">
              <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full pl-10 pr-4 py-2.5 rounded-lg text-sm text-white placeholder-gray-500"
                style={{
                  background: 'rgba(15, 23, 42, 0.5)',
                  border: '1px solid rgba(255, 255, 255, 0.1)',
                  backdropFilter: 'blur(8px)',
                }}
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full py-3 mt-2 rounded-lg text-white font-semibold text-sm transition-all"
            style={{
              background: 'linear-gradient(135deg, #DA291C 0%, #a51d14 100%)',
              boxShadow: '0 12px 24px rgba(218, 41, 28, 0.3)',
            }}
          >
            {loading ? 'Validando...' : 'Iniciar Sesión'}
          </button>
        </form>

        <div className="mt-8 text-center">
          <p className="text-[10px] text-gray-500 font-medium uppercase tracking-wider">
            Exclusivo para uso interno - Claro 2026
          </p>
        </div>
      </div>
    </div>
  );
}
