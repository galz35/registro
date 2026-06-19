import { useState, useCallback, useEffect } from 'react';
import { toast } from '../utils/toast';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { usePolling } from '../hooks/usePolling';
import { getSummary, getCenso, getColaboradorFull, registrarAsistencia } from '../services/asistencia.api';
import type { ColaboradorFicha, DashboardKPI, CensoItem } from '../types';
import { Search, Check, Users, Gift, ChevronLeft, Loader2, RotateCcw, Package, UserCheck, Heart } from 'lucide-react';
import api from '../services/api';

const EVENTO_ACTIVO_ID = 1;

export default function AttendancePage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(false);
  const [ficha, setFicha] = useState<ColaboradorFicha | null>(null);
    const [registering, setRegistering] = useState(false);
  const [reverting, setReverting] = useState<number | null>(null);
  const [adultos, setAdultos] = useState(1);
  const [ninos, setNinos] = useState(0);
  const [asistioPor, setAsistioPor] = useState('COLABORADOR');
  const [nombreAsistente, setNombreAsistente] = useState('');
  const [filterTexto, setFilterTexto] = useState('');
  const [infoCarnet, setInfoCarnet] = useState<string | null>(null);
  const [searchResults, setSearchResults] = useState<any[] | null>(null);
  const [searchingName, setSearchingName] = useState(false);
  const [asistidos, setAsistidos] = useState<CensoItem[]>([]);
  const [allAsistidos, setAllAsistidos] = useState<CensoItem[]>([]);
  const [totalAsistidos, setTotalAsistidos] = useState(0);
  const [pagina, setPagina] = useState(1);
  const porPagina = 15;

  const kpiFetcher = useCallback(() => getSummary(EVENTO_ACTIVO_ID), []);
  const { data: kpis } = usePolling<DashboardKPI>(kpiFetcher, 10000);

  const show = (msg: string, type: 'success' | 'error' = 'success') => toast(msg, type);

  const loadAsistidos = async (page: number) => {
    try {
      // Load first page to get total, then load all if needed
      const first = await getCenso(EVENTO_ACTIVO_ID, undefined, undefined, 1, 200);
      let allData = first.data || [];
      const totalPages = first.totalPaginas || 1;
      // Load remaining pages
      for (let p = 2; p <= totalPages; p++) {
        const res = await getCenso(EVENTO_ACTIVO_ID, undefined, undefined, p, 200);
        allData = allData.concat(res.data || []);
      }
      const filtered = allData.filter((c: CensoItem) => c.Asistio > 0);
      setAllAsistidos(filtered);
      setTotalAsistidos(filtered.length);
      const start = (page - 1) * porPagina;
      setAsistidos(filtered.slice(start, start + porPagina));
    } catch {}
  };

  useEffect(() => { loadAsistidos(pagina); }, [pagina]);

  const buscar = async () => {
    const q = searchTerm.trim();
    if (!q) return;
    setSearchResults(null);
    setFicha(null);
    // Si solo contiene digitos -> buscar por carnet exacto
    if (/^\d+$/.test(q) && q.length >= 4) {
      setLoading(true);
      try {
        const data = await getColaboradorFull(q, EVENTO_ACTIVO_ID);
        setFicha(data);
      } catch { show('Colaborador no encontrado', 'error'); }
      finally { setLoading(false); }
    } else {
      // Buscar por nombre
      setSearchingName(true);
      try {
        const { data } = await api.get('/attendance/search', { params: { q } });
        setSearchResults(data || []);
        if (!data || data.length === 0) show('No se encontraron resultados', 'error');
      } catch { show('Error al buscar', 'error'); }
      finally { setSearchingName(false); }
    }
  };

  const seleccionarResultado = async (carnet: string) => {
    setSearchResults(null);
    setSearchTerm(carnet);
    setLoading(true);
    try {
      const data = await getColaboradorFull(carnet, EVENTO_ACTIVO_ID);
      setFicha(data);
    } catch { show('Error al cargar colaborador', 'error'); }
    finally { setLoading(false); }
  };

  const handleRegistrar = async () => {
    if (!ficha || ficha.asistio) return;
    setRegistering(true);
    try {
      await registrarAsistencia(EVENTO_ACTIVO_ID, ficha.colaborador.carnet, adultos, ninos, asistioPor, nombreAsistente);
      show(`✅ Asistencia registrada: ${ficha.colaborador.nombre}`);
      const data = await getColaboradorFull(ficha.colaborador.carnet, EVENTO_ACTIVO_ID);
      setFicha(data);
      await loadAsistidos(pagina);
    } catch (err: any) {
      show(err?.response?.data?.message || 'Error al registrar', 'error');
    } finally { setRegistering(false); }
  };

  const asistioLabel = (r: any) => {
    if (!r.AsistioPor) return '-';
    if (r.AsistioPor === 'COLABORADOR') return 'Colaborador';
    if (r.AsistioPor === 'CONYUGE') return 'Cónyuge';
    return r.NombreAsistente || 'Tercero';
  };

  const handleExportExcel = () => {
    const headers = ['Carnet','Nombre','Gerencia','Adultos','Niños','Asiste','Hijos','Entregados'];
    const rows = asistidos.map(r => [r.Carnet, r.Nombre, r.Gerencia||'', r.TotalAdultos||0, r.TotalNinos||0, asistioLabel(r), r.TotalHijos, `${r.Entregados}/${r.TotalHijos}`]);
    const csv = [headers.join(','), ...rows.map(r => r.map(v => `"${v}"`).join(','))].join('\n');
    const blob = new Blob(['\uFEFF'+csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a'); link.href = URL.createObjectURL(blob); link.download = 'asistencia.csv'; link.click();
  };

  const handleRevertir = async (carnet: string) => {
    if (!confirm('¿Reversar asistencia de ' + ficha?.colaborador.nombre + '?')) return;
    setReverting(1);
    try {
      await api.post('/attendance/revert', { eventoId: EVENTO_ACTIVO_ID, carnet });
      show('↺ Asistencia revertida');
      const data = await getColaboradorFull(carnet, EVENTO_ACTIVO_ID);
      setFicha(data);
      await loadAsistidos(pagina);
    } catch { show('Error al revertir', 'error'); }
    finally { setReverting(null); }
  };

  const totalAdultos = allAsistidos.reduce((s, r) => s + (r.TotalAdultos || 0), 0);
  const totalHijosAsist = allAsistidos.reduce((s, r) => s + r.TotalHijos, 0);

  const stats = [
    { label: 'Total Niños Censados', value: kpis?.TotalNinos || 0, gradient: 'linear-gradient(135deg, #374151 0%, #111827 100%)', icon: Package, bg: 'rgba(255,255,255,0.1)' },
    { label: 'Adultos Registrados', value: totalAdultos, gradient: 'linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%)', icon: UserCheck, bg: 'rgba(255,255,255,0.1)' },
    { label: 'Niños Registrados', value: totalHijosAsist, gradient: 'linear-gradient(135deg, #10b981 0%, #059669 100%)', icon: Heart, bg: 'rgba(255,255,255,0.1)' },
  ];

  return (
    <div className="app-shell" style={{ background: '#f8f9fa', minHeight: '100vh' }}>
      <header style={{ background: 'linear-gradient(135deg, #da121a 0%, #1e1e1e 100%)', color: 'white' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px 24px' }}>
          <button onClick={() => navigate('/')} style={{ background: 'rgba(255,255,255,0.15)', border: 'none', color: 'white', borderRadius: '20px', padding: '6px 16px', fontWeight: 700, fontSize: 13, cursor: 'pointer' }}>
            <ChevronLeft className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Dashboard
          </button>
          <h2 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, fontSize: 22, margin: 0 }}>
            <Gift className="w-5 h-5" style={{ verticalAlign: 'middle', marginRight: 8 }} /> Registro de Asistencia
          </h2>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span style={{ fontSize: 12, opacity: 0.8 }}>{user?.nombre}</span>
            <button onClick={logout} style={{ background: 'rgba(255,255,255,0.15)', border: 'none', color: 'white', borderRadius: 8, padding: '6px 10px', cursor: 'pointer', fontSize: 12 }}>Salir</button>
          </div>
        </div>
      </header>

      <main style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
        {/* KPIs */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 24 }}>
          {stats.map((s) => (
            <div key={s.label} style={{ background: s.gradient, color: 'white', borderRadius: 10, padding: '10px 14px', display: 'flex', alignItems: 'center', gap: 10 }}>
              <div style={{ width: 36, height: 36, borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(255,255,255,0.2)' }}>
                <s.icon className="w-5 h-5" />
              </div>
              <div>
                <div style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 800, fontSize: 22, lineHeight: 1 }}>{s.value}</div>
                <div style={{ fontSize: 10, fontWeight: 600, opacity: 0.85, marginTop: 2 }}>{s.label}</div>
              </div>
            </div>
          ))}
        </div>

        {/* Buscar + info colaborador */}
        <div style={{ display: 'grid', gridTemplateColumns: '5fr 7fr', gap: 20, marginBottom: 24 }}>
          <section style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
            <div style={{ background: '#da121a', color: 'white', padding: '10px 16px', borderRadius: '12px 12px 0 0', fontWeight: 700, fontSize: 14 }}>
              <Search className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 6 }} /> Buscar Colaborador
            </div>
            <div style={{ padding: 16 }}>
              <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
                <input type="text" value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && buscar()}
                  placeholder="Buscar por carnet o nombre..."
                  style={{ flex: 1, padding: '10px 14px', borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14, outline: 'none' }} />
                <button onClick={buscar} disabled={loading}
                  style={{ background: 'linear-gradient(135deg, #da121a 0%, #1e1e1e 100%)', border: 'none', color: 'white', borderRadius: 8, padding: '10px 20px', fontWeight: 600, fontSize: 13, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 6 }}>
                  {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Search className="w-4 h-4" />} Buscar
                </button>
              </div>

              {/* Search results (names) */}
              {searchingName && (
                <div style={{ textAlign: 'center', padding: 20, color: '#9ca3af' }}>
                  <Loader2 className="w-5 h-5" style={{ margin: '0 auto 8px', animation: 'spin 0.8s linear infinite' }} />
                  <p style={{ fontSize: 12 }}>Buscando...</p>
                </div>
              )}
              {searchResults && searchResults.length > 0 && (
                <div style={{ marginBottom: 16 }}>
                  <p style={{ fontSize: 12, fontWeight: 700, color: '#374151', marginBottom: 8 }}>Seleccione un colaborador:</p>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                    {searchResults.map((r: any) => (
                      <button key={r.carnet} onClick={() => seleccionarResultado(r.carnet)}
                        style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 12px', borderRadius: 8, border: '1px solid #e5e7eb', background: 'white', cursor: 'pointer', textAlign: 'left', width: '100%', fontSize: 13 }}>
                        <div style={{ width: 32, height: 32, borderRadius: '50%', background: '#da121a', color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 12, flexShrink: 0 }}>
                          {r.nombre.charAt(0)}
                        </div>
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div style={{ fontWeight: 600 }}>{r.nombre}</div>
                          <div style={{ fontSize: 11, color: '#6b7280' }}>{r.carnet} · {r.gerencia || ''}</div>
                        </div>
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {ficha && (
                <div style={{ display: 'flex', gap: 16 }}>
                  <div style={{ textAlign: 'center' }}>
                    {ficha.fotoHcm ? (
                      <img src={ficha.fotoHcm} alt="Foto" style={{ width: 140, height: 140, borderRadius: '50%', objectFit: 'cover', boxShadow: '0 8px 24px rgba(0,0,0,0.15)' }} />
                    ) : (
                      <div style={{ width: 140, height: 140, borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#e2e8f0', fontSize: 48, fontWeight: 700, color: '#da121a', boxShadow: '0 8px 24px rgba(0,0,0,0.15)' }}>
                        {ficha.colaborador.nombre.charAt(0)}
                      </div>
                    )}
                    {/* Foto label removed per request */}
                  </div>
                  <div style={{ flex: 1 }}>
                    <h3 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, margin: '0 0 2px', fontSize: 16 }}>{ficha.colaborador.nombre}</h3>
                    <p style={{ color: '#da121a', fontSize: 12, fontWeight: 700, margin: '4px 0' }}>Carnet: {ficha.colaborador.carnet}</p>
                    <p style={{ color: '#6b7280', fontSize: 13, margin: 0 }}>{ficha.colaborador.gerencia || '-'} · {ficha.colaborador.ubicacion || '-'}</p>

                    {ficha.asistio ? (
                      <div style={{ marginTop: 10, display: 'flex', gap: 8, alignItems: 'center' }}>
                        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '6px 12px', background: '#d1fae5', color: '#065f46', borderRadius: 6, fontWeight: 700, fontSize: 12 }}>
                          <Check className="w-4 h-4" /> Asistió {ficha.fechaAsistencia ? new Date(ficha.fechaAsistencia).toLocaleTimeString() : ''}
                          {ficha.asistioPor && ficha.asistioPor !== 'COLABORADOR' && (
                            <span style={{ fontSize: 10, opacity: 0.8 }}>
                              ({ficha.asistioPor === 'CONYUGE' ? 'Cónyuge' : ficha.nombreAsistente || 'Tercero'})
                            </span>
                          )}
                        </span>
                        <button onClick={() => handleRevertir(ficha.colaborador.carnet)} disabled={reverting !== null}
                          style={{ background: '#fee2e2', color: '#dc2626', border: 'none', borderRadius: 6, padding: '6px 12px', fontWeight: 700, fontSize: 12, cursor: 'pointer' }}>
                          <RotateCcw className="w-3 h-3" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Reversar
                        </button>
                      </div>
                    ) : (
                      <div style={{ marginTop: 10 }}>
                        <div style={{ display: 'flex', gap: 12, marginBottom: 10 }}>
                          <div>
                            <label style={{ fontSize: 11, fontWeight: 700, color: '#6b7280', marginBottom: 4, display: 'block' }}>Adultos</label>
                            <input type="number" min="0" value={adultos} onChange={(e) => setAdultos(Math.max(0, parseInt(e.target.value) || 0))}
                              style={{ width: 70, padding: '6px 10px', borderRadius: 6, border: '1px solid #e5e7eb', fontSize: 14, textAlign: 'center', fontWeight: 700 }} />
                          </div>
                          <div>
                            <label style={{ fontSize: 11, fontWeight: 700, color: '#6b7280', marginBottom: 4, display: 'block' }}>Niños</label>
                            <input type="number" min="0" value={ninos} onChange={(e) => setNinos(Math.max(0, parseInt(e.target.value) || 0))}
                              style={{ width: 70, padding: '6px 10px', borderRadius: 6, border: '1px solid #e5e7eb', fontSize: 14, textAlign: 'center', fontWeight: 700 }} />
                          </div>
                        </div>
                        <div style={{ marginBottom: 10 }}>
                          <label style={{ fontSize: 11, fontWeight: 700, color: '#6b7280', marginBottom: 4, display: 'block' }}>¿Quién asiste?</label>
                          <div style={{ display: 'flex', gap: 6 }}>
                            {['COLABORADOR', 'CONYUGE', 'TERCERO'].map(r => (
                              <button key={r} onClick={() => { setAsistioPor(r); setNombreAsistente(''); }}
                                style={{ padding: '5px 10px', borderRadius: 6, border: 'none', fontWeight: 600, fontSize: 11, cursor: 'pointer',
                                  ...(asistioPor === r ? { background: '#da121a', color: 'white' } : { background: '#f3f4f6', color: '#6b7280' }) }}>
                                {r === 'COLABORADOR' ? 'Colaborador' : r === 'CONYUGE' ? 'Cónyuge' : 'Tercero'}
                              </button>
                            ))}
                          </div>
                          {asistioPor === 'TERCERO' && (
                            <input type="text" value={nombreAsistente} onChange={e => setNombreAsistente(e.target.value)}
                              placeholder="Nombre de quien asiste"
                              style={{ marginTop: 6, width: '100%', padding: '6px 10px', borderRadius: 6, border: '1px solid #e5e7eb', fontSize: 12, boxSizing: 'border-box' }} />
                          )}
                        </div>
                        <button onClick={handleRegistrar} disabled={registering}
                          style={{ background: '#10b981', color: 'white', border: 'none', borderRadius: 8, padding: '10px 20px', fontWeight: 700, fontSize: 13, cursor: 'pointer', display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                          {registering ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />} Registrar Asistencia
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              )}

              {!ficha && !loading && (
                <div style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>
                  <Search className="w-10 h-10" style={{ opacity: 0.3, margin: '0 auto 12px', display: 'block' }} />
                  <p>Ingrese un carnet para buscar</p>
                </div>
              )}
            </div>
          </section>

          {/* Info familiar */}
          <section style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
            <div style={{ background: '#da121a', color: 'white', padding: '10px 16px', borderRadius: '12px 12px 0 0', fontWeight: 700, fontSize: 14 }}>
              <Users className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 6 }} /> Información Familiar
            </div>
            <div style={{ padding: 16 }}>
              {ficha ? (
                ficha.hijos.length > 0 ? (
                  <div style={{ overflowX: 'auto' }}>
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                      <thead><tr style={{ background: '#da121a', color: 'white' }}>
                        <th style={{ padding: '8px 12px', textAlign: 'left', fontWeight: 600 }}>Nombre</th>
                        <th style={{ padding: '8px 12px', textAlign: 'left', fontWeight: 600 }}>Edad</th>
                        <th style={{ padding: '8px 12px', textAlign: 'left', fontWeight: 600 }}>Género</th>
                        <th style={{ padding: '8px 12px', textAlign: 'left', fontWeight: 600 }}>Categoría</th>
                        <th style={{ padding: '8px 12px', textAlign: 'center', fontWeight: 600 }}>Estado</th>
                      </tr></thead>
                      <tbody>
                        {ficha.hijos.map((hijo) => (
                          <tr key={hijo.id} style={{ background: hijo.edadHijo <= 12 ? '#FFFACD' : 'transparent', borderBottom: '1px solid #e5e7eb' }}>
                            <td style={{ padding: '8px 12px', fontWeight: 600 }}>{hijo.generoHijo === 'F' ? '👧' : '👦'} {hijo.nombreHijo}</td>
                            <td style={{ padding: '8px 12px' }}>{hijo.edadHijo}</td>
                            <td style={{ padding: '8px 12px' }}>{hijo.generoHijo === 'F' ? 'Femenino' : 'Masculino'}</td>
                            <td style={{ padding: '8px 12px' }}>{hijo.categoria}</td>
                            <td style={{ padding: '8px 12px', textAlign: 'center' }}>
                              {hijo.estadoEntrega === 'DELIVERED'
                                ? <span style={{ background: '#d1fae5', color: '#065f46', padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700 }}>Entregado</span>
                                : <span style={{ background: '#fef3c7', color: '#92400e', padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700 }}>Pendiente</span>
                              }
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>
                    <Users className="w-10 h-10" style={{ opacity: 0.3, margin: '0 auto 12px', display: 'block' }} />
                    <p>No hay hijos registrados para este colaborador.</p>
                  </div>
                )
              ) : (
                <div style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>
                  <Users className="w-10 h-10" style={{ opacity: 0.3, margin: '0 auto 12px', display: 'block' }} />
                  <p>Seleccione un colaborador para ver su información familiar</p>
                </div>
              )}

              {/* Contactos (desde Oracle HCM) - sin duplicar los que ya salen en hijos */}
              {ficha?.familiaresHcm && ficha.familiaresHcm.length > 0 && (() => {
                // Normalizar nombres: invertir orden "APELLIDO NOMBRE" -> "NOMBRE APELLIDO" y viceversa
                const normalizar = (n: string) => n.toUpperCase().trim().replace(/\s+/g, ' ').split(' ').sort().join(' ');
                const nombresHijos = new Set(ficha.hijos.map(h => normalizar(h.nombreHijo)));
                const contactosUnicos = ficha.familiaresHcm.filter(f => !nombresHijos.has(normalizar(f.nombre)));
                if (contactosUnicos.length === 0) return null;
                return (
                  <div style={{ marginTop: 16 }}>
                    <p style={{ fontSize: 13, fontWeight: 700, color: '#da121a', marginBottom: 8 }}>
                      <Users className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 6 }} /> Contactos
                    </p>
                    <div style={{ overflowX: 'auto' }}>
                      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
                        <thead>
                          <tr style={{ background: '#f8fafc' }}>
                            <th style={{ padding: '6px 10px', textAlign: 'left', fontSize: 10, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Nombre</th>
                            <th style={{ padding: '6px 10px', textAlign: 'left', fontSize: 10, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Parentesco</th>
                            <th style={{ padding: '6px 10px', textAlign: 'center', fontSize: 10, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Edad</th>
                          </tr>
                        </thead>
                        <tbody>
                          {contactosUnicos.map((fam, i) => (
                            <tr key={i} style={{ borderBottom: '1px solid #e5e7eb', background: fam.tipoRela?.includes('HIJO') && fam.edad <= 12 ? '#FFFACD' : 'transparent' }}>
                              <td style={{ padding: '6px 10px', fontWeight: 600 }}>{fam.nombre}</td>
                              <td style={{ padding: '6px 10px', fontSize: 11 }}>{fam.tipoRela}</td>
                              <td style={{ padding: '6px 10px', textAlign: 'center' }}>{fam.edad}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                );
              })()}
            </div>
          </section>
        </div>

        {/* Tabla de registrados (como EventoLiga) */}
        <section style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
          <div style={{ background: '#da121a', color: 'white', padding: '10px 16px', borderRadius: '12px 12px 0 0', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>
            <span style={{ fontWeight: 700, fontSize: 14 }}>
              <Users className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 6 }} /> Asistieron ({totalAsistidos})
            </span>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <input type="text" value={filterTexto} onChange={(e) => { setFilterTexto(e.target.value); setPagina(1); }}
                placeholder="Filtrar por nombre o carnet..."
                style={{ padding: '5px 10px', borderRadius: 6, border: '1px solid rgba(255,255,255,0.3)', fontSize: 12, outline: 'none', background: 'rgba(255,255,255,0.15)', color: 'white', width: 200 }}
              />
              <button onClick={handleExportExcel}
                style={{ background: 'rgba(255,255,255,0.2)', border: 'none', color: 'white', borderRadius: 6, padding: '5px 12px', fontWeight: 600, fontSize: 11, cursor: 'pointer' }}>
                📥 Excel
              </button>
            </div>
          </div>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
              <thead>
                <tr style={{ background: '#f8fafc' }}>
                  <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Carnet</th>
                  <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Nombre</th>
                  <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Gerencia</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Adultos</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Niños</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Asiste</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Hijos</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Entreg.</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Fecha</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Reg.</th>
                  <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Acción</th>
                </tr>
              </thead>
              <tbody>
                {(() => {
                  const filtrados = filterTexto
                    ? asistidos.filter(r => (r.Carnet+' '+r.Nombre).toUpperCase().includes(filterTexto.toUpperCase()))
                    : asistidos;
                  const paginados = filtrados.slice((pagina - 1) * porPagina, pagina * porPagina);
                  let sumAdultos = 0, sumNinos = 0;
                  const rows = paginados.map((row) => {
                    sumAdultos += row.TotalAdultos || 0;
                    sumNinos += row.TotalNinos || 0;
                    return (
                      <tr key={row.Carnet} style={{ borderBottom: '1px solid #e5e7eb' }}>
                        <td style={{ padding: '10px 16px', fontWeight: 700, color: '#da121a', fontSize: 12 }}>{row.Carnet}</td>
                        <td style={{ padding: '10px 16px', fontWeight: 600 }}>{row.Nombre}</td>
                        <td style={{ padding: '10px 16px', color: '#6b7280', fontSize: 12 }}>{row.Gerencia || '-'}</td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>{row.TotalAdultos || 0}</td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>{row.TotalNinos || 0}</td>
                        <td style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11 }}>{asistioLabel(row)}</td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>{row.TotalHijos}</td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                          <span style={{ fontWeight: 700, color: row.Entregados === row.TotalHijos ? '#10b981' : '#6b7280' }}>
                            {row.Entregados}/{row.TotalHijos}
                          </span>
                        </td>
                        <td style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, color: '#6b7280' }}>
                          {row.FechaAsistencia ? new Date(row.FechaAsistencia).toLocaleString() : '-'}
                        </td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                          <button onClick={() => setInfoCarnet(infoCarnet === row.Carnet ? null : row.Carnet)}
                            style={{ background: 'none', border: '1px solid #e5e7eb', borderRadius: 6, padding: '4px 6px', cursor: 'pointer', fontSize: 14 }}>
                            👁️
                          </button>
                          {infoCarnet === row.Carnet && (
                            <div style={{ position: 'absolute', background: 'white', border: '1px solid #e5e7eb', borderRadius: 8, padding: 8, boxShadow: '0 4px 12px rgba(0,0,0,0.15)', zIndex: 10, fontSize: 11, minWidth: 150, marginTop: 4 }}>
                              <strong>Registrado por:</strong> {row.RegistradoPor || '—'}<br />
                              <strong>Adultos:</strong> {row.TotalAdultos || 0}<br />
                              <strong>Niños:</strong> {row.TotalNinos || 0}
                            </div>
                          )}
                        </td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                          <button onClick={() => handleRevertir(row.Carnet)}
                            style={{ background: '#fee2e2', color: '#dc2626', border: 'none', borderRadius: 6, padding: '4px 10px', fontWeight: 600, fontSize: 11, cursor: 'pointer' }}>
                            <RotateCcw className="w-3 h-3" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Reversar
                          </button>
                        </td>
                      </tr>
                    );
                  });
                  if (rows.length > 0) {
                    rows.push(
                      <tr key="totales" style={{ background: '#f1f5f9', fontWeight: 700 }}>
                        <td colSpan={3} style={{ padding: '10px 16px', textAlign: 'right', fontSize: 12 }}>TOTALES:</td>
                        <td style={{ padding: '10px 16px', textAlign: 'center', color: '#da121a', fontSize: 14 }}>{sumAdultos}</td>
                        <td style={{ padding: '10px 16px', textAlign: 'center', color: '#da121a', fontSize: 14 }}>{sumNinos}</td>
                        <td colSpan={5}></td>
                      </tr>
                    );
                  }
                  return rows;
                })()}
                {asistidos.length === 0 && (
                  <tr><td colSpan={8} style={{ textAlign: 'center', padding: 30, color: '#9ca3af' }}>No hay colaboradores que hayan asistido</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '10px 20px', borderTop: '1px solid #e5e7eb' }}>
            <span style={{ fontSize: 12, color: '#6b7280' }}>
              {(() => { const f = filterTexto ? asistidos.filter(r => (r.Carnet+' '+r.Nombre).toUpperCase().includes(filterTexto.toUpperCase())).length : totalAsistidos; return `Pág ${pagina} de ${Math.ceil(Math.max(1,f)/porPagina)} (${f} registros)`; })()}
            </span>
            <div style={{ display: 'flex', gap: 6 }}>
              <button onClick={() => setPagina(p => Math.max(1, p - 1))} disabled={pagina === 1}
                style={{ padding: '5px 12px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, cursor: pagina === 1 ? 'not-allowed' : 'pointer', opacity: pagina === 1 ? 0.4 : 1 }}>
                ← Anterior
              </button>
              <button onClick={() => setPagina(p => Math.min(Math.ceil(totalAsistidos/porPagina), p + 1))} disabled={pagina >= Math.ceil(totalAsistidos/porPagina)}
                style={{ padding: '5px 12px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, cursor: pagina >= Math.ceil(totalAsistidos/porPagina) ? 'not-allowed' : 'pointer', opacity: pagina >= Math.ceil(totalAsistidos/porPagina) ? 0.4 : 1 }}>
                Siguiente →
              </button>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
