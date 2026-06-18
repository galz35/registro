import { useState, useCallback, useEffect } from 'react';
import { usePolling } from '../hooks/usePolling';
import { useDebounce } from '../hooks/useDebounce';
import { getSummary, getCenso, getColaboradorFull, registrarAsistencia, registrarEntrega, reversarEntrega } from '../services/asistencia.api';
import type { CensoItem, ColaboradorFicha, DashboardKPI, Hijo } from '../types';
import { LayoutDashboard, Search, Gift, Camera, X, RotateCcw } from 'lucide-react';

const EVENTO_ACTIVO_ID = 1;

export default function CommandCenter() {
  const [globalSearch, setGlobalSearch] = useState('');
  const debouncedSearch = useDebounce(globalSearch, 300);
  const [pagina, setPagina] = useState(1);
  const [filtroEstado, setFiltroEstado] = useState<string | undefined>(undefined);

  const kpiFetcher = useCallback(() => getSummary(EVENTO_ACTIVO_ID), []);
  const { data: kpis } = usePolling<DashboardKPI>(kpiFetcher, 30000);

  const [censoData, setCensoData] = useState<{ data: CensoItem[]; total: number; totalPaginas: number } | null>(null);
  const loadCensoData = useCallback(async () => {
    try {
      const data = await getCenso(EVENTO_ACTIVO_ID, debouncedSearch || undefined, filtroEstado, pagina, 50);
      setCensoData(data);
    } catch {}
  }, [debouncedSearch, filtroEstado, pagina]);

  useEffect(() => { loadCensoData(); }, [loadCensoData]);

  const [drawerCarnet, setDrawerCarnet] = useState<string | null>(null);
  const [drawerData, setDrawerData] = useState<ColaboradorFicha | null>(null);
  const [showDeliverModal, setShowDeliverModal] = useState<Hijo | null>(null);
  const [showRevertDialog, setShowRevertDialog] = useState<{ hijo: Hijo; entregaId: number } | null>(null);
  const [notif, setNotif] = useState<{ msg: string; type: 'success' | 'error' } | null>(null);

  const showNotif = (msg: string, type: 'success' | 'error' = 'success') => {
    setNotif({ msg, type });
    setTimeout(() => setNotif(null), 3000);
  };

  const openDrawer = async (carnet: string) => {
    setDrawerCarnet(carnet);
    try {
      const data = await getColaboradorFull(carnet, EVENTO_ACTIVO_ID);
      setDrawerData(data);
    } catch {
      showNotif('Error al cargar datos', 'error');
    }
  };

  const handleRegistrarAsistencia = async (carnet: string) => {
    try {
      await registrarAsistencia(EVENTO_ACTIVO_ID, carnet);
      showNotif('Asistencia registrada');
      await loadCensoData();
      if (drawerCarnet === carnet) openDrawer(carnet);
    } catch {
      showNotif('Error al registrar asistencia', 'error');
    }
  };

  const handleEntrega = async (hijoId: number, jugueteId: number, recibidoPor: string, nombreReceptor: string | null, carnetColab: string, foto?: File) => {
    const fd = new FormData();
    fd.append('eventoId', String(EVENTO_ACTIVO_ID));
    fd.append('hijoId', String(hijoId));
    fd.append('jugueteId', String(jugueteId));
    fd.append('carnetColaborador', carnetColab);
    fd.append('recibidoPor', recibidoPor);
    if (nombreReceptor) fd.append('nombreReceptor', nombreReceptor);
    if (foto) fd.append('foto', foto);
    try {
      await registrarEntrega(fd);
      showNotif('Entrega registrada');
      setShowDeliverModal(null);
      await loadCensoData();
      if (drawerCarnet) openDrawer(drawerCarnet);
    } catch {
      showNotif('Error al registrar entrega', 'error');
    }
  };

  const handleReversar = async (entregaId: number, motivo: string) => {
    try {
      await reversarEntrega(entregaId, motivo);
      showNotif('Entrega reversada');
      setShowRevertDialog(null);
      await loadCensoData();
      if (drawerCarnet) openDrawer(drawerCarnet);
    } catch {
      showNotif('Error al reversar', 'error');
    }
  };

  const stats = [
    { label: 'Total Niños', value: kpis?.TotalNinos || 0, gradient: 'linear-gradient(135deg, #374151 0%, #111827 100%)' },
    { label: 'Entregados', value: kpis?.Entregados || 0, gradient: 'linear-gradient(135deg, #10b981 0%, #059669 100%)' },
    { label: 'Pendientes', value: kpis?.Pendientes || 0, gradient: 'linear-gradient(135deg, #9ca3af 0%, #4b5563 100%)' },
  ];

  return (
    <div style={{ background: '#f8f9fa', minHeight: '100vh' }}>
      {/* Gradient header */}
      <header style={{ background: 'linear-gradient(135deg, #da121a 0%, #1e1e1e 100%)', color: 'white' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '14px 24px' }}>
          <h1 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, fontSize: 22, margin: 0 }}>
            <LayoutDashboard className="w-5 h-5" style={{ verticalAlign: 'middle', marginRight: 10 }} /> Dashboard
          </h1>
          <div style={{ position: 'relative', width: 320 }}>
            <Search className="w-4 h-4" style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)', color: 'rgba(255,255,255,0.5)' }} />
            <input type="text" value={globalSearch} onChange={(e) => { setGlobalSearch(e.target.value); setPagina(1); }}
              onKeyDown={(e) => e.key === 'Enter' && setPagina(1)}
              placeholder="Buscar carnet, colaborador o hijo..."
              style={{ width: '100%', padding: '8px 12px 8px 34px', borderRadius: 8, border: '1px solid rgba(255,255,255,0.2)', fontSize: 13, outline: 'none', background: 'rgba(255,255,255,0.12)', color: 'white' }} />
          </div>
        </div>
        <p style={{ fontSize: 12, opacity: 0.75, textAlign: 'center', margin: 0, padding: '0 24px 10px' }}>
          Panel de control - Día del Niño 2026
        </p>
      </header>

      <main style={{ padding: 24, maxWidth: 1280, margin: '0 auto' }}>
            {/* Compact KPIs */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 20 }}>
              {stats.map((s) => (
                <div key={s.label} className="stat-card dashboard-stat" style={{ background: s.gradient }}>
                  <div className="stat-card__value">{s.value}</div>
                  <div className="stat-card__label">{s.label}</div>
                </div>
              ))}
            </div>

            {/* Table with filters + search + pagination */}
            <section className="card" style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
              <div style={{ padding: '14px 20px', borderBottom: '1px solid #e5e7eb', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>
                <div style={{ display: 'flex', gap: 6 }}>
                  {[{ id: undefined, label: 'Todos' }, { id: 'pendientes', label: 'Pendientes' }, { id: 'completos', label: 'Completos' }].map((f) => (
                    <button key={f.label} onClick={() => { setFiltroEstado(f.id); setPagina(1); }}
                      style={{
                        padding: '5px 12px', borderRadius: 6, border: 'none', fontSize: 11, fontWeight: 700, cursor: 'pointer', textTransform: 'uppercase', letterSpacing: '0.3px',
                        ...((filtroEstado === f.id || (!filtroEstado && !f.id)) ? { background: '#da121a', color: 'white' } : { background: '#f1f5f9', color: '#6b7280' })
                      }}>
                      {f.label}
                    </button>
                  ))}
                </div>
              </div>
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                  <thead>
                    <tr style={{ background: '#f8fafc' }}>
                      <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Carnet</th>
                      <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Colaborador</th>
                      <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Gerencia</th>
                      <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Hijos</th>
                      <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Entreg.</th>
                      <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Asist.</th>
                      <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Acción</th>
                    </tr>
                  </thead>
                  <tbody>
                    {censoData?.data.map((row) => (
                      <tr key={row.Carnet} style={{ borderBottom: '1px solid #e5e7eb' }}>
                        <td style={{ padding: '10px 16px' }}><span style={{ fontWeight: 700, fontSize: 12, color: '#da121a' }}>{row.Carnet}</span></td>
                        <td style={{ padding: '10px 16px', fontWeight: 600 }}>{row.Nombre}</td>
                        <td style={{ padding: '10px 16px', color: '#6b7280', fontSize: 12 }}>{row.Gerencia || '-'}</td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>{row.TotalHijos}</td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                          <span style={{ fontWeight: 700, color: row.Entregados === row.TotalHijos ? '#10b981' : '#6b7280' }}>
                            {row.Entregados}/{row.TotalHijos}
                          </span>
                        </td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                          {row.Asistio > 0
                            ? <span style={{ background: '#d1fae5', color: '#065f46', padding: '2px 7px', borderRadius: 4, fontSize: 10, fontWeight: 700 }}>✅</span>
                            : <span style={{ color: '#9ca3af', fontSize: 11 }}>—</span>
                          }
                        </td>
                        <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                          <button onClick={() => openDrawer(row.Carnet)}
                            style={{ background: '#da121a', color: 'white', border: 'none', borderRadius: 6, padding: '5px 12px', fontWeight: 600, fontSize: 11, cursor: 'pointer' }}>
                            Ver
                          </button>
                        </td>
                      </tr>
                    ))}
                    {(!censoData || censoData.data.length === 0) && (
                      <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>No se encontraron registros</td></tr>
                    )}
                  </tbody>
                </table>
              </div>
              {censoData && censoData.totalPaginas > 1 && (
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '10px 20px', borderTop: '1px solid #e5e7eb' }}>
                  <span style={{ fontSize: 12, color: '#6b7280' }}>
                    Pág {pagina} de {censoData.totalPaginas} ({censoData.total} registros)
                  </span>
                  <div style={{ display: 'flex', gap: 6 }}>
                    <button onClick={() => setPagina(p => Math.max(1, p - 1))} disabled={pagina === 1}
                      style={{ padding: '5px 12px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, cursor: pagina === 1 ? 'not-allowed' : 'pointer', opacity: pagina === 1 ? 0.4 : 1 }}>
                      ← Anterior
                    </button>
                    <button onClick={() => setPagina(p => Math.min(censoData.totalPaginas, p + 1))} disabled={pagina >= censoData.totalPaginas}
                      style={{ padding: '5px 12px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, cursor: pagina >= censoData.totalPaginas ? 'not-allowed' : 'pointer', opacity: pagina >= censoData.totalPaginas ? 0.4 : 1 }}>
                      Siguiente →
                    </button>
                  </div>
                </div>
              )}
            </section>
      </main>

      {drawerCarnet && drawerData && (
        <Drawer
          data={drawerData}
          onClose={() => setDrawerCarnet(null)}
          onRegistrarAsistencia={handleRegistrarAsistencia}
          onDeliver={setShowDeliverModal}
          onRevert={(hijo, entregaId) => setShowRevertDialog({ hijo, entregaId })}
        />
      )}

      {showDeliverModal && drawerData && (
        <DeliverModal
          hijo={showDeliverModal}
          carnetColaborador={drawerData.colaborador.carnet}
          onConfirm={handleEntrega}
          onClose={() => setShowDeliverModal(null)}
        />
      )}

      {showRevertDialog && (
        <RevertDialog
          hijo={showRevertDialog.hijo}
          onConfirm={(motivo) => handleReversar(showRevertDialog.entregaId, motivo)}
          onClose={() => setShowRevertDialog(null)}
        />
      )}

      {notif && <div className={`toast toast--${notif.type}`}>{notif.msg}</div>}
    </div>
  );
}

function Drawer({ data, onClose, onRegistrarAsistencia, onDeliver, onRevert }: {
  data: ColaboradorFicha;
  onClose: () => void;
  onRegistrarAsistencia: (carnet: string) => void;
  onDeliver: (hijo: Hijo) => void;
  onRevert: (hijo: Hijo, entregaId: number) => void;
}) {
  return (
    <div className="fixed inset-0 z-50 flex justify-end" onClick={onClose}>
      <div className="absolute inset-0" style={{ background: 'rgba(15,23,42,0.45)' }} />
      <aside className="relative w-full max-w-[500px] bg-white border-l overflow-auto p-5" onClick={(e) => e.stopPropagation()} style={{ borderColor: 'var(--border)' }}>
        <div className="flex justify-between items-center mb-5">
          <h2 className="m-0 text-lg font-extrabold">Colaborador</h2>
          <button onClick={onClose} className="btn-icon" style={{ color: '#94A3B8' }}>
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="profile-summary mb-5">
          <div className="avatar">{data.colaborador.nombre.charAt(0)}</div>
          <div className="min-w-0">
            <h3 className="m-0 text-base font-extrabold">{data.colaborador.nombre}</h3>
            <p className="mt-1 mb-1 text-xs font-bold" style={{ color: '#DA291C' }}>Carnet: {data.colaborador.carnet}</p>
            <p className="m-0 text-xs" style={{ color: '#64748B' }}>{data.colaborador.gerencia || '-'} · {data.colaborador.ubicacion || '-'}</p>
          </div>
        </div>

        <div className="mb-5">
          {data.asistio ? (
            <div className="badge" style={{ background: '#D1FAE5', color: '#065F46' }}>
              Asistió {data.fechaAsistencia ? new Date(data.fechaAsistencia).toLocaleTimeString() : ''}
            </div>
          ) : (
            <button onClick={() => onRegistrarAsistencia(data.colaborador.carnet)} className="btn btn-primary w-full">
              Registrar Asistencia
            </button>
          )}
        </div>

        <h4 className="text-sm font-extrabold mb-3">
          Hijos ({data.hijos.filter((h) => h.estadoEntrega === 'DELIVERED').length} de {data.hijos.length} entregados)
        </h4>

        <div className="child-list">
          {data.hijos.map((hijo) => (
            <div key={hijo.id} className={`child-card ${hijo.estadoEntrega === 'DELIVERED' ? 'child-card--delivered' : 'child-card--pending'}`}>
              <div className="child-card__main">
                <div className="min-w-0">
                  <p className="m-0 text-sm font-extrabold">{hijo.nombreHijo}</p>
                  <p className="mt-1 mb-0 text-xs" style={{ color: '#64748B' }}>{hijo.categoria} · {hijo.edadHijo} años</p>
                  {hijo.jugueteSugerido && (
                    <p className="mt-2 mb-0 text-xs font-semibold">
                      {hijo.jugueteSugerido.nombreJuguete}
                      <span style={{ color: hijo.jugueteSugerido.stockActual > 0 ? '#10B981' : '#EF4444' }}>
                        {' '}Stock: {hijo.jugueteSugerido.stockActual}
                      </span>
                    </p>
                  )}
                </div>
                <div className="child-card__actions">
                  {hijo.estadoEntrega === 'DELIVERED' ? (
                    <>
                      <span className="badge" style={{ background: '#D1FAE5', color: '#065F46' }}>Entregado</span>
                      <button onClick={() => onRevert(hijo, hijo.entregaId!)} className="btn btn-sm btn-outline">
                        <RotateCcw className="w-3 h-3" />
                        Reversar
                      </button>
                    </>
                  ) : (
                    <button onClick={() => onDeliver(hijo)} className="btn btn-sm btn-primary">
                      <Gift className="w-3 h-3" />
                      Entregar
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </aside>
    </div>
  );
}

function DeliverModal({ hijo, carnetColaborador, onConfirm, onClose }: {
  hijo: Hijo;
  carnetColaborador: string;
  onConfirm: (hijoId: number, jugueteId: number, recibidoPor: string, nombreReceptor: string | null, carnetColab: string, foto?: File) => void;
  onClose: () => void;
}) {
  const [recibidoPor, setRecibidoPor] = useState('COLABORADOR');
  const [nombreReceptor, setNombreReceptor] = useState('');
  const [foto, setFoto] = useState<File | null>(null);
  const jugueteId = hijo.jugueteSugerido?.id || 0;

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center" onClick={onClose}>
      <div className="absolute inset-0" style={{ background: 'rgba(15,23,42,0.45)' }} />
      <div className="modal-card relative" onClick={(e) => e.stopPropagation()}>
        <div className="flex justify-between items-center mb-4">
          <h3 className="m-0 text-lg font-extrabold">Entregar</h3>
          <button onClick={onClose} className="btn-icon" style={{ color: '#94A3B8' }}><X className="w-4 h-4" /></button>
        </div>
        <div className="grid gap-4">
          <div>
            <label>Juguete sugerido</label>
            <p className="m-0 text-sm font-bold">{hijo.jugueteSugerido?.nombreJuguete || 'Sin sugerencia'}</p>
          </div>
          <div>
            <label>Recibido por</label>
            <div className="grid grid-cols-3 gap-2">
              {['COLABORADOR', 'CONYUGE', 'TERCERO'].map((r) => (
                <button key={r} onClick={() => setRecibidoPor(r)} className="btn btn-sm"
                  style={recibidoPor === r ? { background: '#DA291C', color: 'white' } : { background: '#F1F5F9', color: '#64748B' }}>
                  {r === 'COLABORADOR' ? 'Colaborador' : r === 'CONYUGE' ? 'Cónyuge' : 'Tercero'}
                </button>
              ))}
            </div>
            {recibidoPor === 'TERCERO' && (
              <input type="text" value={nombreReceptor} onChange={(e) => setNombreReceptor(e.target.value)} placeholder="Nombre del tercero" className="mt-2" />
            )}
          </div>
          <div>
            <label>Foto evidencia (opcional)</label>
            <label className="flex items-center gap-2 px-4 py-3 rounded-lg cursor-pointer" style={{ background: '#F8FAFC', border: '1px solid #E2E8F0' }}>
              <Camera className="w-4 h-4" style={{ color: '#94A3B8' }} />
              <span className="text-sm" style={{ color: '#64748B' }}>{foto ? foto.name : 'Seleccionar foto'}</span>
              <input type="file" accept="image/*" style={{ display: 'none' }} onChange={(e) => setFoto(e.target.files?.[0] || null)} />
            </label>
          </div>
          <button onClick={() => onConfirm(hijo.id, jugueteId, recibidoPor, nombreReceptor || null, carnetColaborador, foto || undefined)} className="btn btn-primary">
            Confirmar Entrega
          </button>
        </div>
      </div>
    </div>
  );
}

function RevertDialog({ hijo, onConfirm, onClose }: { hijo: Hijo; onConfirm: (motivo: string) => void; onClose: () => void }) {
  const [motivo, setMotivo] = useState('');
  const valido = motivo.length >= 10;

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center" onClick={onClose}>
      <div className="absolute inset-0" style={{ background: 'rgba(15,23,42,0.45)' }} />
      <div className="modal-card relative" onClick={(e) => e.stopPropagation()}>
        <h3 className="m-0 mb-2 text-lg font-extrabold">Reversar entrega</h3>
        <p className="mb-4 text-sm" style={{ color: '#64748B' }}>
          ¿Reversar entrega de <strong style={{ color: '#0F172A' }}>{hijo.nombreHijo}</strong>?
        </p>
        <textarea value={motivo} onChange={(e) => setMotivo(e.target.value)} placeholder="Motivo de la reversión (mín. 10 caracteres)" className="w-full mb-4 h-20 resize-none" />
        <div className="grid grid-cols-2 gap-3">
          <button onClick={onClose} className="btn btn-outline">Cancelar</button>
          <button onClick={() => valido && onConfirm(motivo)} disabled={!valido} className="btn"
            style={valido ? { background: '#EF4444', color: 'white' } : { background: '#E2E8F0', color: '#94A3B8' }}>
            Confirmar
          </button>
        </div>
      </div>
    </div>
  );
}
