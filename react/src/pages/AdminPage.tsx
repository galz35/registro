import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft, Search, Shield, UserCheck } from 'lucide-react';
import api from '../services/api';
import { toast } from '../utils/toast';

const ROLES = ['admin', 'supervisor', 'despachador', 'consulta'];

export default function AdminPage() {
  const navigate = useNavigate();
  const [users, setUsers] = useState<any[]>([]);
  const [searchQ, setSearchQ] = useState('');
  const [results, setResults] = useState<any[]>([]);
  const [showSearch, setShowSearch] = useState(false);

  const loadUsers = async () => {
    try {
      const { data } = await api.get('/admin/users');
      setUsers(data || []);
    } catch {}
  };

  useEffect(() => { loadUsers(); }, []);

  const handleSearch = async () => {
    if (!searchQ.trim()) return;
    try {
      const { data } = await api.get('/admin/search-portal', { params: { q: searchQ } });
      setResults(data || []);
      setShowSearch(true);
    } catch { toast('Error al buscar', 'error'); }
  };

  const handleSetRole = async (carnet: string, rol: string) => {
    try {
      await api.post('/admin/set-role', { carnet, rol });
      toast(`Rol ${rol} asignado a ${carnet}`);
      loadUsers();
      setResults([]);
      setShowSearch(false);
      setSearchQ('');
    } catch { toast('Error al asignar rol', 'error'); }
  };

  return (
    <div style={{ background: '#f8f9fa', minHeight: '100vh' }}>
      <header style={{ background: 'linear-gradient(135deg, #da121a 0%, #1e1e1e 100%)', color: 'white', padding: '16px 24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <button onClick={() => navigate('/')} style={{ background: 'rgba(255,255,255,0.15)', border: 'none', color: 'white', borderRadius: '20px', padding: '6px 16px', fontWeight: 700, fontSize: 13, cursor: 'pointer' }}>
          <ChevronLeft className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Dashboard
        </button>
        <h2 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, fontSize: 22, margin: 0 }}>
          <Shield className="w-5 h-5" style={{ verticalAlign: 'middle', marginRight: 8 }} /> Usuarios y Roles
        </h2>
        <div />
      </header>

      <main style={{ padding: 24, maxWidth: 900, margin: '0 auto' }}>
        {/* Buscador de usuarios del Portal */}
        <section style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)', padding: 20, marginBottom: 20 }}>
          <h3 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, fontSize: 16, margin: '0 0 12px' }}>
            <Search className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 6 }} /> Buscar usuario en Portal
          </h3>
          <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
            <input type="text" value={searchQ} onChange={e => setSearchQ(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleSearch()}
              placeholder="Buscar por carnet, nombre o correo..."
              style={{ flex: 1, padding: '10px 14px', borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14, outline: 'none' }} />
            <button onClick={handleSearch}
              style={{ background: '#da121a', color: 'white', border: 'none', borderRadius: 8, padding: '10px 20px', fontWeight: 600, cursor: 'pointer' }}>
              Buscar
            </button>
          </div>

          {showSearch && (
            <div>
              {results.length === 0 ? (
                <p style={{ color: '#9ca3af', fontSize: 13 }}>No se encontraron usuarios</p>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                  {results.map(u => {
                    const existing = users.find(x => x.carnet === u.carnet);
                    return (
                      <div key={u.carnet} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '10px 14px', borderRadius: 8, border: '1px solid #e5e7eb' }}>
                        <div>
                          <div style={{ fontWeight: 600, fontSize: 14 }}>{u.nombre}</div>
                          <div style={{ fontSize: 12, color: '#6b7280' }}>{u.carnet} {u.gerencia ? `· ${u.gerencia}` : ''}</div>
                          <div style={{ fontSize: 11 }}>
                            <span style={{ background: u.activo ? '#d1fae5' : '#fee2e2', color: u.activo ? '#065f46' : '#991b1b', padding: '1px 6px', borderRadius: 4 }}>
                              {u.activo ? 'Activo' : 'Inactivo'}
                            </span>
                            {existing && (
                              <span style={{ marginLeft: 8, color: '#da121a', fontWeight: 600 }}>Rol: {existing.rol}</span>
                            )}
                          </div>
                        </div>
                        <select value={existing?.rol || ''} onChange={e => e.target.value && handleSetRole(u.carnet, e.target.value)}
                          style={{ padding: '6px 10px', borderRadius: 6, border: '1px solid #e5e7eb', fontSize: 12, fontWeight: 600 }}>
                          <option value="">Sin rol</option>
                          {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
                        </select>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          )}
        </section>

        {/* Tabla de usuarios del sistema */}
        <section style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
          <div style={{ padding: '14px 20px', borderBottom: '1px solid #e5e7eb', fontWeight: 700, fontSize: 14, color: '#374151' }}>
            <UserCheck className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 6 }} /> Usuarios del Sistema ({users.length})
          </div>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
              <thead>
                <tr style={{ background: '#f8fafc' }}>
                  <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Carnet</th>
                  <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Nombre</th>
                  <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Correo</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Rol</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Activo</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Cambiar Rol</th>
                </tr>
              </thead>
              <tbody>
                {users.map(u => (
                  <tr key={u.id} style={{ borderBottom: '1px solid #e5e7eb' }}>
                    <td style={{ padding: '10px 16px', fontWeight: 700, color: '#da121a', fontSize: 12 }}>{u.carnet}</td>
                    <td style={{ padding: '10px 16px', fontWeight: 600 }}>{u.nombre}</td>
                    <td style={{ padding: '10px 16px', color: '#6b7280', fontSize: 12 }}>{u.correo || '-'}</td>
                    <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                      <span style={{ background: '#f1f5f9', color: '#475569', padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700, textTransform: 'uppercase' }}>{u.rol}</span>
                    </td>
                    <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                      {u.activo ? <span style={{ color: '#10b981' }}>✅</span> : <span style={{ color: '#ef4444' }}>❌</span>}
                    </td>
                    <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                      <select value={u.rol} onChange={e => handleSetRole(u.carnet, e.target.value)}
                        style={{ padding: '4px 8px', borderRadius: 6, border: '1px solid #e5e7eb', fontSize: 11, fontWeight: 600 }}>
                        {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
                      </select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </main>
    </div>
  );
}
