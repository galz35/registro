import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import { ChevronLeft, FileText, Download, Users, Gift, Package, Search, Loader2 } from 'lucide-react';

type Tab = 'asistencia' | 'despacho' | 'inventario';

export default function ReportsPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>('asistencia');

  return (
    <div style={{ background: '#f8f9fa', minHeight: '100vh' }}>
      <div style={{ background: 'linear-gradient(135deg, #da121a 0%, #1e1e1e 100%)', color: 'white', padding: '16px 24px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <button onClick={() => navigate('/')}
            style={{ background: 'rgba(255,255,255,0.15)', border: 'none', color: 'white', borderRadius: '20px', padding: '6px 16px', fontWeight: 700, fontSize: 13, cursor: 'pointer' }}>
            <ChevronLeft className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Dashboard
          </button>
          <h2 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, fontSize: 22, margin: 0 }}>
            <FileText className="w-5 h-5" style={{ verticalAlign: 'middle', marginRight: 8 }} /> Reportes
          </h2>
          <div />
        </div>
      </div>

      <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
        <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
          {([['asistencia', 'Asistencia', Users], ['despacho', 'Despacho', Gift], ['inventario', 'Inventario', Package]] as const).map(([k, label, Icon]) => (
            <button key={k} onClick={() => setTab(k)}
              style={{ flex: 1, padding: '12px', borderRadius: 10, border: 'none', fontWeight: 700, fontSize: 13, cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8,
                ...(tab === k ? { background: '#da121a', color: 'white' } : { background: 'white', color: '#6b7280', border: '1px solid #e5e7eb' })
              }}>
              <Icon className="w-4 h-4" /> {label}
            </button>
          ))}
        </div>

        {tab === 'asistencia' && <AsistenciaReport />}
        {tab === 'despacho' && <DespachoReport />}
        {tab === 'inventario' && <InventarioReport />}
      </div>
    </div>
  );
}

function AsistenciaReport() {
  const [rows, setRows] = useState<any[]>([]);
  const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [loading, setLoading] = useState(true);
  const porPagina = 20;

  const load = async (p: number) => {
    setLoading(true);
    try {
      const res = await api.get('/attendance/censo', { params: { eventoId: 1, pagina: p, porPagina } });
      const d = res.data;
      const items = (d.data || []).filter((r: any) => r.Asistio > 0);
      setRows(items);
      setTotal(d.total || 0);
    } catch {}
    setLoading(false);
  };

  useEffect(() => { load(pagina); }, [pagina]);

  if (loading) return <div style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}><Loader2 className="w-6 h-6" style={{ animation: 'spin 0.8s linear infinite', margin: '0 auto 8px' }} /><p>Cargando...</p></div>;

  const totalAdultos = rows.reduce((s, r) => s + (r.TotalAdultos || 0), 0);
  const totalNinos = rows.reduce((s, r) => s + (r.TotalNinos || 0), 0);
  const totalHijos = rows.reduce((s, r) => s + r.TotalHijos, 0);
  const totalPag = Math.max(1, Math.ceil(total / porPagina));

  return (
    <div>
      <div style={{ background: '#da121a', color: 'white', padding: '12px 16px', borderRadius: '12px 12px 0 0', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <span style={{ fontWeight: 700, fontSize: 15 }}>📋 Reporte de Asistencia</span>
        <button onClick={() => window.open('/api/reports/asistencia.xlsx?eventoId=1', '_blank')}
          style={{ background: 'rgba(255,255,255,0.2)', border: 'none', color: 'white', borderRadius: 6, padding: '5px 12px', fontWeight: 600, fontSize: 11, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4 }}>
          <Download className="w-3 h-3" /> Excel
        </button>
      </div>
      <div style={{ background: 'white', padding: 16, borderRadius: '0 0 12px 12px', boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12, marginBottom: 20 }}>
          {[
            { label: 'Asistieron (esta pág)', value: rows.length, color: '#065f46', bg: '#d1fae5' },
            { label: 'Adultos', value: totalAdultos, color: '#1e40af', bg: '#dbeafe' },
            { label: 'Niños (evento)', value: totalNinos, color: '#92400e', bg: '#fef3c7' },
            { label: 'Hijos censados', value: totalHijos, color: '#831843', bg: '#fce7f3' },
          ].map(s => (
            <div key={s.label} style={{ background: s.bg, borderRadius: 10, padding: '14px', textAlign: 'center' }}>
              <div style={{ fontSize: 11, fontWeight: 700, color: s.color, textTransform: 'uppercase' }}>{s.label}</div>
              <div style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 800, fontSize: 28, color: s.color }}>{s.value}</div>
            </div>
          ))}
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
            <thead><tr style={{ background: '#da121a', color: 'white' }}>
              <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Carnet</th>
              <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Nombre</th>
              <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Gerencia</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Adultos</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Niños</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Hijos</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Asistió</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Fecha</th>
            </tr></thead>
            <tbody>
              {rows.map(r => (
                <tr key={r.Carnet} style={{ borderBottom: '1px solid #f3f4f6' }}>
                  <td style={{ padding: '8px 14px', color: '#da121a', fontWeight: 700 }}>{r.Carnet}</td>
                  <td style={{ padding: '8px 14px', fontWeight: 600 }}>{r.Nombre}</td>
                  <td style={{ padding: '8px 14px', color: '#6b7280', fontSize: 11 }}>{r.Gerencia || '-'}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>{r.TotalAdultos || 0}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>{r.TotalNinos || 0}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>{r.TotalHijos}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>
                    <span style={{ padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700, background: '#d1fae5', color: '#065f46' }}>
                      {r.AsistioPor === 'COLABORADOR' ? 'Colaborador' : r.AsistioPor === 'CONYUGE' ? 'Cónyuge' : r.NombreAsistente || 'Sí'}
                    </span>
                  </td>
                  <td style={{ padding: '8px 14px', textAlign: 'center', fontSize: 11, color: '#6b7280' }}>
                    {r.FechaAsistencia ? new Date(r.FechaAsistencia).toLocaleString() : '-'}
                  </td>
                </tr>
              ))}
              {rows.length === 0 && <tr><td colSpan={8} style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>Sin datos</td></tr>}
            </tbody>
          </table>
        </div>
        {totalPag > 1 && (
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 0 0', marginTop: 12, borderTop: '1px solid #e5e7eb' }}>
            <span style={{ fontSize: 12, color: '#6b7280' }}>Página {pagina} de {totalPag} ({total} registros)</span>
            <div style={{ display: 'flex', gap: 8 }}>
              <button onClick={() => setPagina(p => Math.max(1, p - 1))} disabled={pagina === 1}
                style={{ padding: '6px 14px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, fontWeight: 600, cursor: pagina === 1 ? 'not-allowed' : 'pointer', opacity: pagina === 1 ? 0.5 : 1 }}>Anterior</button>
              <button onClick={() => setPagina(p => Math.min(totalPag, p + 1))} disabled={pagina >= totalPag}
                style={{ padding: '6px 14px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, fontWeight: 600, cursor: pagina >= totalPag ? 'not-allowed' : 'pointer', opacity: pagina >= totalPag ? 0.5 : 1 }}>Siguiente</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function DespachoReport() {
  const [data, setData] = useState<any[]>([]);
  const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [busqueda, setBusqueda] = useState('');
  const porPagina = 30;

  const load = async (p: number, q: string) => {
    try {
      const params: any = { eventoId: 1, pagina: p, porPagina };
      if (q) params.busqueda = q;
      const res = await api.get('/dispatch/event/1/summary', { params });
      setData(res.data.data || []);
      setTotal(res.data.total || 0);
    } catch {}
  };

  useEffect(() => { load(pagina, busqueda); }, [pagina]);

  const totalPag = Math.ceil(total / porPagina);

  return (
    <div>
      <div style={{ background: '#da121a', color: 'white', padding: '12px 16px', borderRadius: '12px 12px 0 0', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <span style={{ fontWeight: 700, fontSize: 15 }}>📦 Reporte de Despacho</span>
        <button onClick={() => window.open('/api/reports/despacho.xlsx?eventoId=1', '_blank')}
          style={{ background: 'rgba(255,255,255,0.2)', border: 'none', color: 'white', borderRadius: 6, padding: '5px 12px', fontWeight: 600, fontSize: 11, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4 }}>
          <Download className="w-3 h-3" /> Excel
        </button>
      </div>
      <div style={{ background: 'white', padding: 16, borderRadius: '0 0 12px 12px', boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
        <div style={{ display: 'flex', gap: 12, marginBottom: 16 }}>
          <div style={{ flex: 1, position: 'relative' }}>
            <Search className="w-4 h-4" style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)', color: '#9ca3af' }} />
            <input type="text" value={busqueda} onChange={e => setBusqueda(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && (setPagina(1), load(1, busqueda))}
              placeholder="Buscar por carnet, nombre o hijo..."
              style={{ width: '100%', padding: '10px 14px 10px 36px', borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14, outline: 'none' }} />
          </div>
          <button onClick={() => { setPagina(1); load(1, busqueda); }}
            style={{ background: '#da121a', color: 'white', border: 'none', borderRadius: 8, padding: '10px 20px', fontWeight: 600, fontSize: 13, cursor: 'pointer' }}>Buscar</button>
          <div style={{ fontSize: 13, color: '#6b7280', fontWeight: 600, display: 'flex', alignItems: 'center' }}>{total} registros</div>
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
            <thead><tr style={{ background: '#da121a', color: 'white' }}>
              <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Colaborador</th>
              <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Hijo</th>
              <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Juguete</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Estado</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Recibió</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Fecha</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Despachó</th>
            </tr></thead>
            <tbody>
              {data.map(r => (
                <tr key={r.entregaId} style={{ borderBottom: '1px solid #f3f4f6' }}>
                  <td style={{ padding: '8px 14px' }}><div style={{ fontWeight: 600 }}>{r.colaboradorNombre}</div><div style={{ fontSize: 11, color: '#da121a' }}>{r.colaboradorCarnet}</div></td>
                  <td style={{ padding: '8px 14px', fontWeight: 600 }}>{r.hijoNombre}</td>
                  <td style={{ padding: '8px 14px', color: '#6b7280' }}>{r.nombreJuguete}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>
                    <span style={{ padding: '3px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700,
                      ...(r.estado === 'DELIVERED' ? { background: '#d1fae5', color: '#065f46' } : { background: '#fee2e2', color: '#991b1b' }) }}>
                      {r.estado === 'DELIVERED' ? 'Entregado' : 'Reversado'}
                    </span>
                  </td>
                  <td style={{ padding: '8px 14px', textAlign: 'center', fontSize: 11 }}>{r.receptorFinal || r.recibidoPor}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center', fontSize: 11, color: '#6b7280' }}>{r.fechaEntrega ? new Date(r.fechaEntrega).toLocaleString() : '-'}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center', fontSize: 11 }}>{r.usuarioDespacho}</td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>Sin movimientos</td></tr>}
            </tbody>
          </table>
        </div>
        {totalPag > 1 && (
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 0 0', marginTop: 12, borderTop: '1px solid #e5e7eb' }}>
            <span style={{ fontSize: 12, color: '#6b7280' }}>Página {pagina} de {totalPag}</span>
            <div style={{ display: 'flex', gap: 8 }}>
              <button onClick={() => setPagina(p => Math.max(1, p - 1))} disabled={pagina === 1}
                style={{ padding: '6px 14px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, fontWeight: 600, cursor: pagina === 1 ? 'not-allowed' : 'pointer', opacity: pagina === 1 ? 0.5 : 1 }}>Anterior</button>
              <button onClick={() => setPagina(p => Math.min(totalPag, p + 1))} disabled={pagina >= totalPag}
                style={{ padding: '6px 14px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, fontWeight: 600, cursor: pagina >= totalPag ? 'not-allowed' : 'pointer', opacity: pagina >= totalPag ? 0.5 : 1 }}>Siguiente</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function InventarioReport() {
  const [items, setItems] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedJuguete, setSelectedJuguete] = useState<any>(null);
  const [detalle, setDetalle] = useState<any[]>([]);
  const [loadingDetalle, setLoadingDetalle] = useState(false);

  useEffect(() => {
    api.get('/catalog/summary').then(res => setItems(res.data || [])).catch(() => {}).finally(() => setLoading(false));
  }, []);

  const verDetalle = async (nombre: string) => {
    setLoadingDetalle(true);
    try {
      const res = await api.get('/dispatch/event/1/summary', { params: { pagina: 1, porPagina: 500 } });
      const all = res.data.data || [];
      const filtrados = all.filter((r: any) => r.nombreJuguete === nombre && r.estado === 'DELIVERED');
      setDetalle(filtrados);
      setSelectedJuguete(items.find(i => i.nombreJuguete === nombre) || null);
    } catch {}
    setLoadingDetalle(false);
  };

  if (loading) return <p style={{ textAlign: 'center', color: '#9ca3af' }}>Cargando inventario...</p>;

  const totalInicial = items.reduce((s, i) => s + i.stockInicial, 0);
  const totalActual = items.reduce((s, i) => s + i.stockActual, 0);
  const totalEntregados = items.reduce((s, i) => s + (i.entregados || 0), 0);

  return (
    <div>
      <div style={{ background: '#da121a', color: 'white', padding: '12px 16px', borderRadius: '12px 12px 0 0', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <span style={{ fontWeight: 700, fontSize: 15 }}>📊 Reporte de Inventario</span>
        <button onClick={() => window.open('/api/reports/inventario.xlsx', '_blank')}
          style={{ background: 'rgba(255,255,255,0.2)', border: 'none', color: 'white', borderRadius: 6, padding: '5px 12px', fontWeight: 600, fontSize: 11, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4 }}>
          <Download className="w-3 h-3" /> Excel
        </button>
      </div>
      <div style={{ background: 'white', padding: 16, borderRadius: '0 0 12px 12px', boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 20 }}>
          {[
            { label: 'Stock Inicial', value: totalInicial, color: '#1e40af', bg: '#dbeafe' },
            { label: 'Stock Actual', value: totalActual, color: '#065f46', bg: '#d1fae5' },
            { label: 'Entregados', value: totalEntregados, color: '#991b1b', bg: '#fee2e2' },
          ].map(s => (
            <div key={s.label} style={{ background: s.bg, borderRadius: 10, padding: '14px', textAlign: 'center' }}>
              <div style={{ fontSize: 11, fontWeight: 700, color: s.color, textTransform: 'uppercase' }}>{s.label}</div>
              <div style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 800, fontSize: 28, color: s.color }}>{s.value}</div>
            </div>
          ))}
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
            <thead><tr style={{ background: '#da121a', color: 'white' }}>
              <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Juguete</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Categoría</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Stock Inicial</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Stock Actual</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Entregados</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>%</th>
              <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Detalle</th>
            </tr></thead>
            <tbody>
              {items.map((item, i) => (
                <tr key={i} style={{ borderBottom: '1px solid #f3f4f6' }}>
                  <td style={{ padding: '8px 14px', fontWeight: 600 }}>{item.nombreJuguete}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center', color: '#6b7280' }}>{item.categoria}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>{item.stockInicial}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>
                    <span style={{ fontWeight: 700, color: item.stockActual <= 5 ? '#dc2626' : item.stockActual <= 10 ? '#d97706' : '#10b981' }}>{item.stockActual}</span>
                  </td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>{item.entregados || 0}</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>{item.porcentajeDespacho || 0}%</td>
                  <td style={{ padding: '8px 14px', textAlign: 'center' }}>
                    <button onClick={() => verDetalle(item.nombreJuguete)}
                      style={{ background: 'none', border: '1px solid #e5e7eb', borderRadius: 6, padding: '4px 8px', cursor: 'pointer', fontSize: 12 }}>👁️ Ver</button>
                  </td>
                </tr>
              ))}
              {items.length === 0 && <tr><td colSpan={7} style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>Sin datos</td></tr>}
            </tbody>
          </table>
        </div>
      </div>

      {selectedJuguete && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 50, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(0,0,0,0.4)' }}
          onClick={() => { setSelectedJuguete(null); setDetalle([]); }}>
          <div onClick={e => e.stopPropagation()} style={{ background: 'white', borderRadius: 16, padding: 24, width: '90%', maxWidth: 700, margin: 20, maxHeight: '80vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <h3 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, margin: 0 }}>📦 {selectedJuguete.nombreJuguete}</h3>
              <button onClick={() => { setSelectedJuguete(null); setDetalle([]); }} style={{ background: 'none', border: 'none', fontSize: 20, cursor: 'pointer', color: '#6b7280' }}>✕</button>
            </div>
            <div style={{ display: 'flex', gap: 16, marginBottom: 16, fontSize: 13 }}>
              <span><strong>Categoría:</strong> {selectedJuguete.categoria}</span>
              <span><strong>Stock:</strong> {selectedJuguete.stockActual}/{selectedJuguete.stockInicial}</span>
              <span><strong>Entregados:</strong> {selectedJuguete.entregados || 0}</span>
            </div>
            {loadingDetalle ? <p style={{ textAlign: 'center', color: '#9ca3af' }}>Cargando...</p>
            : detalle.length > 0 ? (
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
                <thead><tr style={{ background: '#f8fafc' }}>
                  <th style={{ padding: '8px 12px', textAlign: 'left', fontWeight: 700, color: '#6b7280' }}>Colaborador</th>
                  <th style={{ padding: '8px 12px', textAlign: 'left', fontWeight: 700, color: '#6b7280' }}>Hijo</th>
                  <th style={{ padding: '8px 12px', textAlign: 'center', fontWeight: 700, color: '#6b7280' }}>Recibió</th>
                  <th style={{ padding: '8px 12px', textAlign: 'center', fontWeight: 700, color: '#6b7280' }}>Fecha</th>
                  <th style={{ padding: '8px 12px', textAlign: 'center', fontWeight: 700, color: '#6b7280' }}>Despachó</th>
                </tr></thead>
                <tbody>
                  {detalle.map((r: any) => (
                    <tr key={r.entregaId} style={{ borderBottom: '1px solid #f3f4f6' }}>
                      <td style={{ padding: '8px 12px', fontWeight: 600 }}>{r.colaboradorNombre}</td>
                      <td style={{ padding: '8px 12px' }}>{r.hijoNombre}</td>
                      <td style={{ padding: '8px 12px', textAlign: 'center' }}>{r.receptorFinal || r.recibidoPor}</td>
                      <td style={{ padding: '8px 12px', textAlign: 'center', fontSize: 11, color: '#6b7280' }}>{r.fechaEntrega ? new Date(r.fechaEntrega).toLocaleString() : '-'}</td>
                      <td style={{ padding: '8px 12px', textAlign: 'center', fontSize: 11 }}>{r.usuarioDespacho}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : <p style={{ textAlign: 'center', color: '#9ca3af' }}>Sin entregas registradas</p>}
          </div>
        </div>
      )}
    </div>
  );
}
